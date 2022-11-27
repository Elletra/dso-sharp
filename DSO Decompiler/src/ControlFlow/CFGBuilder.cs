using System.Collections.Generic;
using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class CFGBuilder
	{
		protected InstructionGraph insnGraph = null;
		protected ControlFlowGraph cfg = null;

		public ControlFlowGraph Build (InstructionGraph graph)
		{
			insnGraph = graph;
			cfg = new ControlFlowGraph ();

			TraverseInstructions ();

			return cfg;
		}

		// We can't use the `PreorderDFS()` method because we need to maintain data between node visits.
		// It's also iterative to prevent stack overflows on large files. Enjoy!
		protected void TraverseInstructions ()
		{
			var stack = new Stack<(Instruction, ControlFlowNode)> ();
			var visited = new HashSet<Instruction> ();

			stack.Push ((insnGraph.EntryPoint, null));

			while (stack.Count > 0)
			{
				var (instruction, node) = stack.Pop ();

				if (visited.Contains (instruction))
				{
					continue;
				}

				visited.Add (instruction);

				if (node == null)
				{
					node = cfg.CreateOrGet (instruction.Addr);
				}
				else if (IsJumpTarget (instruction))
				{
					var newNode = cfg.CreateOrGet (instruction.Addr);

					if (node != null && node != newNode)
					{
						node.AddEdgeTo (newNode);
					}

					node = newNode;
				}

				node.Instructions.Add (instruction);

				var isNodeEnd = IsControlFlowNodeEnd (instruction);

				foreach (Instruction successor in instruction.Successors)
				{
					if (isNodeEnd)
					{
						node.AddEdgeTo (cfg.CreateOrGet (successor.Addr));
					}
				}

				for (var i = instruction.Successors.Count - 1; i >= 0; i--)
				{
					Instruction successor = (Instruction) instruction.Successors[i];

					if (isNodeEnd)
					{
						var newNode = cfg.CreateOrGet (successor.Addr);

						node.AddEdgeTo (newNode);
						stack.Push ((successor, newNode));
					}
					else
					{
						stack.Push ((successor, node));
					}
				}
			}
		}

		protected bool IsJumpTarget (Instruction instruction)
		{
			foreach (Instruction predecessor in instruction.Predecessors)
			{
				if (predecessor is JumpInsn)
				{
					return true;
				}
			}

			return false;
		}

		protected bool IsControlFlowNodeEnd (Instruction instruction)
		{
			return instruction is JumpInsn || instruction is FuncDeclInsn || instruction is ReturnInsn;
		}
	}
}
