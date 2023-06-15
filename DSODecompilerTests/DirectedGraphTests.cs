using Microsoft.VisualStudio.TestTools.UnitTesting;

using DSODecompiler.Util;

namespace DSODecompilerTests
{
	using TestGraph = DirectedGraph<uint>;
	using TestNode = DirectedGraph<uint>.Node;

	[TestClass]
	public class DirectedGraphTests
	{
		/**
		 * Node tests
		 */

		[TestMethod]
		public void Node_AddEdgeTo_AddsEdgeProperly ()
		{
			var node1 = CreateNode(6);
			var node2 = CreateNode(10);

			node1.AddEdgeTo(node2);
			Node_AssertEdge(node1, node2, "node1", "node2");

			node2.AddEdgeTo(node1);
			Node_AssertEdge(node2, node1, "node2", "node1");
		}

		[TestMethod]
		public void Node_AddEdgeTo_AddsEdgeWithNoDuplicates ()
		{
			var node1 = CreateNode(6);
			var node2 = CreateNode(10);

			node1.AddEdgeTo(node2);
			node1.AddEdgeTo(node2);
			node1.AddEdgeTo(node2);
			node1.AddEdgeTo(node2);

			var count = 0;

			foreach (var successor in node1.Successors)
			{
				if (successor == node2)
				{
					count++;
				}
			}

			Assert.AreEqual(1, count, "More than one (1) node2 instances found in node1.Successors");
			Assert.AreNotEqual(4, count, "Four (4) node2 instances found in node1.Successors");
		}

		[TestMethod]
		public void Node_RemoveEdgeTo_RemovesEdgeProperly ()
		{
			var node1 = CreateNode(6);
			var node2 = CreateNode(10);

			node1.AddEdgeTo(node2);
			Node_AssertEdge(node1, node2, "node1", "node2");

			node1.RemoveEdgeTo(node2);
			Node_AssertNoEdge(node1, node2, "node1", "node2");
		}

		/**
		 * DirectedGraph tests
		 */

		[TestMethod]
		public void DirectedGraph_AddEdge_AddsEdgeProperly ()
		{
			var graph = CreateGraph();
			var node1 = graph.AddNode(CreateNode(5));
			var node2 = graph.AddNode(CreateNode(0));

			Assert.IsTrue(graph.AddEdge(node1.Key, node2.Key));

			Node_AssertSingleEdge(node1, node2, "node1", "node2");
		}

		[TestMethod]
		public void DirectedGraph_AddEdge_FailsOnInvalidKey ()
		{
			var graph = CreateGraph();

			Assert.IsFalse(graph.AddEdge(1, 2), "Edge from non-existent nodes (1=>2)");

			graph.AddNode(CreateNode(1));
			Assert.IsFalse(graph.AddEdge(1, 2), "Edge to non-existent node with key 2");

			graph.AddNode(CreateNode(5));
			Assert.IsFalse(graph.AddEdge(8, 5), "Edge from non-existent node with key 8");
		}

		[TestMethod]
		public void DirectedGraph_RemoveEdge_RemovesEdgeProperly ()
		{
			var graph = CreateGraph();
			var node1 = graph.AddNode(CreateNode(5));
			var node2 = graph.AddNode(CreateNode(0));

			Assert.IsTrue(graph.AddEdge(node1.Key, node2.Key));
			Node_AssertSingleEdge(node1, node2, "node1", "node2");

			Assert.IsTrue(graph.RemoveEdge(node1.Key, node2.Key));
			Node_AssertNoEdge(node1, node2, "node1", "node2");
		}

		[TestMethod]
		public void DirectedGraph_PreorderDFS_VisitsInRightOrder ()
		{
			var graph = CreateGraph();

			for (uint i = 1; i <= 6; i++)
			{
				graph.AddNode(CreateNode(i));
			}

			graph.AddEdge(1, 2);
			graph.AddEdge(1, 5);
			graph.AddEdge(1, 6);
			graph.AddEdge(2, 3);
			graph.AddEdge(2, 4);

			uint index = 1;

			foreach (var node in graph.PreorderDFS(1))
			{
				Assert.AreEqual(index++, node.Key);
			}
		}

		[TestMethod]
		public void DirectedGraph_PostorderDFS_VisitsInRightOrder ()
		{
			var graph = CreateGraph();

			for (uint i = 1; i <= 6; i++)
			{
				graph.AddNode(CreateNode(i));
			}

			graph.AddEdge(3, 1);
			graph.AddEdge(3, 2);
			graph.AddEdge(5, 3);
			graph.AddEdge(5, 4);

			uint index = 1;

			foreach (var node in graph.PostorderDFS(5))
			{
				Assert.AreEqual(index++, node.Key);
			}
		}

		/**
		 * Node utility methods
		 */

		private void Node_AssertHasSuccessor (TestNode node, TestNode successor, string nodeName, string successorName)
		{
			Assert.IsTrue(node.Successors.Contains(successor), $"{successorName} not found in {nodeName}.Successors");
		}

		private void Node_AssertHasPredecessor (TestNode node, TestNode predecessor, string nodeName, string predecessorName)
		{
			Assert.IsTrue(node.Predecessors.Contains(predecessor), $"{predecessorName} not found in {nodeName}.Predecessors");
		}

		private void Node_AssertEdge (TestNode from, TestNode to, string fromName, string toName)
		{
			Node_AssertHasSuccessor(from, to, fromName, toName);
			Node_AssertHasPredecessor(to, from, toName, fromName);
		}

		private void Node_AssertNoSuccessor (TestNode node, TestNode successor, string nodeName, string successorName)
		{
			Assert.IsFalse(node.Successors.Contains(successor), $"{successorName} was found in {nodeName}.Successors");
		}

		private void Node_AssertNoPredecessor (TestNode node, TestNode predecessor, string nodeName, string predecessorName)
		{
			Assert.IsFalse(node.Predecessors.Contains(predecessor), $"{predecessorName} was found in {nodeName}.Predecessors");
		}

		private void Node_AssertNoEdge (TestNode from, TestNode to, string fromName, string toName)
		{
			Node_AssertNoSuccessor(from, to, fromName, toName);
			Node_AssertNoPredecessor(to, from, toName, fromName);
		}

		private void Node_AssertSingleEdge (TestNode from, TestNode to, string fromName, string toName)
		{
			Node_AssertEdge(from, to, fromName, toName);
			Node_AssertNoEdge(to, from, toName, fromName);
		}

		private TestNode CreateNode (uint key) => new(key);

		/**
		 * DirectedGraph utility methods
		 */

		private TestGraph CreateGraph () => new();
	}
}
