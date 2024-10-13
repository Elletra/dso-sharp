using DSO.Disassembler;

namespace DSO.ControlFlow
{
	public enum ControlFlowBlockType
	{
		Root,
		Function,
		Conditional,
		Loop,
	}

	public enum ControlFlowBranchType
	{
		Else,
		Continue,
		Break,
	}

	public class ControlFlowBranch(uint start, uint target)
	{
		public ControlFlowBranchType Type { get; set; } = ControlFlowBranchType.Else;
		public readonly uint StartAddress = start;
		public readonly uint TargetAddress = target;
	}

	public class ControlFlowBlock(ControlFlowBlockType type, Instruction start, Instruction end)
	{
		public readonly ControlFlowBlockType Type = type;
		public readonly Instruction Start = start;
		public readonly Instruction End = end;

		public readonly List<ControlFlowBlock> Children = [];
		public readonly Dictionary<uint, ControlFlowBranch> Branches = [];

		public uint? ContinuePoint { get; set; } = null;
		public ControlFlowBlock? Parent { get; set; } = null;

		public void AddBranch(BranchInstruction branch)
		{
			Branches.Add(branch.Address, new(branch.Address, branch.TargetAddress));
		}

		public void AddChild(ControlFlowBlock child)
		{
			child.Parent = this;

			Children.Add(child);
		}

		public ControlFlowBlock? FindOuterLoop()
		{
			if (Parent == null)
			{
				return null;
			}

			if (Parent.Type == ControlFlowBlockType.Loop)
			{
				return Parent;
			}

			return Parent.FindOuterLoop();
		}
	}
}
