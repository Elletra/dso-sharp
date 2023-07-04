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
	/// <strong>Sources:</strong><br/><br/>
	///
	/// <list type="number">
	/// <item>
	/// <see href="https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf">
	/// "Native x86 Decompilation Using Semantics-Preserving Structural Analysis and Iterative
	/// Control-Flow Structuring"</see> by Edward J. Schwartz, JongHyup Lee, Maverick Woo, and David Brumley.
	/// </item>
	///
	/// <item>
	/// <see href="https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf">
	/// "No More Gotos: Decompilation Using Pattern-Independent Control-Flow Structuring and
	/// Semantics-Preserving Transformations"</see> by Khaled Yakdan, Sebastian Eschweiler,
	/// Elmar Gerhards-Padilla, Matthew Smith.
	/// </item>
	/// </list>
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
			return null;//cfg.GetNode(cfg.EntryPoint).CollapsedNode;
		}

		protected bool ReduceNode (ControlFlowNode node)
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

		protected bool ReduceSequence(ControlFlowNode node)
		{
			return false;
		}

		protected bool ReduceConditional (ControlFlowNode node)
		{
			return false;
		}
	}
}
