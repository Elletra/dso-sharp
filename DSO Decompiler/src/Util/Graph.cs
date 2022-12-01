using System.Collections.Generic;

namespace DSODecompiler.Util
{
	public class GraphNode<K>
	{
		public K Key { get; }

		public readonly List<GraphNode<K>> Predecessors = new List<GraphNode<K>> ();
		public readonly List<GraphNode<K>> Successors = new List<GraphNode<K>> ();

		public GraphNode (K key)
		{
			Key = key;
		}

		public void AddEdgeTo (GraphNode<K> node)
		{
			if (!HasEdgeTo (node))
			{
				Successors.Add (node);
			}

			if (!HasEdgeFrom (node))
			{
				node.Predecessors.Add (this);
			}
		}

		public void AddEdgeFrom (GraphNode<K> node)
		{
			node.AddEdgeTo (this);
		}

		public bool HasEdgeTo (GraphNode<K> node) => Successors.Contains (node);
		public bool HasEdgeFrom (GraphNode<K> node) => node.Predecessors.Contains (this);
	}

	public class Graph<K, N> where N : GraphNode<K>
	{
		public delegate void DFSCallbackFn (N node);

		protected Dictionary<K, N> nodes = new Dictionary<K, N> ();

		public int Count => nodes.Count;

		public N Add (N node)
		{
			nodes[node.Key] = node;

			return nodes[node.Key];
		}

		public N Get (K key) => Has (key) ? nodes[key] : null;
		public bool Has (K key) => nodes.ContainsKey (key);
		public bool Has (N node) => Has (node.Key);

		public bool AddEdge (K from, K to)
		{
			if (!Has (from) || !Has (to))
			{
				return false;
			}

			Get (from).AddEdgeTo (Get (to));

			return true;
		}

		public bool HasEdge (K from, K to)
		{
			if (!Has (from) || !Has (to))
			{
				return false;
			}

			return Get (from).HasEdgeTo (Get (to));
		}

		public IEnumerable<N> GetNodes ()
		{
			foreach (var (_, node) in nodes)
			{
				yield return node;
			}
		}

		// TODO: Somehow make this iterative because it causes a stack overflow on larger files.
		protected void PostorderDFS (K key, HashSet<K> visited, DFSCallbackFn callback)
		{
			if (visited.Contains (key))
			{
				return;
			}

			visited.Add (key);

			var node = Get (key);
			var successors = node.Successors;

			foreach (var successor in successors)
			{
				PostorderDFS (successor.Key, visited, callback);
			}

			callback (node);
		}

		protected void PreorderDFS (N entryPoint, DFSCallbackFn callback)
		{
			var stack = new Stack<N> ();
			var visited = new HashSet<K> ();

			stack.Push (entryPoint);

			while (stack.Count > 0)
			{
				var node = stack.Pop ();

				if (visited.Contains (node.Key))
				{
					continue;
				}

				visited.Add (node.Key);
				callback (node);

				for (var i = node.Successors.Count - 1; i >= 0; i--)
				{
					stack.Push ((N) node.Successors[i]);
				}
			}
		}
	}
}
