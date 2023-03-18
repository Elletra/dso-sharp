using System;
using System.Collections.Generic;

namespace DSODecompiler.Util
{
	public class GraphNode
	{
		public List<GraphNode> Predecessors { get; } = new();
		public List<GraphNode> Successors { get; } = new();

		public GraphNode FirstPredecessor => Predecessors.Count > 0 ? Predecessors[0] : null;
		public GraphNode FirstSuccessor => Successors.Count > 0 ? Successors[0] : null;

		public GraphNode LastPredecessor => Predecessors.Count > 0 ? Predecessors[^1] : null;
		public GraphNode LastSuccessor => Successors.Count > 0 ? Successors[^1] : null;
	}

	public class DirectedGraph<K, N> where N : GraphNode
	{
		protected readonly Dictionary<K, N> nodes = new();

		// TODO: There must be a better way than allowing users to manually set the entry point.
		public virtual N EntryPoint { get; set; } = null;

		public int Count => nodes.Count;

		public N Add (K key, N node)
		{
			if (Has(key))
			{
				return Get(key);
			}

			nodes[key] = node;

			return node;
		}

		public bool Remove (K key)
		{
			if (!Has(key))
			{
				return false;
			}

			var node = Get(key);

			foreach (N predecessor in node.Predecessors)
			{
				RemoveEdge(predecessor, node);
			}

			foreach (N successor in node.Successors)
			{
				RemoveEdge(node, successor);
			}

			return nodes.Remove(key);
		}

		public bool Has (K key) => nodes.ContainsKey(key);
		public N Get (K key) => Has(key) ? nodes[key] : null;

		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns>false if either `from` or `to` nodes do not exist.</returns>
		public bool AddEdge (K from, K to)
		{
			if (!Has(from) || !Has(to))
			{
				return false;
			}

			var nodeFrom = Get(from);
			var nodeTo = Get(to);

			nodeFrom.Successors.Add(nodeTo);
			nodeTo.Predecessors.Add(nodeFrom);

			return true;
		}

		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns>false if either `from` or `to` nodes do not exist.</returns>
		public bool RemoveEdge (K from, K to)
		{
			if (!Has(from) || !Has(to))
			{
				return false;
			}

			var nodeFrom = Get(from);
			var nodeTo = Get(to);

			nodeFrom.Successors.Remove(nodeTo);
			nodeTo.Predecessors.Remove(nodeFrom);

			return true;
		}

		public bool HasEdge (K from, K to)
		{
			if (!Has(from) || !Has(to))
			{
				return false;
			}

			var nodeFrom = Get(from);
			var nodeTo = Get(to);

			return nodeFrom.Successors.Contains(nodeTo) && nodeTo.Predecessors.Contains(nodeFrom);
		}

		public void AddEdge (N from, N to)
		{
			from.Successors.Add(to);
			to.Predecessors.Add(from);
		}

		public void RemoveEdge (N from, N to)
		{
			from.Successors.Remove(to);
			to.Predecessors.Remove(from);
		}

		public bool HasEdge (N from, N to) => from.Successors.Contains(to);

		/// <summary>
		/// Iterates over all the nodes, even if they're not connected to anything.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<N> GetEnumerator ()
		{
			foreach (var pair in nodes)
			{
				yield return pair.Value;
			}
		}

		public List<N> PreorderDFS ()
		{
			var list = new List<N>();
			var node = EntryPoint;
			var visited = new HashSet<N>();
			var queue = new Queue<N>();

			queue.Enqueue(node);

			while (queue.Count > 0)
			{
				node = queue.Dequeue();

				list.Add(node);
				visited.Add(node);

				foreach (N successor in node.Successors)
				{
					if (!visited.Contains(successor))
					{
						queue.Enqueue(successor);
					}
				}
			}

			return list;
		}

		/// <summary>
		/// Iterative postorder traversal on a cyclic graph... Good lord.<br /><br />
		///
		/// I did not come up with this algorithm, though I certainly tried.<br /><br />
		///
		/// Full credit goes to <see href="https://stackoverflow.com/a/50646181">Hans Olsson</see> on Stack Overflow.
		/// </summary>
		/// <returns></returns>
		public List<N> PostorderDFS ()
		{
			var list = new List<N>();
			var visited = new HashSet<N>();
			var stack = new Stack<(N, bool)>();

			stack.Push((EntryPoint, false));

			while (stack.Count > 0)
			{
				var (node, visitNode) = stack.Pop();

				if (visitNode)
				{
					list.Add(node);
				}
				else if (!visited.Contains(node))
				{
					visited.Add(node);
					stack.Push((node, true));

					var count = node.Successors.Count;

					for (var i = count - 1; i >= 0; i--)
					{
						stack.Push((node.Successors[i] as N, false));
					}
				}
			}

			return list;
		}
	}
}
