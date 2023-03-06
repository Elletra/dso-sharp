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
			var instructions = disassembly.GetInstructions();

			foreach (var instruction in instructions)
			{
				ControlFlowNode newNode = null;

				if (!instruction.HasPrev)
				{
					currNode = cfg.AddOrGet(instruction.Addr);
				}
				else if (IsControlBlockStart(instruction, disassembly) || (currNode != null && IsControlBlockEnd(currNode.LastInstruction)))
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
		}

		protected void ConnectBranches ()
		{
			cfg.Iterate(ConnectBranch);
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
					throw new Exception($"Invalid branch from {branch.Addr} to {branch.TargetAddr}");
				}
			}
		}
	}
}
