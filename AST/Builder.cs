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
		private Instruction? _currentInstruction = null;
		private uint _endAddress = 0;
		private bool _running = false;
		private ControlFlowData _data = new();
		private Stack<Node> _nodeStack = [];

		private bool IsAtEnd => !_running || _currentInstruction == null || _currentInstruction.Address > _endAddress;

		public List<Node> Build(ControlFlowData data, Disassembly disassembly) => Build(data, disassembly, disassembly.First.Address, disassembly.Last.Address);

		public List<Node> Build(ControlFlowData data, Disassembly disassembly, uint startAddress, uint endAddress)
		{
			_disassembly = disassembly;
			_data = data;
			_currentInstruction = disassembly.GetInstruction(startAddress);
			_endAddress = endAddress;
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

						return node;
					}

					case ControlFlowBlockType.Loop:
					{
						var node = new LoopNode(body.Last())
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

				case FunctionInstruction function:
				{
					return new FunctionDeclarationNode(function)
					{
						Body = function.HasBody ? ParseRange(_currentInstruction.Address, function.EndAddress - 1) : [],
					};
				}

				case BranchInstruction branch:
				{
					if (branch.IsLogicalOperator)
					{
						throw new NotImplementedException("Logical AND/OR parsing not implemented.");
					}

					if (branch.IsUnconditional)
					{
						return _data.Branches[branch.Address].Type switch
						{
							ControlFlowBranchType.Break => new BreakNode(),
							ControlFlowBranchType.Continue => new ContinueNode(),
							_ => null,
						};
					}

					return null;
				}

				case LoadVariableInstruction or UnusedInstruction or DebugBreakInstruction:
					return null;

				default:
					throw new BuilderException($"Unknown or unhandled instruction class: {instruction.GetType().Name}");
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
		private Node Pop() => _nodeStack.Pop();
		private Node Peek() => _nodeStack.Peek();

		private Instruction? Read()
		{
			var instruction = _currentInstruction;

			_currentInstruction = _currentInstruction?.Next;

			return instruction;
		}
	}
}
