using System.Linq;
using System.Collections.Generic;

namespace DSODecompiler.ControlFlow
{
	public class Loop
	{
		public ControlFlowNode Header { get; set; } = null;
		public ControlFlowNode End => Body.Count > 0 ? Body[^1] : Header;

		public int Count => Body.Count;

		protected List<ControlFlowNode> Body = new();

		/// <summary>
		/// For quick lookup.
		/// </summary>
		protected HashSet<ControlFlowNode> Nodes = new();

		public void AddNode (ControlFlowNode node)
		{
			if (!Nodes.Contains(node))
			{
				Body.Add(node);
				Nodes.Add(node);
			}
		}

		public bool HasNode (ControlFlowNode node) => Nodes.Contains(node);

		public List<ControlFlowNode> GetBody () => new(Body);
	}

	public class LoopFinder
	{
		public List<Loop> Find (ControlFlowNode header)
		{
			var list = new List<Loop>();

			foreach (ControlFlowNode predecessor in header?.Predecessors)
			{
				if (header.Dominates(predecessor, strictly: false))
				{
					list.Add(FindSingleLoop(header, predecessor));
				}
			}

			// Sort by body size so that nested loops are ordered from innermost to outermost.
			list.Sort((loop1, loop2) => loop1.Count.CompareTo(loop2.Count));

			return list;
		}

		protected Loop FindSingleLoop (ControlFlowNode header, ControlFlowNode end)
		{
			var loop = new Loop();
			var visited = new HashSet<ControlFlowNode>();
			var queue = new Queue<ControlFlowNode>();

			loop.Header = header;

			queue.Enqueue(end);

			while (queue.Count > 0)
			{
				var node = queue.Dequeue();

				if (!visited.Contains(node) && node != header)
				{
					loop.AddNode(node);

					foreach (ControlFlowNode predecessor in node.Predecessors)
					{
						if (header.Dominates(predecessor, strictly: true))
						{
							queue.Enqueue(predecessor);
						}
					}

					visited.Add(node);
				}
			}

			return loop;
		}

		public bool IsLoopStart (ControlFlowNode node)
		{
			return node.Predecessors.Any(pred =>
			{
				ControlFlowNode predecessor = pred as ControlFlowNode;

				return node.Dominates(predecessor, strictly: false);
			});
		}

		public bool IsLoopEnd (ControlFlowNode node)
		{
			return node.Successors.Any(succ =>
			{
				ControlFlowNode successor = succ as ControlFlowNode;

				return successor.Dominates(node, strictly: false);
			});
		}

		public bool IsLoopNode (ControlFlowNode node) => IsLoopStart(node) || IsLoopEnd(node);
	}
}
