using System.Collections.Generic;
using DSODecompiler.Disassembler;

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
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base (message) {}
			public Exception (string message, System.Exception inner) : base (message, inner) {}
		}

		/// <summary>
		/// Calculates the immediate dominator of each node in `cfg`.<br />
		/// <br />
		/// NOTE: This function mutates the `Postorder` and `ImmediateDom` properties of CFG nodes.<br />
		/// <br />
		/// "A Simple, Fast Dominance Algorithm" by Keith Cooper, Timothy Harvey, and Ken Kennedy:<br />
		/// https://www.cs.rice.edu/~keith/EMBED/dom.pdf
		/// </summary>
		/// <param name="graph"></param>
		public static void CalculateDominators (ControlFlowGraph graph)
		{
			var nodes = CalculatePostorder (graph);
			var entryPoint = graph.EntryPoint;

			// Temporarily set this so the algorithm works.
			entryPoint.ImmediateDom = entryPoint;

			var changed = true;

			while (changed)
			{
				changed = false;

				// Iterate over the nodes (except the entry point) in reverse postorder.
				for (var i = nodes.Length - 2; i >= 0; i--)
				{
					var node = nodes[i];

					ControlFlowNode newIDom = null;

					var predecessors = new HashSet <ControlFlowNode> ();

					/* Find first predecessor whose dominator has been calculated. */

					foreach (ControlFlowNode pred in node.Predecessors)
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
						throw new Exception ($"Could not find predecessor of {node.Postorder}");
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
			// dominator of itself.
			entryPoint.ImmediateDom = null;
		}

		/// <summary>
		/// Finds loop ends and marks them by changing `IsLoopStart` and `IsLoopEnd` properties.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns>Number of loops found.</returns>
		public static uint FindLoops (ControlFlowGraph graph)
		{
			uint numLoops = 0;

			graph.PreorderDFS ((ControlFlowNode node) =>
			{
				if (node.ImmediateDom == null && node != graph.EntryPoint)
				{
					throw new Exception ("Immediate dominator is null! Make sure you call CalculateDominators() before FindLoops()");
				}

				var last = node.LastInstruction;

				if (last is not JumpInsn jump)
				{
					return;
				}

				var target = graph.Get (jump.TargetAddr);

				// Is this a loop?
				if (target.Dominates (node))
				{
					if (target.Addr > node.Addr)
					{
						// Back edge somehow jumps forward??
						throw new Exception ($"Node at {target.Addr} dominates earlier node at {node.Addr}");
					}

					target.IsLoopStart = true;
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
		protected static ControlFlowNode FindCommonDominator (ControlFlowNode node1, ControlFlowNode node2)
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

		/// <summary>
		/// Calculates the postorder of nodes in the given CFG and returns an array of the nodes
		/// indexed by their postorder value.<br />
		/// <br />
		/// NOTE: This function modifies the `Postorder` property of CFG nodes.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		protected static ControlFlowNode[] CalculatePostorder (ControlFlowGraph graph)
		{
			var nodes = new ControlFlowNode[graph.Count];
			var postorder = 0;

			graph.PostorderDFS ((ControlFlowNode node) =>
			{
				node.Postorder = postorder++;
				nodes[node.Postorder] = node;
			});

			return nodes;
		}
	}
}
