using System.Collections.Generic;

namespace DSODecompiler.Util
{
	public class DirectedGraph<K, V>
	{
		public class Node
		{
			public readonly List<Node> Predecessors = new();
			public readonly List<Node> Successors = new();

			public K Key { get; set; }
			public V Value { get; set; }

			public Node (K key, V value)
			{
				Key = key;
				Value = value;
			}

			public void AddEdgeTo (Node node)
			{
				if (!Successors.Contains(node))
				{
					Successors.Add(node);
					node.Predecessors.Add(this);
				}
			}

			public void RemoveEdgeTo (Node node)
			{
				Successors.Remove(node);
				node.Predecessors.Remove(this);
			}
		}

		protected Dictionary<K, Node> nodes = new();

		/**
		 * Node-related methods
		 */

		public Node AddNode (Node node)
		{
			nodes[node.Key] = node;

			return node;
		}

		public void RemoveNode (Node node)
		{
			foreach (var successor in node.Successors)
			{
				RemoveEdge(node.Key, successor.Key);
				RemoveEdge(successor.Key, node.Key);
			}
		}

		public bool HasNode (K key) => nodes.ContainsKey(key);
		public bool HasNode (Node node) => nodes.ContainsKey(node.Key);

		public Node GetNode (K key) => HasNode(key) ? nodes[key] : null;

		/**
		 * Edge-related methods
		 */

		public bool AddEdge (K from, K to)
		{
			if (!HasNode(from) || !HasNode(to))
			{
				return false;
			}

			GetNode(from).AddEdgeTo(GetNode(to));

			return true;
		}

		public bool RemoveEdge (K from, K to)
		{
			if (!HasNode(from) || !HasNode(to))
			{
				return false;
			}

			GetNode(from).RemoveEdgeTo(GetNode(to));

			return true;
		}

		/**
		 * Traversal methods
		 */

		public IEnumerable<Node> PreorderDFS (Node entry)
		{
			var node = entry;
			var visited = new HashSet<Node>();
			var stack = new Stack<Node>();

			stack.Push(node);

			while (stack.Count > 0)
			{
				node = stack.Pop();

				if (HasNode(node))
				{
					yield return node;

					visited.Add(node);

					for (var i = node.Successors.Count - 1; i >= 0; i--)
					{
						var successor = node.Successors[i];

						if (!visited.Contains(successor))
						{
							stack.Push(successor);
						}
					}
				}
			}
		}

		public IEnumerable<Node> PreorderDFS (K entryKey) => PreorderDFS(GetNode(entryKey));
	}
}
