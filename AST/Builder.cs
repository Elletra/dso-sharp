using DSO.ControlFlow;
using DSO.Disassembler;
using static System.Net.Mime.MediaTypeNames;

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
		private List<Instruction> _instructions = [];
		private int _index = 0;
		private Stack<Node> _nodeStack = [];
		private Stack<List<Node>> _frameStack = [];
		private Stack<ControlFlowBlock> _blockStack = [];

		// TODO: Possibly handle nested functions later?
		private FunctionDeclarationNode? _function = null;

		private int _objectDepth = 0;

		private bool IsAtEnd => _index >= _instructions.Count;

		public List<Node> Build(ControlFlowBlock root, Disassembly disassembly)
		{
			_instructions = disassembly.GetInstructions();
			_index = 0;
			_nodeStack = [];
			_frameStack = [];
			_blockStack = [];
			_function = null;
			_objectDepth = 0;

			_blockStack.Push(root);

			Build();

			List<Node> list = [.._nodeStack];

			list.Reverse();

			return list;
		}

		private void Build()
		{
			while (!IsAtEnd)
			{
				var node = Parse(Read());

				if (node != null)
				{
					Push(node);
				}
			}
		}

		private Node? Parse(Instruction? instruction)
		{
			if (instruction == null)
			{
				throw new BuilderException("Instruction is null");
			}

			if (_function != null && instruction.Address >= _function.EndAddress)
			{
				_function = null;
			}

			switch (instruction)
			{
				case ReturnInstruction ret: return new ReturnNode(ret.ReturnsValue ? Pop() : null);

				case ImmediateInstruction<string> immediate: return new ConstantNode<string>(immediate);
				case ImmediateInstruction<double> immediate: return new ConstantNode<double>(immediate);
				case ImmediateInstruction<uint> immediate: return new ConstantNode<uint>(immediate);

				case PushInstruction:
					_frameStack.Peek().Add(Pop());
					return null;

				case PushFrameInstruction:
					_frameStack.Push([]);
					return null;

				case CallInstruction call:
				{
					var node = new FunctionCallNode(call);

					_frameStack.Pop().ForEach(node.Arguments.Add);

					return node;
				}

				case FunctionInstruction function:
					return _function = new(function);

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
						throw new BuilderException($"Expected ObjectDeclarationNode before {add.Opcode.Value} at {add.Address}");
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
						throw new BuilderException($"Expected ObjectDeclarationNode before {end.Opcode.Value} at {end.Address}");
					}

					while (children.Count > 0)
					{
						obj.Children.Add(children.Pop());
					}

					_objectDepth--;

					return obj;
				}

				case BranchInstruction: return null;

				case BinaryInstruction binary:
					return new BinaryNode(Pop(), Pop(), binary.Opcode);

				case BinaryStringInstruction binary:
				{
					var right = Pop();
					var left = Pop();

					return new BinaryStringNode(left, right, binary.Opcode);
				}

				case UnaryInstruction unary:
				{
					var popped = Pop();
					var opcode = unary.Opcode;

					if (popped is BinaryStringNode binary && (opcode.Value == Opcodes.Ops.OP_NOT || opcode.Value == Opcodes.Ops.OP_NOTF))
					{
						return new BinaryStringNode(binary.Left, binary.Right, opcode, not: true);
					}

					return new UnaryNode(popped, opcode);
				}

				case FieldInstruction field: return new FieldNode(field.Name);
				case VariableInstruction variable: return new VariableNode(variable.Name);

				case ObjectInstruction or ObjectNewInstruction:
				{
					var next = Parse(Read());

					if (next is not FieldNode field)
					{
						throw new BuilderException($"Expected FieldInstruction after {instruction.Opcode.Value} at {instruction.Address}");
					}

					if (instruction is not ObjectNewInstruction)
					{
						field.Object = Pop();
					}

					return field;
				}

				case FieldArrayInstruction or VariableArrayInstruction:
				{
					var node = Pop();

					if (node is FieldNode field)
					{
						field.Index = Pop();
					}
					else if (node is VariableNode variable)
					{
						variable.Index = Pop();
					}
					else if (instruction is VariableArrayInstruction && node is ConcatNode concat && concat.Left is ConstantNode<string> identifier)
					{
						node = new VariableNode(identifier.Value, concat.Right);
					}
					else
					{
						throw new BuilderException($"Expected variable or field before array instruction at {instruction.Address}");
					}

					return node;
				}

				case SaveFieldInstruction or SaveVariableInstruction:
				{
					AssignmentNode node;

					var prev = Pop();

					if (prev is FieldNode or VariableNode)
					{
						node = new(prev, Pop());
					}
					else if (prev is BinaryNode binary)
					{
						node = new(binary.Left, binary.Right, binary.Op);
					}
					else
					{
						throw new BuilderException($"Expected field, variable, or binary expression before {instruction.Opcode.Value} at {instruction.Address}");
					}

					return node;
				}

				case AdvanceStringInstruction str: return new ConcatNode(Pop());
				case AdvanceAppendInstruction str: return new ConcatNode(Pop(), str.Char);
				case AdvanceCommaInstruction str: return new CommaConcatNode(Pop());

				case RewindStringInstruction or TerminateRewindInstruction:
				{
					var right = Pop();
					var node = Pop();

					if (node is not ConcatNode concat)
					{
						throw new BuilderException($"Node is not a ConcatNode");
					}

					Node? returnNode = instruction is TerminateRewindInstruction ? null : concat;

					if (returnNode == null)
					{
						Push(concat.Left);
						Push(right);
					}
					else
					{
						concat.Right = right;
					}

					return returnNode;
				}

				case LoadVariableInstruction or LoadFieldInstruction or ConvertToTypeInstruction or
					AdvanceNullInstruction or DebugBreakInstruction or UnusedInstruction:
					return null;

				default:
					throw new BuilderException($"Unknown or unhandled instruction class: {instruction.GetType().Name}");
			};
		}

		private void Push(Node node)
		{
			if (_function == null || node is FunctionDeclarationNode)
			{
				_nodeStack.Push(node);
			}
			else
			{
				_function.Body.Add(node);
			}
		}

		private Node Pop()
		{
			if (_function == null)
			{
				return _nodeStack.Pop();
			}

			var last = _function.Body.Last();

			_function.Body.RemoveAt(_function.Body.Count - 1);

			return last;
		}

		private Node Peek() => _function == null ? _nodeStack.Peek() : _function.Body.Last();

		private Instruction? Read() => !IsAtEnd ? _instructions[_index++] : null;
	}
}
