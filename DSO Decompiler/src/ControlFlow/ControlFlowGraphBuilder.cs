using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowGraphBuilder
	{
		protected ControlFlowGraph cfg = null;
		protected ControlFlowNode currNode = null;
		protected Disassembly disassembly = null;

		public ControlFlowGraph Build (Disassembly disasm)
		{
			cfg = new ControlFlowGraph();
			disassembly = disasm;

			BuildInitialGraph();
			ConnectBranches();

			return cfg;
		}

		protected void BuildInitialGraph ()
		{
			foreach (var instruction in disassembly)
			{
				if (currNode == null || IsBlockStart(instruction) || IsBlockEnd(currNode.LastInstruction))
				{
					CreateAndConnect(instruction);
				}

				currNode.Instructions.Add(instruction);
			}
		}

		protected void CreateAndConnect (Instruction instruction)
		{
			var node = cfg.AddOrGet(instruction.Addr);

			if (currNode != null && ShouldConnectToNext(currNode.LastInstruction))
			{
				cfg.AddEdge(currNode, node);
			}

			if (instruction is FunctionInstruction func && func.HasBody)
			{
				cfg.AddEdge(node, cfg.AddOrGet(func.EndAddr));
			}

			currNode = node;
		}

		protected bool IsBlockStart (Instruction instruction)
		{
			return disassembly.HasBranchTarget(instruction.Addr) || disassembly.HasFunctionEnd(instruction.Addr);
		}

		protected bool IsBlockEnd (Instruction instruction)
		{
			switch (instruction)
			{
				/* For most CFG implementations, return statements also end blocks, but for our purposes they don't... */
				case FunctionInstruction:
				case BranchInstruction:
				{
					return true;
				}

				default:
				{
					return false;
				}
			}
		}

		protected bool ShouldConnectToNext (Instruction instruction)
		{
			return instruction is not BranchInstruction branch || !branch.IsUnconditional;
		}

		/**
		 * We do this in a separate function/pass because we want branch targets to come after the
		 * adjacent nodes, and it's much simpler to just do it in a second pass.
		 */
		protected void ConnectBranches ()
		{
			foreach (var node in cfg)
			{
				if (node.LastInstruction is BranchInstruction branch)
				{
					cfg.AddEdge(node, cfg.AddOrGet(branch.TargetAddr));
				}
			}
		}
	}
}
