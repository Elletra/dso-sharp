using System;
using System.Collections.Generic;

namespace DSODecompiler.ControlFlow
{
	/// <summary>
	/// Calculates the immediate dominators of nodes in a control flow graph (CFG).<br />
	/// <br />
	/// A node D is said to dominate a node N if all paths from the entry point must go through
	/// D to get to N.<br />
	/// <br />
	/// Nodes can have multiple dominators, but we only care about the immediate dominator, which
	/// is the dominator that all other dominators of a node have to go through get to said node.
	/// </summary>
	public class DominanceCalculator
	{
		public class DominanceCalculatorException : Exception
		{
			public DominanceCalculatorException () {}
			public DominanceCalculatorException (string message) : base (message) {}
			public DominanceCalculatorException (string message, Exception inner) : base (message, inner) {}
		}

		/// <summary>
		/// Calculates the immediate dominator of each node in `cfg`.<br />
		/// <br />
		/// NOTE: This function mutates the `postorder` and `immediate_dom` fields of CFG nodes.<br />
		/// <br />
		/// "A Simple, Fast Dominance Algorithm" by Keith Cooper, Timothy Harvey, and Ken Kennedy:<br />
		/// https://www.cs.rice.edu/~keith/EMBED/dom.pdf
		/// </summary>
		/// <param name="graph"></param>
		public static void CalculateDominators (ControlFlowGraph graph)
		{
			CalculatePostorder (graph);

			var entryPoint = graph.EntryPoint;

			// Temporarily set this so the algorithm works.
			entryPoint.ImmediateDom = entryPoint;

			var nodes = BuildNodeArray (graph);
			var changed = true;

			while (changed)
			{
				changed = false;

				// Iterate over the nodes (except the entry point) in reverse postorder.
				for (var i = nodes.Length - 2; i >= 0; i--)
				{
					var node = nodes[i];

					ControlFlowGraph.Node newIDom = null;

					var predecessors = new HashSet <ControlFlowGraph.Node> ();

					/* Find first predecessor whose dominator has been calculated. */

					foreach (var pred in node.Predecessors)
					{
						if (newIDom == null && pred.ImmediateDom != null)
						{
							newIDom = pred;
						}
						else
						{
							predecessors.Add (pred);
						}
					}

					if (newIDom == null)
					{
						throw new DominanceCalculatorException ($"Could not find predecessor of {node.Postorder}");
					}

					/* Calculate new immediate dominator. */

					foreach (var pred in predecessors)
					{
						if (pred.ImmediateDom != null)
						{
							newIDom = FindCommonDominator (pred, newIDom);
						}
					}

					if (node.ImmediateDom != newIDom)
					{
						node.ImmediateDom = newIDom;
						changed = true;
					}
				}
			}

			// Set immediate dominator back to `null` because a node cannot be the immediate
			// dominator of itself, even the entry point.
			entryPoint.ImmediateDom = null;
		}

		/// <summary>
		/// Finds loops based on back edges, which are defined as CFG nodes jumping back to one of
		/// their dominators.<br />
		/// <br />
		/// NOTE: This function mutates the `IsLoopEnd` property of CFG nodes.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static uint FindLoops (ControlFlowGraph graph)
		{
			uint numLoops = 0;

			graph.PostorderDFS ((ControlFlowGraph.Node node) =>
			{
				if (node.ImmediateDom == null && node != graph.EntryPoint)
				{
					throw new DominanceCalculatorException (
						"Immediate is null! Make sure you call CalculateDominators() before FindLoops()"
					);
				}

				var last = node.LastInstruction;

				if (!Opcodes.IsJump (last.Op))
				{
					return;
				}

				var jumpTarget = last.Operands[0];

				if (graph.GetNode (jumpTarget).Dominates (node))
				{
					if (jumpTarget > node.Addr)
					{
						// Back edge somehow jumps forward??
						throw new DominanceCalculatorException (
							$"Node at {jumpTarget} dominates earlier node at {node.Addr}"
						);
					}

					node.IsLoopEnd = true;
					numLoops++;
				}
			});

			return numLoops;
		}

		/// <summary>
		/// "A Simple, Fast Dominance Algorithm" by Keith Cooper, Timothy Harvey, and Ken Kennedy:<br />
		/// https://www.cs.rice.edu/~keith/EMBED/dom.pdf
		/// </summary>
		/// <param name="node1"></param>
		/// <param name="node2"></param>
		/// <returns></returns>
		protected static ControlFlowGraph.Node FindCommonDominator (ControlFlowGraph.Node node1, ControlFlowGraph.Node node2)
		{
			var finger1 = node1;
			var finger2 = node2;

			while (finger1 != finger2)
			{
				while (finger1.Postorder < finger2.Postorder)
				{
					finger1 = finger1.ImmediateDom;
				}

				while (finger2.Postorder < finger1.Postorder)
				{
					finger2 = finger2.ImmediateDom;
				}
			}

			return finger1;
		}

		protected static void CalculatePostorder (ControlFlowGraph graph)
		{
			var postorder = 0;

			graph.PostorderDFS ((ControlFlowGraph.Node node) =>
			{
				node.Postorder = postorder++;
			});
		}

		/// <summary>
		/// Builds an array of nodes indexed by their postorder value.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		protected static ControlFlowGraph.Node[] BuildNodeArray (ControlFlowGraph graph)
		{
			var nodes = new ControlFlowGraph.Node[graph.NodeCount];

			graph.PostorderDFS ((ControlFlowGraph.Node node) =>
			{
				nodes[node.Postorder] = node;
			});

			return nodes;
		}
	}
}
