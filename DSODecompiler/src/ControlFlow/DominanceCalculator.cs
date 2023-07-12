using System.Collections.Generic;

namespace DSODecompiler.ControlFlow
{
	/// <summary>
	/// Calculates the immediate dominators of nodes in a control flow graph.<br/><br/>
	///
	/// A node D is said to dominate a node N if all paths from the entry point must go through D
	/// to get to N.<br/><br/>
	///
	/// Nodes can have multiple dominators, but we only care about the immediate dominator, which
	/// is the very last dominator before the node itself.<br/><br/>
	///
	/// <strong>Source:</strong><br/><br/>
	///
	/// <see href="https://www.cs.rice.edu/~keith/EMBED/dom.pdf">"A Simple, Fast Dominance Algorithm"</see>
	/// by Keith Cooper, Timothy Harvey, and Ken Kennedy.
	/// </summary>
	public class DominanceCalculator
	{
		protected List<ControlFlowNode> reversePostorder;
		protected ControlFlowGraph graph;

		public void Calculate (ControlFlowGraph cfg)
		{
			graph = cfg;

			CalculateReversePostorder();
			Calculate();
		}

		protected void Calculate ()
		{
			var entry = graph.GetEntryPoint();
			var changed = true;

			entry.ImmediateDom = entry;

			while (changed)
			{
				changed = false;

				foreach (var node in reversePostorder)
				{
					if (node == entry)
					{
						continue;
					}

					ControlFlowNode newIDom = null;
					
					foreach (ControlFlowNode predecessor in node.Predecessors)
					{
						// Ignore predecessors that haven't been processed yet. Since we set the
						// entry point's immediate dominator to itself, we will always have an
						// available predecessor.
						if (predecessor.ImmediateDom != null)
						{
							if (newIDom == null)
							{
								newIDom = predecessor;
							}
							else
							{
								newIDom = FindCommonDominator(predecessor, newIDom);
							}
						}
					}

					if (node.ImmediateDom != newIDom)
					{
						node.ImmediateDom = newIDom;
						changed = true;
					}
				}
			}
		}

		protected ControlFlowNode FindCommonDominator (ControlFlowNode node1, ControlFlowNode node2)
		{
			var finger1 = node1;
			var finger2 = node2;

			while (finger1 != finger2)
			{
				/* Comparison operators are flipped since we're using reverse postorder values. */

				while (finger1.ReversePostorder > finger2.ReversePostorder)
				{
					finger1 = finger1.ImmediateDom;
				}

				while (finger2.ReversePostorder > finger1.ReversePostorder)
				{
					finger2 = finger2.ImmediateDom;
				}
			}

			return finger1;
		}

		protected void CalculateReversePostorder ()
		{
			reversePostorder = new();

			foreach (ControlFlowNode node in graph.PostorderDFS())
			{
				reversePostorder.Add(node);
			}

			var index = 0;

			reversePostorder.Reverse();
			reversePostorder.ForEach((ControlFlowNode node) => node.ReversePostorder = index++);
		}
	}
}
