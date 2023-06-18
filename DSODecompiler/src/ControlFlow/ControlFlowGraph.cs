﻿using System.Collections.Generic;

using DSODecompiler.Disassembly;
using DSODecompiler.Util;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowNode : DirectedGraph<uint>.Node
	{
		/// <summary>
		/// Immediate <see cref="DominanceCalculator">dominator</see> of this node.
		/// </summary>
		public ControlFlowNode ImmediateDom { get; set; } = null;

		/// <summary>
		/// Used by <see cref="DominanceCalculator"/> to calculate dominators.
		/// </summary>
		public int ReversePostorder { get; set; }

		/// <summary>
		/// During <see cref="StructureAnalyzer">structural analysis</see>, we need to "collapse"
		/// or "reduce" nodes into representations of higher-level structures.
		/// </summary>
		public CollapsedNode CollapsedNode { get; set; }  = null;

		public uint Addr => Key;

		public Instruction FirstInstruction => CollapsedNode is InstructionNode
			? (CollapsedNode as InstructionNode).FirstInstruction
			: null;

		public Instruction LastInstruction => CollapsedNode is InstructionNode
			? (CollapsedNode as InstructionNode).LastInstruction
			: null;

		public ControlFlowNode (uint key) : base(key) {}

		public ControlFlowNode GetSuccessor (int index) => Successors.Count > 0
			? Successors[index] as ControlFlowNode
			: null;

		public ControlFlowNode GetPredecessor (int index) => Predecessors.Count > 0
			? Predecessors[index] as ControlFlowNode
			: null;

		public Instruction AddInstruction (Instruction instruction)
		{
			if (CollapsedNode is InstructionNode node)
			{
				node.Instructions.Add(instruction);

				return instruction;
			}

			return null;
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

		public ControlFlowNode AddNode (uint addr)
		{
			var node = new ControlFlowNode(addr)
			{
				CollapsedNode = new InstructionNode(addr),
			};

			return AddNode(node) as ControlFlowNode;
		}

		public List<ControlFlowNode> GetNodes ()
		{
			var list = GetNodes<ControlFlowNode>();

			// TODO: Again, if I ever implement recursive descent disassembly, this will not work.
			list.Sort((node1, node2) => node1.Addr.CompareTo(node2.Addr));

			return list;
		}
	}
}