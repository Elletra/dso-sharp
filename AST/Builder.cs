using DSO.AST.Nodes;
using DSO.ControlFlow;
using DSO.Disassembler;

namespace DSO.AST
{
    public class BuilderException : Exception
	{
		public BuilderException() { }
		public BuilderException(string message) : base(message) { }
		public BuilderException(string message, Exception inner) : base(message, inner) { }
	}

	public class Builder
	{
		private Disassembly _disassembly = new();
		private ControlFlowData _data = new();
		private Instruction? _currentInstruction = null;
		private uint _endAddress = 0;
		private bool _running = false;
		private int _objectDepth = 0;
		private Stack<List<Node>> _frameStack = [];
		private Stack<Node> _nodeStack = [];

		private bool IsAtEnd => !_running || _currentInstruction == null || _currentInstruction.Address > _endAddress;

		public List<Node> Build(ControlFlowData data, Disassembly disassembly) => Build(data, disassembly, disassembly.First.Address, disassembly.Last.Address);

		public List<Node> Build(ControlFlowData data, Disassembly disassembly, uint startAddress, uint endAddress)
		{
			_disassembly = disassembly;
			_data = data;
			_currentInstruction = disassembly.GetInstruction(startAddress);
			_endAddress = endAddress;
			_objectDepth = 0;
			_frameStack = [];
			_nodeStack = [];

			Build();

			List<Node> list = [.._nodeStack];

			list.Reverse();

			return list;
		}

		private void Build()
		{
			_running = true;

			while (!IsAtEnd)
			{
				var node = Parse(Read());

				if (node != null)
				{
					Push(node);
				}
			}

			// Like with function declarations, a return statement automatically gets put at the ends of files, so we remove it.
			if (Peek() is ReturnNode ret && ret.Value == null)
			{
				Pop();
			}
		}

