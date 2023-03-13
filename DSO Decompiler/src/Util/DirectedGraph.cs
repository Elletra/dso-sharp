using System;
using System.Collections.Generic;

namespace DSODecompiler.Util
{
	public class DirectedGraph<K, N> where N : DirectedGraph<K, N>.Node
	{
		public class Node
		{
			public List<N> Predecessors { get; } = new();
			public List<N> Successors { get; } = new();
		}

		private readonly Dictionary<K, N> nodes = new();

		public virtual N EntryPoint
		{
			get
			{
				foreach (var pair in nodes)
				{
					return pair.Value;
				}

				return null;
			}
		}

		public N Add (K key, N node)
		{
			if (Has(key))
			{
				return Get(key);
			}

			nodes[key] = node;

			return node;
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

		public IEnumerable<N> PreorderDFS ()
		{
			var node = EntryPoint;
			var visited = new HashSet<N>();
			var queue = new Queue<N>();

			queue.Enqueue(node);

			while (queue.Count > 0)
			{
				node = queue.Dequeue();

				yield return node;

				visited.Add(node);

				foreach (var successor in node.Successors)
				{
					if (!visited.Contains(successor))
					{
						queue.Enqueue(successor);
					}
				}
			}
		}

		public IEnumerable<N> PostorderDFS ()
		{
			throw new NotImplementedException($"TODO: Implement me!");
		}
	}
}
