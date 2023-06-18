using System;

namespace DSODecompiler.ControlFlow
{
	/// <summary>
	/// Performs structural analysis on a control flow graph to recover control flow structures (e.g.
	/// if statements, loops, etc.) from it.<br/><br/>
	///
	/// This doesn't implement everything described in the Schwartz paper, but it works for our current
	/// target, which is normal DSO files produced by the Torque Game Engine.<br/><br/>
	///
	/// <strong>Source:</strong><br/><br/>
	///
	/// <see href="https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf">
	/// "Native x86 Decompilation Using Semantics-Preserving Structural Analysis and Iterative
	/// Control-Flow Structuring"</see> by Edward J. Schwartz, JongHyup Lee, Maverick Woo, and David Brumley.
	/// </summary>
	public class StructureAnalyzer
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base(message) {}
			public Exception (string message, Exception inner) : base(message, inner) {}
		}

		protected ControlFlowGraph graph = null;

		public CollapsedNode Analyze (ControlFlowGraph cfg)
		{
			graph = cfg;

			while (cfg.Count > 1)
			{
				foreach (ControlFlowNode node in graph.PostorderDFS(graph.EntryPoint))
				{
					var reduced = true;

					while (reduced)
					{
						reduced = ReduceNode(node);
					}
				}
			}

			// !!! FIXME: This is a hacky way to set the entry point to the remaining node !!!
			foreach (ControlFlowNode node in cfg.GetNodes())
			{
				cfg.EntryPoint = node.Addr;
				break;
			}

			// !!! FIXME: Also very hacky !!!
			return cfg.GetNode(cfg.EntryPoint).CollapsedNode;
		}

		protected bool ReduceNode (ControlFlowNode node)
		{
			return ReduceAcyclic(node);
		}

		protected bool ReduceAcyclic (ControlFlowNode node)
		{
			switch (node.Successors.Count)
			{
				case 0:
					return false;

				case 1:
					return ReduceSequence(node);

				case 2:
					return ReduceConditional(node);

				default:
					throw new Exception($"Node {node.Addr} has more than 2 successors");
			}
		}

		protected bool ReduceSequence (ControlFlowNode node)
		{
			var next = node.GetSuccessor(0);

			if (next.Predecessors.Count > 1 || next.Successors.Count > 1)
			{
				return false;
			}

			AddSequenceNode(node, next);

			if (next.Successors.Count > 0)
			{
				node.AddEdgeTo(next.Successors[0]);
			}

			graph.RemoveNode(next);

			return true;
		}

		protected bool ReduceConditional (ControlFlowNode node)
		{
			var reduced = false;

			ControlFlowNode then = node.GetSuccessor(0);
			ControlFlowNode @else = node.GetSuccessor(1);
			ControlFlowNode thenSuccessor = then.GetSuccessor(0);
			ControlFlowNode elseSuccessor = @else.GetSuccessor(0);

			if (thenSuccessor == @else)
			{
				/* if-then */

				if (then.Predecessors.Count <= 1)
				{
					AddConditionalNode(node, then, null);

					graph.RemoveNode(then);

					reduced = true;
				}
			}
			else if (elseSuccessor != null && thenSuccessor == elseSuccessor)
			{
				/* if-then-else */

				if (then.Predecessors.Count <= 1 && @else.Predecessors.Count <= 1)
				{
					AddConditionalNode(node, then, @else);

					graph.RemoveNode(then);
					graph.RemoveNode(@else);
					graph.AddEdge(node, thenSuccessor);

					reduced = true;
				}
			}
			else
			{
				Console.WriteLine($"{node.Addr} :: <FAILED>");
			}

			return reduced;
		}

		protected SequenceNode AddSequenceNode (ControlFlowNode node, ControlFlowNode next)
		{
			var collapsed = new SequenceNode(node.Addr);

			collapsed.AddNodes(node.CollapsedNode, next.CollapsedNode);

			return (node.CollapsedNode = collapsed) as SequenceNode;
		}

		protected ConditionalNode AddConditionalNode (ControlFlowNode node, ControlFlowNode then, ControlFlowNode @else)
		{
			var collapsed = new ConditionalNode(node.Addr)
			{
				Then = then?.CollapsedNode,
				Else = @else?.CollapsedNode,
			};

			if (node.CollapsedNode is not InstructionNode instructions)
			{
				throw new Exception($"Expected {node.Addr}.CollapsedNode to be `InstructionNode`");
			}

			collapsed.CopyInstructions(instructions);

			return (node.CollapsedNode = collapsed) as ConditionalNode;
		}

		public static void PrintIndent (int indent)
		{
			for (var i = 0; i < indent; i++)
			{
				Console.Write("\t");
			}
		}

		public static void PrintNode (CollapsedNode collapsed, int indent)
		{
			if (indent == 0)
			{
				Console.WriteLine("\n================\n");
			}

			PrintIndent(indent);

			if (collapsed == null)
			{
				Console.WriteLine("<NULL>");
				return;
			}

			Console.WriteLine($"+ [{collapsed.Addr}] {collapsed}");
			PrintIndent(indent);

			switch (collapsed)
			{
				case InstructionNode node:
				{
					PrintIndent(indent - 1);

					Console.WriteLine("> Instructions:");

					foreach (var insn in node.Instructions)
					{
						PrintIndent(indent + 1);
						Console.WriteLine(insn);
					}

					Console.WriteLine("");

					if (collapsed is ConditionalNode cond)
					{
						PrintIndent(indent);
						Console.WriteLine("> Then:");
						PrintNode(cond.Then, indent + 1);

						PrintIndent(indent);
						Console.WriteLine("> Else:");
						PrintNode(cond.Else, indent + 1);
					}
					else if (collapsed is LoopNode loop)
					{
						PrintIndent(indent);
						Console.WriteLine("> Body:");

						foreach (var child in loop.Body)
						{
							PrintNode(child, indent + 1);
						}
					}

					break;
				}

				case SequenceNode node:
				{
					PrintIndent(indent - 1);

					Console.WriteLine("> Nodes:");

					foreach (var child in node.Nodes)
					{
						PrintNode(child, indent + 1);
					}

					break;
				}
			}
		}
	}
}
