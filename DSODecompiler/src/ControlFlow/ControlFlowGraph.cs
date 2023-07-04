using System.Collections.Generic;

using DSODecompiler.Disassembly;
using DSODecompiler.Util;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowNode : DirectedGraph<uint>.Node
	{
		/// <summary>
		/// The instructions that make up this node.
		/// </summary>
		public readonly List<Instruction> Instructions = new();

		/// <summary>
		/// Immediate <see cref="DominanceCalculator">dominator</see> of this node.
		/// </summary>
		public ControlFlowNode ImmediateDom { get; set; } = null;

		/// <summary>
		/// Used by <see cref="DominanceCalculator"/> to calculate dominators.
		/// </summary>
		public int ReversePostorder { get; set; }

		public uint Addr => Key;

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;

		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[^1] : null;

		public ControlFlowNode (uint key) : base(key) {}

		public ControlFlowNode GetSuccessor (int index) => Successors.Count > 0
			? Successors[index] as ControlFlowNode
			: null;

		public ControlFlowNode GetPredecessor (int index) => Predecessors.Count > 0
			? Predecessors[index] as ControlFlowNode
			: null;

		public Instruction AddInstruction (Instruction instruction)
		{
			Instructions.Add(instruction);

			return instruction;
		}

		/// <summary>
		/// Calculates whether this node dominates <paramref name="target"/>, strictly or not strictly.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="strictly"></param>
		/// <returns></returns>
		public bool Dominates (ControlFlowNode target, bool strictly = false)
		{
			// All nodes dominate themselves, but not strictly.
			if (this == target)
			{
				return !strictly;
			}

			var dom = target.ImmediateDom;

			while (dom != this && dom != null && dom != dom.ImmediateDom)
			{
				dom = dom.ImmediateDom;
			}

			return dom == this;
		}
	}

	public class ControlFlowGraph : DirectedGraph<uint>
	{
		/// <summary>
		/// There must be a better way than to make this a public, settable value...<br/><br/>
		///
		/// TODO: Maybe fix someday?
		/// </summary>
		public uint EntryPoint { get; set; }

		public FunctionInstruction FunctionInstruction { get; set; } = null;
		public bool IsFunction => FunctionInstruction != null;

		public ControlFlowNode AddNode (uint addr) => AddNode(new ControlFlowNode(addr)) as ControlFlowNode;
		public ControlFlowNode GetNode (uint key) => GetNode<ControlFlowNode>(key);

		public List<ControlFlowNode> GetNodes ()
		{
			var list = GetNodes<ControlFlowNode>();

			// TODO: Again, if I ever implement recursive descent disassembly, this will not work.
			list.Sort((node1, node2) => node1.Addr.CompareTo(node2.Addr));

			return list;
		}

		public ControlFlowNode GetEntryPoint () => GetNode(EntryPoint);

		public List<Node> PreorderDFS () => PreorderDFS(EntryPoint);
		public List<Node> PostorderDFS () => PostorderDFS(EntryPoint);
	}
}
