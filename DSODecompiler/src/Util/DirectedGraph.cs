using System.Collections.Generic;

namespace DSODecompiler.Util
{
	public class DirectedGraph<K>
	{
		public class Node
		{
			public readonly List<Node> Predecessors = new();
			public readonly List<Node> Successors = new();

			public K Key { get; set; }

			public Node (K key)
			{
				Key = key;
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

		/// <summary>
		/// Gets a list of nodes of the type specified.<br/><br/>
		///
		/// Intended for subclasses to use it to implement their own public `GetNodes()` method.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected List<T> GetNodes<T> () where T : Node
		{
			var list = new List<T>();

			foreach (T value in nodes.Values)
			{
				list.Add(value);
			}

			return list;
		}

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
		 *
		 * These all return `List`s instead of `IEnumerable`s so the graph can be modified during traversal.
		 */

		public List<Node> PreorderDFS (Node entry)
		{
			var nodes = new List<Node>();
			var node = entry;
			var visited = new HashSet<Node>();
			var stack = new Stack<Node>();

			stack.Push(node);

			while (stack.Count > 0)
			{
				nodes.Add(node = stack.Pop());
				visited.Add(node);

				// Iterate in reverse order since we're using a stack.
				for (var i = node.Successors.Count - 1; i >= 0; i--)
				{
					var successor = node.Successors[i];

					if (!visited.Contains(successor))
					{
						stack.Push(successor);
					}
				}
			}

			return nodes;
		}

		public List<Node> PreorderDFS (K entryKey) => PreorderDFS(GetNode(entryKey));

		/// <summary>
		/// Iterative postorder traversal on a cyclic graph... Good lord.<br/><br/>
		///
		/// I did not come up with this algorithm, though I certainly tried.<br/><br/>
		///
		/// Full credit goes to <see href="https://stackoverflow.com/a/50646181">Hans Olsson</see> on Stack Overflow.
		/// </summary>
		/// <returns></returns>
		public List<Node> PostorderDFS (Node entry)
		{
			var nodes = new List<Node>();
			var visited = new HashSet<Node>();
			var stack = new Stack<(Node, bool)>();

			stack.Push((entry, false));

			while (stack.Count > 0)
			{
				var (node, visitNode) = stack.Pop();

				if (visitNode)
				{
					nodes.Add(node);
				}
				else if (!visited.Contains(node))
				{
					visited.Add(node);
					stack.Push((node, true));

					var count = node.Successors.Count;

					for (var i = count - 1; i >= 0; i--)
					{
						stack.Push((node.Successors[i], false));
					}
				}
			}

			return nodes;
		}

		public List<Node> PostorderDFS (K entryKey) => PostorderDFS(GetNode(entryKey));
	}
}
