using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class CFGBuilder
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base(message) {}
			public Exception (string message, System.Exception inner) : base(message, inner) {}
		}

		protected ControlFlowGraph cfg = null;
		protected ControlFlowNode currNode = null;

		public ControlFlowGraph Build (Disassembly disassembly)
		{
			cfg = new();
			currNode = null;

			BuildInitialGraph(disassembly);
			ConnectBranches();

			return cfg;
		}

		protected void BuildInitialGraph (Disassembly disassembly)
		{
			new DisassemblyTraverser().Traverse(disassembly, Visit, TraverseFrom);
		}

		protected void ConnectBranches ()
		{
			cfg.Iterate(ConnectBranch);
		}

		protected void Visit (Instruction instruction, Disassembly disassembly)
		{
			ControlFlowNode newNode = null;

			if (IsControlBlockStart(instruction, disassembly) || IsControlBlockEnd(currNode.LastInstruction))
			{
				newNode = cfg.AddOrGet(instruction.Addr);
			}

			if (newNode != null)
			{
				if (currNode != null)
				{
					cfg.Connect(currNode.Addr, newNode.Addr);
				}

				currNode = newNode;
			}

			currNode.LastInstruction = instruction;
		}

		protected void TraverseFrom (Instruction instruction, Disassembly disassembly)
		{
			currNode = cfg.AddOrGet(instruction.Addr);
		}

		protected bool IsControlBlockStart (Instruction instruction, Disassembly disassembly)
		{
			return disassembly.IsBranchTarget(instruction.Addr);
		}

		protected bool IsControlBlockEnd (Instruction instruction)
		{
			return instruction is BranchInsn || instruction is FuncDeclInsn || instruction is ReturnInsn;
		}

		protected void ConnectBranch (ControlFlowNode node)
		{
			if (node.LastInstruction is BranchInsn branch)
			{
				if (!cfg.Connect(node.Addr, branch.TargetAddr))
				{
					throw new Exception($"Invalid branch address {branch.TargetAddr}");
				}
			}
		}
	}
}
