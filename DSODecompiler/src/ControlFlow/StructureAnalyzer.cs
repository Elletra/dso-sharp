using System;
using System.Collections.Generic;

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
	/// <see href="https://www.ndss-symposium.org/wp-content/uploads/2017/09/11_4_2.pdf">
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

		protected ControlFlowGraph graph;
		protected Dictionary<uint, CollapsedNode> collapsedNodes;

		public CollapsedNode Analyze (ControlFlowGraph cfg)
		{
			graph = cfg;
			collapsedNodes = new();

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

			return collapsedNodes[cfg.EntryPoint];
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

		protected bool ReduceSequence (ControlFlowNode node)
		{
			var next = node.GetSuccessor(0);

			if (!next.IsSequential)
			{
				return false;
			}

			node.AddEdgeTo(next.GetSuccessor(0));

			var sequence = new SequenceNode(node.Addr);

			if (collapsedNodes.ContainsKey(node.Addr))
			{
				sequence.AddNode(collapsedNodes[node.Addr]);
			}

			sequence.AddNode(ExtractCollapsed(next) ?? new InstructionNode(next));

			collapsedNodes[node.Addr] = sequence;

			return true;
		}

		protected bool ReduceConditional (ControlFlowNode node)
		{
			var reduced = false;
			var then = node.GetSuccessor(0);
			var @else = node.GetSuccessor(1);
			var thenSuccessor = then.GetSuccessor(0);

			if (thenSuccessor == @else)
			{
				if (then.IsSequential)
				{
					collapsedNodes[node.Addr] = new ConditionalNode(node)
					{
						Then = ExtractCollapsed(then) ?? new InstructionNode(then),
					};

					reduced = true;
				}
			}

			return reduced;
		}

		protected CollapsedNode ExtractCollapsed (uint key)
		{
			CollapsedNode node = null;

			if (collapsedNodes.ContainsKey(key))
			{
				node = collapsedNodes[key];
				collapsedNodes.Remove(key);
			}

			return node;
		}

		protected CollapsedNode ExtractCollapsed (ControlFlowNode node)
		{
			graph.RemoveNode(node);

			return ExtractCollapsed(node.Addr);
		}
	}
}
