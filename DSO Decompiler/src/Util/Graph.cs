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
			if (!HasEdgeTo (node) && !HasEdgeFrom (node))
			{
				Successors.Add (node);
				node.Predecessors.Add (this);
			}
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

		protected void PreorderDFS (K key, HashSet<K> visited, DFSCallbackFn callback)
		{
			if (visited.Contains (key))
			{
				return;
			}

			visited.Add (key);

			var node = Get (key);
			var successors = node.Successors;

			callback (node);

			foreach (var successor in successors)
			{
				PreorderDFS (successor.Key, visited, callback);
			}
		}
	}
}
