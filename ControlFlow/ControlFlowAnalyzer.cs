using DSO.Disassembler;

namespace DSO.ControlFlow
{
	public class ControlFlowAnalyzerException : Exception
	{
		public ControlFlowAnalyzerException() { }
		public ControlFlowAnalyzerException(string message) : base(message) { }
		public ControlFlowAnalyzerException(string message, Exception inner) : base(message, inner) { }
	}

	public class ControlFlowData
	{
		public readonly Dictionary<uint, List<ControlFlowBlock>> Blocks = [];
		public readonly Dictionary<uint, ControlFlowBranch> Branches = [];

		public void AddBlock(ControlFlowBlock block)
		{
			if (!Blocks.ContainsKey(block.Start.Address))
			{
				Blocks[block.Start.Address] = [];
			}

			Blocks[block.Start.Address].Add(block);
		}

		public void AddBranch(ControlFlowBranch branch) => Branches[branch.StartAddress] = branch;
	}

	public class ControlFlowAnalyzer
	{
		public ControlFlowData Analyze(Disassembly disassembly)
		{
			var root = BuildControlFlowBlocks(disassembly);

			AnalyzeBranches(root);

			return FlattenBlocks(root);
		}

		public ControlFlowData FlattenBlocks(ControlFlowBlock root)
		{
			var data = new ControlFlowData();

			FlattenBlocks(root, data);

			return data;
		}

		private ControlFlowBlock BuildControlFlowBlocks(Disassembly disassembly)
		{
			var blocks = new List<ControlFlowBlock>()
			{
				new(ControlFlowBlockType.Root, disassembly.First, disassembly.Last),
			};

			foreach (var branch in disassembly.Branches)
			{
				if (branch.IsConditional && !branch.IsLogicalOperator)
				{
					var type = branch.IsLoopEnd ? ControlFlowBlockType.Loop : ControlFlowBlockType.Conditional;
					var start = branch.IsLoopEnd ? disassembly.GetInstruction(branch.TargetAddress) : branch;
					var end = branch.IsLoopEnd ? branch : disassembly.GetInstruction(branch.TargetAddress).Prev;

					blocks.Add(new(type, start, end));
				}
			}

			blocks.Sort((block1, block2) =>
			{
				if (block1.Start.Address == block2.Start.Address)
				{
					if (block1.End.Address < block2.End.Address)
					{
						return 1;
					}

					if (block1.End.Address > block2.End.Address)
					{
						return -1;
					}

					return 0;
				}

				return block1.Start.Address < block2.Start.Address ? -1 : 1;
			});

			var blockStack = new Stack<ControlFlowBlock>();
			var blockIndex = 0;

			foreach (var instruction in disassembly)
			{
				while (blockStack.Count > 0 && blockStack.Peek().End.Address < instruction.Address)
				{
					var popped = blockStack.Pop();

					blockStack.Peek().AddChild(popped);
				}

				while (blockIndex < blocks.Count && blocks[blockIndex].Start.Address == instruction.Address)
				{
					blockStack.Push(blocks[blockIndex++]);
				}

				if (instruction is BranchInstruction branch && branch.IsUnconditional)
				{
					blockStack.Peek().AddBranch(branch);
				}
			}

			if (blockStack.Count != 1)
			{
				throw new ControlFlowAnalyzerException("Block stack count != 1");
			}

			return blockStack.Pop();
		}

		private void AnalyzeBranches(ControlFlowBlock block)
		{
			var outerLoop = block.FindOuterLoop();
			var parent = block.Parent;

			foreach (var branch in block.Branches.Values)
			{
				if (block.Type == ControlFlowBlockType.Loop)
				{
					// If the OP_JMP is just directly in a loop, it's a continue.
					branch.Type = branch.TargetAddress <= block.End.Address ? ControlFlowBranchType.Continue : ControlFlowBranchType.Break;
				}
				else if (outerLoop != null)
				{
					if (branch.TargetAddress == outerLoop.End.Next?.Address)
					{
						branch.Type = ControlFlowBranchType.Break;
					}
					else if (branch.StartAddress < block.End.Address)
					{
						// If the branch is in a conditional, but it's not at the end, it's a continue.
						branch.Type = ControlFlowBranchType.Continue;
					}
					else if (parent != null && parent.Type == ControlFlowBlockType.Conditional && branch.TargetAddress > parent.End.Address)
					{
						// If the parent is a conditional, and the destination is higher than its end, it's a continue.
						branch.Type = ControlFlowBranchType.Continue;
					}
				}

				if (branch.Type == ControlFlowBranchType.Continue)
				{
					block.ContinuePoint = branch.TargetAddress;
				}
			}

			// Once the continue point has been determined (if there is one), we can set all branches to it as continues, just in case.
			foreach (var branch in block.Branches.Values)
			{
				if (outerLoop != null && branch.TargetAddress == outerLoop.ContinuePoint)
				{
					branch.Type = ControlFlowBranchType.Continue;
				}
			}

			block.Children.ForEach(AnalyzeBranches);
		}

		private void FlattenBlocks(ControlFlowBlock block, ControlFlowData data)
		{
			data.AddBlock(block);

			foreach (var branch in block.Branches.Values)
			{
				data.AddBranch(branch);
			}

			block.Children.ForEach(child => FlattenBlocks(child, data));
		}
	}
}
