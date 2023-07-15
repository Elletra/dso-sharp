using System.Linq;
using System.Collections.Generic;

namespace DSODecompiler.ControlFlow
{
	public class Loop
	{
		public ControlFlowNode Header => Body.Count > 0 ? Body[0] : null;
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

			// Sort by address so nested loops are ordered from innermost to outermost.
			list.Sort((loop1, loop2) => loop1.Header.Addr.CompareTo(loop2.Header.Addr));

			return list;
		}

		protected Loop FindSingleLoop (ControlFlowNode header, ControlFlowNode end)
		{
			var loop = new Loop();
			var visited = new HashSet<ControlFlowNode>();
			var queue = new Queue<ControlFlowNode>();

			loop.AddNode(header);
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
			return node != null && node.Predecessors.Any(pred =>
			{
				ControlFlowNode predecessor = pred as ControlFlowNode;

				return node.Dominates(predecessor, strictly: false);
			});
		}

		public bool IsLoopEnd (ControlFlowNode node)
		{
			return node != null && node.Successors.Any(succ =>
			{
				ControlFlowNode successor = succ as ControlFlowNode;

				return successor.Dominates(node, strictly: false);
			});
		}

		public bool IsLoopNode (ControlFlowNode node) => IsLoopStart(node) || IsLoopEnd(node);

		public bool IsLoop (ControlFlowNode start, ControlFlowNode end) => IsLoopStart(start) && IsLoopEnd(end)
			&& start.Predecessors.Any(pred => pred == end)
			&& start.Dominates(end, strictly: false);
	}
}