		private Node? Parse(Instruction? instruction)
		{
			if (instruction == null)
			{
				throw new BuilderException("Instruction is null");
			}

			if (_data.Blocks.TryGetValue(instruction.Address, out Queue<ControlFlowBlock>? queue))
			{
				var block = queue.Dequeue();

				if (queue.Count <= 0)
				{
					_data.Blocks.Remove(instruction.Address);
				}

				var body = ParseRange(block.Start, block.End);

				switch (block.Type)
				{
					case ControlFlowBlockType.Conditional:
					{
						var node = new IfNode(Pop())
						{
							True = body,
						};

						if (block.End is BranchInstruction branch && branch.IsUnconditional && _data.Branches[branch.Address].Type == ControlFlowBranchType.Else)
						{
							node.False = ParseRange(branch.Next, _disassembly.GetInstruction(branch.TargetAddress).Prev);
						}

						return CollapseIfLoop(node);
					}

					case ControlFlowBlockType.Loop:
					{
						var last = body.Last();
						var node = new LoopNode(last is IfNode ifNode ? ifNode.ConvertToTernary() : last)
						{
							Body = body,
						};

						body.RemoveAt(body.Count - 1);

						return node;
					}

					default:
						return null;
				}
			}

			switch (instruction)
			{
				case ImmediateInstruction<string> immediate:
					return new ConstantNode<string>(immediate);

				case ImmediateInstruction<double> immediate:
					return new ConstantNode<double>(immediate);

				case ImmediateInstruction<uint> immediate:
					return new ConstantNode<uint>(immediate);

				case ReturnInstruction ret:
					return new ReturnNode(ret.ReturnsValue ? Pop() : null);

				case PushInstruction:
					_frameStack.Peek().Add(Pop());
					return null;

				case PushFrameInstruction:
					_frameStack.Push([]);
					return null;

				case AdvanceStringInstruction:
					return new ConcatNode(Pop());

				case AdvanceAppendInstruction append:
					return new ConcatNode(Pop(), append.Char);

				case AdvanceCommaInstruction:
					return new CommaConcatNode(Pop());

				case RewindStringInstruction or TerminateRewindInstruction:
				{
					var right = Pop();
					var node = Pop();

					if (node is not ConcatNode concat)
					{
						throw new BuilderException($"Unmatched string rewind at {instruction.Address}");
					}

					if (instruction is TerminateRewindInstruction)
					{
						Push(concat.Left);
						Push(right);

						return null;
					}

					concat.Right = right;

					return concat;
				}

				case UnaryInstruction unary:
				{
					var node = Pop();

					return node is BinaryStringNode binary && unary.IsNot
						? new BinaryStringNode(binary.Left, binary.Right, binary.Op, not: true)
						: new UnaryNode(node, unary.Opcode);
				}

				case BinaryInstruction binary:
					return new BinaryNode(Pop(), Pop(), binary.Opcode);

				case BinaryStringInstruction binary:
				{
					var right = Pop();
					var left = Pop();

					return new BinaryStringNode(left, right, binary.Opcode);
				}

				case VariableInstruction variable:
					return new VariableNode(variable.Name);

				case VariableArrayInstruction array:
				{
					var node = Pop();

					if (node is not ConcatNode concat || concat.Left is not ConstantNode<string> left)
					{
						throw new BuilderException($"Expected valid ConcatNode before variable array at {array.Address}");
					}

					return new VariableNode(left.Value, concat.Right);
				}

				case SaveVariableInstruction:
					return Pop() switch
					{
						VariableNode variable => new AssignmentNode(variable, Pop()),
						BinaryNode binary => new AssignmentNode(binary.Left, binary.Right, binary.Op),
						_ => throw new BuilderException($"Expected variable or binary expression before assignemnt at {instruction.Address}"),
					};

				case FieldInstruction field:
					return new FieldNode(field.Name);

				case ObjectInstruction or ObjectNewInstruction:
				{
					var next = Parse(Read());

					if (next is not FieldNode field)
					{
						throw new BuilderException($"Expected FieldNode after {instruction.Opcode.Value} at {instruction.Address}");
					}

					if (instruction is not ObjectNewInstruction)
					{
						field.Object = Pop();
					}

					return field;
				}

				case FieldArrayInstruction array:
				{
					var node = Pop();

					if (node is not FieldNode field)
					{
						throw new BuilderException($"Expected valid FieldNode before field array at {array.Address}");
					}

					field.Index = Pop();

					return field;
				}

				case SaveFieldInstruction:
					return Pop() switch
					{
						FieldNode field => new AssignmentNode(field, Pop()),
						BinaryNode binary => new AssignmentNode(binary.Left, binary.Right, binary.Op),
						_ => throw new BuilderException($"Expected field or binary expression before assignemnt at {instruction.Address}"),
					};

				case FunctionInstruction function:
				{
					var body = ParseRange(_currentInstruction.Address, function.EndAddress - 1);
					var node = new FunctionDeclarationNode(function)
					{
						Body = body,
					};

					// An empty return statement is automatically put at the ends of functions. It's redundant, so we remove it.
					if (body.Count > 0 && body.Last() is ReturnNode ret && ret.Value == null)
					{
						body.RemoveAt(body.Count - 1);
					}

					if (function.Package == null)
					{
						return node;
					}

					if (Peek() is PackageNode package && package.Name == function.Package)
					{
						Pop();
					}
					else
					{
						package = new(function.Package);
					}

					package.Functions.Add(node);

					return package;
				}

				case CallInstruction call:
				{
					var node = new FunctionCallNode(call);

					_frameStack.Pop().ForEach(node.Arguments.Add);

					return node;
				}

				case BranchInstruction branch:
				{
					if (branch.IsUnconditional)
					{
						return _data.Branches[branch.Address].Type switch
						{
							ControlFlowBranchType.Break => new BreakNode(),
							ControlFlowBranchType.Continue => new ContinueNode(),
							_ => null,
						};
					}

					if (branch.IsLogicalOperator)
					{
						var right = ParseRange(branch.Next, _disassembly.GetInstruction(branch.TargetAddress).Prev);

						if (right.Count != 1)
						{
							throw new BuilderException($"Could not parse logical operator right hand operand at {branch.Address}");
						}

						return new BinaryNode(Pop(), right[0], branch.Opcode);
					}

					return null;
				}

				case CreateObjectInstruction create:
				{
					var frame = _frameStack.Pop();
					var node = new ObjectDeclarationNode(create, frame[0], frame.Count > 1 ? frame[1] : null, _objectDepth++);

					for (var i = 2; i < frame.Count; i++)
					{
						node.Arguments.Add(frame[i]);
					}

					return node;
				}

				case AddObjectInstruction add:
				{
					var fields = new Stack<AssignmentNode>();

					while (Peek() is AssignmentNode)
					{
						fields.Push((AssignmentNode) Pop());
					}

					var node = Pop();

					if (node is not ObjectDeclarationNode obj)
					{
						throw new BuilderException($"Expected object declaration before {add.Opcode.Value} at {add.Address}");
					}

					if (add.PlaceAtRoot)
					{
						// Get rid of 0 uint immediate that gets placed before root objects.
						Pop();
					}

					while (fields.Count > 0)
					{
						obj.Fields.Add(fields.Pop());
					}

					return obj;
				}

				case EndObjectInstruction end:
				{
					var children = new Stack<ObjectDeclarationNode>();

					while (Peek() is ObjectDeclarationNode child && child.Depth == _objectDepth)
					{
						children.Push((ObjectDeclarationNode) Pop());
					}

					var node = Pop();

					if (node is not ObjectDeclarationNode obj)
					{
						throw new BuilderException($"Expected object declaration before {end.Opcode.Value} at {end.Address}");
					}

					while (children.Count > 0)
					{
						obj.Children.Add(children.Pop());
					}

					_objectDepth--;

					return obj;
				}

				case LoadVariableInstruction or LoadFieldInstruction or
					AdvanceNullInstruction or ConvertToTypeInstruction or
					UnusedInstruction or DebugBreakInstruction:
					return null;

				default:
					throw new BuilderException($"Unknown or unhandled instruction class: {instruction?.GetType().Name}");
			};
		}

