using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowNode
	{
		public uint Addr { get; } = 0;
		public int Postorder { get; set; }

		public ControlFlowNode ImmediateDom { get; set; } = null;

		public bool IsLoopStart { get; set; } = false;
		public bool IsLoopEnd { get; set; } = false;

		// We don't need to store all the instructions -- We just need the last one.
		public Instruction LastInstruction { get; set; } = null;

		public readonly List<ControlFlowNode> Predecessors = new();
		public readonly List<ControlFlowNode> Successors = new();

		public ControlFlowNode (uint addr)
		{
			Addr = addr;
		}

		/// <summary>
		/// Calculates if this node dominates the node specified.
		/// <br />
		/// NOTE: Requires dominators to be calculated first.
		/// </summary>
		/// <param name="node"></param>
		/// <returns>Whether this node dominates the node specified.</returns>
		public bool Dominates (ControlFlowNode node)
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
	}

	public class ControlFlowGraph
	{
		public delegate void VisitFn (ControlFlowNode node);

		protected Dictionary<uint, ControlFlowNode> nodes = new();

		public int Count => nodes.Count;
		public ControlFlowNode EntryPoint => Get(0);

		public bool Has (uint addr) => nodes.ContainsKey(addr);

		public ControlFlowNode Add (uint addr) => nodes[addr] = new ControlFlowNode(addr);
		public ControlFlowNode Get (uint addr) => Has(addr) ? nodes[addr] : null;
		public ControlFlowNode AddOrGet (uint addr) => Has(addr) ? Get(addr) : Add(addr);

		public bool Connect (uint fromAddr, uint toAddr)
		{
			if (!Has(fromAddr) || !Has(toAddr))
			{
				return false;
			}

			var fromNode = Get(fromAddr);
			var toNode = Get(toAddr);

			fromNode.Successors.Add(toNode);
			toNode.Predecessors.Add(fromNode);

			return true;
		}

		public void Iterate (VisitFn callback)
		{
			foreach (var (_, node) in nodes)
			{
				callback(node);
			}
		}

		public void PostorderDFS (VisitFn callback)
		{
			PostorderDFS(EntryPoint.Addr, new HashSet<uint>(), callback);
		}

		public void PreorderDFS (VisitFn callback)
		{
			PreorderDFS(EntryPoint, callback);
		}

		// TODO: Somehow make this iterative because it causes a stack overflow on larger files.
		protected void PostorderDFS (uint addr, HashSet<uint> visited, VisitFn callback)
		{
			if (visited.Contains(addr))
			{
				return;
			}

			visited.Add(addr);

			var node = Get(addr);
			var successors = node.Successors;

			foreach (var successor in successors)
			{
				PostorderDFS(successor.Addr, visited, callback);
			}

			callback(node);
		}

		protected void PreorderDFS (ControlFlowNode entryPoint, VisitFn callback)
		{
			var stack = new Stack<ControlFlowNode>();
			var visited = new HashSet<uint>();

			stack.Push(entryPoint);

			while (stack.Count > 0)
			{
				var node = stack.Pop();

				if (visited.Contains(node.Addr))
				{
					continue;
				}

				visited.Add(node.Addr);
				callback(node);

				for (var i = node.Successors.Count - 1; i >= 0; i--)
				{
					stack.Push(node.Successors[i]);
				}
			}
		}
	}
}
