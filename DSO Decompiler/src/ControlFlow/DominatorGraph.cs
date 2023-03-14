using System;
using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	/// <summary>
	/// Calculates the immediate dominators of nodes in a control flow graph.<br /><br />
	///
	/// A node D is said to dominate a node N if all paths from the entry point must go through D
	/// to get to N.<br /><br />
	///
	/// Nodes can have multiple dominators, but we only care about the immediate dominator, which
	/// is the very last dominator before the node itself.
	/// </summary>
	public class DominatorGraph
	{
		public class DominatorGraphException : Exception
		{
			public DominatorGraphException () {}
			public DominatorGraphException (string message) : base(message) {}
			public DominatorGraphException (string message, Exception inner) : base(message, inner) {}
		}

		private readonly ControlFlowGraph cfg = null;

		private readonly Dictionary<ControlFlowNode, int> reversePostorder = new();
		private readonly Dictionary<ControlFlowNode, ControlFlowNode> immediateDoms = new();

		public DominatorGraph (ControlFlowGraph graph)
		{
			cfg = graph;

			Build();
		}

		public ControlFlowNode ImmediateDom (ControlFlowNode node)
		{
			return immediateDoms.ContainsKey(node) ? immediateDoms[node] : null;
		}

		/// <summary>
		/// Checks if `node1` dominates `node2`.
		/// </summary>
		/// <param name="node1"></param>
		/// <param name="node2"></param>
		/// <param name="strictly">
		/// Whether we're checking for strict domination (i.e. A node cannot strictly dominate itself).
		/// </param>
		/// <returns></returns>
		public bool Dominates (ControlFlowNode node1, ControlFlowNode node2, bool strictly = false)
		{
			// All nodes dominate themselves, but not strictly.
			if (node1 == node2)
			{
				return !strictly;
			}

			var dom = ImmediateDom(node2);

			while (dom != node1 && dom != null && dom != ImmediateDom(dom))
			{
				dom = ImmediateDom(dom);
			}

			return dom == node1;
		}

		/// <summary>
		/// Finds loop bounds and adds their address to a hash set.
		/// </summary>
		/// <param name="graph"></param>
		/// <returns>Addresses of loop nodes.</returns>
		public HashSet<uint> FindLoops ()
		{
			var loopEnds = new HashSet<uint>();

			foreach (var node in cfg.PreorderDFS())
			{
				if (node.LastInstruction is not BranchInstruction branch)
				{
					continue;
				}

				if (!cfg.Has(branch.TargetAddr))
				{
					throw new DominatorGraphException($"Control flow graph does not contain node at branch target {branch.TargetAddr}");
				}

				var target = cfg.Get(branch.TargetAddr);

				// Is this a loop?
				if (Dominates(target, node))
				{
					if (target.Addr > node.Addr)
					{
						// Back edge somehow jumps forward??
						throw new DominatorGraphException($"Node at {target.Addr} dominates earlier node at {node.Addr}");
					}

					loopEnds.Add(node.Addr);
				}
			}

			return loopEnds;
		}

		/// <summary>
		/// Calculates the immediate dominator of every node in `cfg`.<br /><br />
		///
		/// "A Simple, Fast Dominance Algorithm" by Keith Cooper, Timothy Harvey, and Ken Kennedy:<br />
		/// https://www.cs.rice.edu/~keith/EMBED/dom.pdf
		/// </summary>
		private void Build ()
		{
			BuildReversePostorder();

			var entry = cfg.EntryPoint;
			var nodes = new SortedList<int, ControlFlowNode>();

			foreach (var (node, order) in reversePostorder)
			{
				immediateDoms[node] = null;

				nodes.Add(order, node);
			}

			// Temporarily set this so the algorithm works.
			immediateDoms[entry] = entry;

			var changed = true;

			while (changed)
			{
				changed = false;

				foreach (var node in nodes.Values)
				{
					if (node == entry)
					{
						continue;
					}

					ControlFlowNode newIDom = null;

					/* Find first predecessor whose dominator has been calculated. */

					var predecessors = new HashSet<ControlFlowNode>();

					foreach (ControlFlowNode pred in node.Predecessors)
					{
						if (newIDom == null && ImmediateDom(pred) != null)
						{
							newIDom = pred;
						}
						else
						{
							predecessors.Add(pred);
						}
					}

					if (newIDom == null)
					{
						throw new DominatorGraphException($"Could not find new immediate dominator for node at {node.Addr}");
					}

					/* Calculate new immediate dominator. */

					foreach (var pred in predecessors)
					{
						if (ImmediateDom(pred) != null)
						{
							newIDom = FindCommonDominator(pred, newIDom);
						}
					}

					if (ImmediateDom(node) != newIDom)
					{
						immediateDoms[node] = newIDom;
						changed = true;
					}
				}
			}

			// Set immediate dominator back to `null` because a node cannot be the immediate dominator
			// of itself.
			immediateDoms[entry] = null;
		}

		/// <summary>
		/// "A Simple, Fast Dominance Algorithm" by Keith Cooper, Timothy Harvey, and Ken Kennedy:<br />
		/// https://www.cs.rice.edu/~keith/EMBED/dom.pdf
		/// </summary>
		/// <param name="node1"></param>
		/// <param name="node2"></param>
		/// <returns></returns>
		private ControlFlowNode FindCommonDominator (ControlFlowNode node1, ControlFlowNode node2)
		{
			var finger1 = node1;
			var finger2 = node2;

			while (finger1 != finger2)
			{
				while (reversePostorder[finger1] > reversePostorder[finger2])
				{
					finger1 = ImmediateDom(finger1);
				}

				while (reversePostorder[finger2] > reversePostorder[finger1])
				{
					finger2 = ImmediateDom(finger2);
				}
			}

			return finger1;
		}

		private void BuildReversePostorder ()
		{
			var order = cfg.Count - 1;

			foreach (var node in cfg.PostorderDFS())
			{
				reversePostorder[node] = order--;
			}
		}
	}
}
