using System.Collections.Generic;
using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class CFGBuilder
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base (message) {}
			public Exception (string message, System.Exception inner) : base (message, inner) {}
		}

		protected Disassembly disassembly = null;
		protected ControlFlowGraph cfg = null;

		public ControlFlowGraph Build (Disassembly disasm)
		{
			disassembly = disasm;
			cfg = new ControlFlowGraph ();

			BuildInitialGraph ();
			ConnectJumps ();

			return cfg;
		}

		protected void BuildInitialGraph ()
		{
			var instruction = disassembly.EntryPoint;
			var node = cfg.CreateOrGet (instruction.Addr);

			while (instruction != null)
			{
				var prevNode = node;

				if (IsJumpTarget (instruction))
				{
					node = cfg.CreateOrGet (instruction.Addr);
					ConnectToPrevious (node, prevNode);
				}

				node.Instructions.Add (instruction);

				if (IsControlFlowNodeEnd (instruction) && instruction.Next != null)
				{
					node = cfg.CreateOrGet (instruction.Next.Addr);
					ConnectToPrevious (node, prevNode);
				}

				instruction = instruction.Next;
			}
		}

		protected void ConnectToPrevious (ControlFlowNode node, ControlFlowNode prevNode)
		{
			if (prevNode != null && prevNode != node)
			{
				prevNode.AddEdgeTo (node);
			}
		}

		// The reason we connect the jumps afterward is that the next instruction is always the
		// first successor and the jump target is always the second.
		//
		// It's easier and less complicated to do it after the fact.
		protected void ConnectJumps ()
		{
			var nodes = cfg.GetNodes ();

			foreach (var node in nodes)
			{
				var last = node.LastInstruction;

				if (last is JumpInsn jump)
				{
					/* I know I made a big deal in BytecodeDisassembler about having it support
					   jumps that jump to the middle of an instruction, but frankly, figuring out
					   how to support that for everything else is not something I'm interested in
					   doing.

					   And, frankly, no one cares enough to do something like that except maybe me,
					   so I'm not going to bother.

					   Consider this a tentative TODO: Maybe. */
					if (!cfg.Has (jump.TargetAddr))
					{
						throw new Exception ($"Jump to address {jump.TargetAddr} that does not have a CFG node");
					}

					node.AddEdgeTo (cfg.Get (jump.TargetAddr));
				}
			}
		}

		protected bool IsJumpTarget (Instruction instruction) => instruction.IsJumpTarget;

		protected bool IsControlFlowNodeEnd (Instruction instruction)
		{
			return instruction is JumpInsn || instruction is FuncDeclInsn || instruction is ReturnInsn;
		}
	}
}
