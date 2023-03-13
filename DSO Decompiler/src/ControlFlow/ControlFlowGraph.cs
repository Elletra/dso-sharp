using System;
using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowNode
	{
		public uint Addr { get; }
		public List<Instruction> Instructions { get; } = new();
		public List<ControlFlowNode> Predecessors { get; } = new();
		public List<ControlFlowNode> Successors { get; } = new();

		public Instruction FirstInstruction => Instructions[0];
		public Instruction LastInstruction => Instructions[^1];

		public ControlFlowNode (uint addr) => Addr = addr;
	}

	public class ControlFlowGraph
	{
		private readonly Dictionary<uint, ControlFlowNode> nodes = new();

		public ControlFlowNode EntryPoint => Get(0);

		public ControlFlowNode Add (uint addr)
		{
			if (Has(addr))
			{
				return Get(addr);
			}

			nodes[addr] = new(addr);

			return nodes[addr];
		}

		public bool Has (uint addr) => nodes.ContainsKey(addr);
		public ControlFlowNode Get (uint addr) => Has(addr) ? nodes[addr] : null;
		public ControlFlowNode AddOrGet (uint addr) => Has(addr) ? Get(addr) : Add(addr);

		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns>false if either `from` or `to` nodes do not exist.</returns>
		public bool AddEdge (uint from, uint to)
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
		public bool RemoveEdge (uint from, uint to)
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

		public bool HasEdge (uint from, uint to)
		{
			if (!Has(from) || !Has(to))
			{
				return false;
			}

			var nodeFrom = Get(from);
			var nodeTo = Get(to);

			return nodeFrom.Successors.Contains(nodeTo) && nodeTo.Predecessors.Contains(nodeFrom);
		}

		public void AddEdge (ControlFlowNode from, ControlFlowNode to)
		{
			from.Successors.Add(to);
			to.Predecessors.Add(from);
		}

		public void RemoveEdge (ControlFlowNode from, ControlFlowNode to)
		{
			from.Successors.Remove(to);
			to.Predecessors.Remove(from);
		}

		public bool HasEdge (ControlFlowNode from, ControlFlowNode to) => from.Successors.Contains(to);

		/// <summary>
		/// Iterates over all the nodes, even if they're not connected to anything.
		/// </summary>
		/// <returns></returns>
		public IEnumerator<ControlFlowNode> GetEnumerator ()
		{
			foreach (var pair in nodes)
			{
				yield return pair.Value;
			}
		}

		public IEnumerable<ControlFlowNode> PreorderDFS ()
		{
			var node = EntryPoint;
			var visited = new HashSet<uint>();
			var queue = new Queue<ControlFlowNode>();

			queue.Enqueue(node);

			while (queue.Count > 0)
			{
				node = queue.Dequeue();

				yield return node;

				visited.Add(node.Addr);

				foreach (var successor in node.Successors)
				{
					if (!visited.Contains(successor.Addr))
					{
						queue.Enqueue(successor);
					}
				}
			}
		}

		public IEnumerable<ControlFlowNode> PostorderDFS ()
		{
			throw new NotImplementedException($"TODO: Implement me!");
		}
	}
}
