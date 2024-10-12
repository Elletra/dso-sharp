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
		private List<Instruction> _instructions = [];
		private int _index = 0;
		private Stack<Node> _nodeStack = [];
		private Stack<List<Node>> _frameStack = [];
		private Stack<ControlFlowBlock> _blockStack = [];

		// TODO: Possibly handle nested functions later?
		private FunctionDeclarationNode? _function = null;

		private bool IsAtEnd => _index >= _instructions.Count;

		public List<Node> Build(ControlFlowBlock root, Disassembly disassembly)
		{
			_instructions = disassembly.GetInstructions();
			_index = 0;
			_nodeStack = [];
			_frameStack = [];
			_blockStack = [];
			_function = null;

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

				case CreateObjectInstruction: return null;
				case AddObjectInstruction: return null;
				case EndObjectInstruction: return null;
				case BranchInstruction: return null;
				case BinaryInstruction: return null;
				case BinaryStringInstruction: return null;
				case UnaryInstruction: return null;
				case VariableInstruction: return null;
				case VariableArrayInstruction: return null;
				case SaveVariableInstruction: return null;
				case ObjectInstruction: return null;
				case ObjectNewInstruction: return null;
				case FieldInstruction: return null;
				case FieldArrayInstruction: return null;
				case SaveFieldInstruction: return null;

				case AdvanceStringInstruction str: return new ConcatNode(Pop());
				case AdvanceAppendInstruction str: return new ConcatNode(Pop(), str.Char);
				case AdvanceCommaInstruction str: return new CommaConcatNode(Pop());

				case RewindStringInstruction:
				case TerminateRewindInstruction:
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
						Push(right);
						Push(concat.Left);
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

		private Instruction? Read() => !IsAtEnd ? _instructions[_index++] : null;
	}
}
