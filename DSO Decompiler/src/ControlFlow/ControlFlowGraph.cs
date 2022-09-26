using System.Collections.Generic;

using DSODecompiler.Disassembler;
using DSODecompiler.Util;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowNode : GraphNode<uint>
	{
		public int Postorder { get; set; }
		public ControlFlowNode ImmediateDom { get; set; } = null;
		public uint Addr => Key;

		public readonly List<Instruction> Instructions = new List<Instruction> ();

		public Instruction this[int index] => index >= 0 && index < Instructions.Count ? Instructions[index] : null;

		public uint EndAddr => (uint) (Key + (Instructions.Count > 0 ? LastInstruction.Size : 1));
		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[Instructions.Count - 1] : null;

		public ControlFlowNode (uint key) : base (key) {}

		/// <summary>
		/// Calculates if this node dominates the node specified.
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

	public class ControlFlowGraph : Graph<uint, ControlFlowNode>
	{
		public ControlFlowNode EntryPoint => Get (0);

		public ControlFlowNode CreateOrGet (uint addr) => Has (addr) ? Get (addr) : Add (new ControlFlowNode (addr));

		public void PostorderDFS (DFSCallbackFn callback)
		{
			PostorderDFS (EntryPoint.Key, new HashSet<uint> (), callback);
		}

		public void PreorderDFS (DFSCallbackFn callback)
		{
			PreorderDFS (EntryPoint.Key, new HashSet<uint> (), callback);
		}
	}
}
