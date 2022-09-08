﻿using System.Collections.Generic;
using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowGraph
	{
		public class Node
		{
			public uint Addr { get; }
			public int Postorder { get; set; }
			public Node ImmediateDom { get; set; } = null;
			public bool IsLoopEnd { get; set; } = false;

			public readonly List<Node> Predecessors = new List<Node> ();
			public readonly List<Node> Successors = new List<Node> ();
			public readonly List<Instruction> Instructions = new List<Instruction> ();

			public Node (uint addr)
			{
				Addr = addr;
			}

			public Instruction this[int index] => index >= 0 && index < Instructions.Count ? Instructions[index] : null;

			public uint EndAddr => (uint) (Addr + (Instructions.Count > 0 ? LastInstruction.Size : 1));
			public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
			public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[Instructions.Count - 1] : null;

			/// <summary>
			/// Calculates if this node dominates the node specified.
			/// </summary>
			/// <param name="node"></param>
			/// <returns>Whether this node dominates the node specified.</returns>
			public bool Dominates (Node node)
			{
				// All nodes dominate themselves.
				if (node == this)
				{
					return true;
				}

				var dom = node.ImmediateDom;

				while (dom != this && dom != null && dom != dom.ImmediateDom)
				{
					dom = dom.ImmediateDom;
				}

				return dom == this;
			}

			public void AddEdgeTo (Node node)
			{
				if (!HasEdgeTo (node))
				{
					Successors.Add (node);
					node.Predecessors.Add (this);
				}
			}

			public bool HasEdgeTo (Node node) => Successors.Contains (node);
		}

		public delegate void DFSCallbackFn (Node node);

		protected Dictionary<uint, Node> nodes = new Dictionary<uint, Node> ();

		public Node AddNode (uint addr)
		{
			nodes.Add (addr, new Node (addr));

			return nodes[addr];
		}

		public Node GetNode (uint addr) => nodes.ContainsKey (addr) ? nodes[addr] : null;
		public bool HasNode (uint addr) => nodes.ContainsKey (addr);
		public Node EntryPoint => HasNode (0) ? GetNode (0) : null;
		public int NodeCount => nodes.Count;

		public bool AddEdge (uint edgeFrom, uint edgeTo)
		{
			if (!HasNode (edgeFrom) || !HasNode (edgeTo))
			{
				return false;
			}

			GetNode (edgeFrom).AddEdgeTo (GetNode (edgeTo));

			return true;
		}

		public bool HasEdge (uint edgeFrom, uint edgeTo)
		{
			if (!HasNode (edgeFrom) || !HasNode (edgeTo))
			{
				return false;
			}

			return GetNode (edgeFrom).HasEdgeTo (GetNode (edgeTo));
		}

		public void PostorderDFS (DFSCallbackFn callback)
		{
			PostorderDFS (EntryPoint.Addr, new HashSet<uint> (), callback);
		}

		protected void PostorderDFS (uint addr, HashSet<uint> visited, DFSCallbackFn callback)
		{
			if (visited.Contains (addr))
			{
				return;
			}

			visited.Add (addr);

			var node = GetNode (addr);
			var successors = node.Successors;

			foreach (var successor in successors)
			{
				PostorderDFS (successor.Addr, visited, callback);
			}

			callback (node);
		}

		public void PreorderDFS (DFSCallbackFn callback)
		{
			PreorderDFS (EntryPoint.Addr, new HashSet<uint> (), callback);
		}

		public void PreorderDFS (uint addr, HashSet<uint> visited, DFSCallbackFn callback)
		{
			if (visited.Contains (addr))
			{
				return;
			}

			visited.Add (addr);

			var node = GetNode (addr);
			var successors = node.Successors;

			callback (node);

			foreach (var successor in successors)
			{
				PreorderDFS (successor.Addr, visited, callback);
			}
		}
	}
}