		private Node CollapseIfLoop(IfNode node)
		{
			if (node.True.Count != 1 || node.False.Count > 0 || node.True[0] is not LoopNode loop || loop is WhileLoopNode or ForLoopNode || !Equals(node.Test, loop.Test))
			{
				return node;
			}

			var peek = Peek();

			if (peek != null && loop.Body.Count > 0 && peek.IsExpression)
			{
				return new ForLoopNode(Pop(), loop.Test, loop.Body[^1])
				{
					Body = loop.Body[..^1],
				};
			}

			return new WhileLoopNode(loop.Test)
			{
				Body = loop.Body,
			};
		}

		private List<Node> ParseRange(Instruction from, Instruction to) => ParseRange(from.Address, to.Address);

		private List<Node> ParseRange(uint fromAddress, uint toAddress)
		{
			var builder = new Builder();
			var list = builder.Build(_data, _disassembly, fromAddress, toAddress);

			_currentInstruction = _disassembly.GetInstruction(toAddress)?.Next;

			return list;
		}

		private void Push(Node node) => _nodeStack.Push(node);

		private Node Pop()
		{
			var node = _nodeStack.Pop();

			if (node is IfNode ifNode)
			{
				node = ifNode.ConvertToTernary();
			}

			return node;
		}

		private Node? Peek() => _nodeStack.Count > 0 ? _nodeStack.Peek() : null;

		private Instruction? Read()
		{
			var instruction = _currentInstruction;

			_currentInstruction = _currentInstruction?.Next;

			return instruction;
		}
	}
}
