using System;
using System.Collections.Generic;

using DSODecompiler.Disassembly;

namespace DSODecompiler.ControlFlow
{

	public abstract class CollapsedNode
	{
		public uint Addr { get; }

		public CollapsedNode (uint addr)
		{
			Addr = addr;
		}
	}

	public class InstructionNode : CollapsedNode
	{
		public readonly List<Instruction> Instructions = new();

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[^1] : null;

		public InstructionNode (uint key) : base(key) { }

		public InstructionNode (ControlFlowNode node) : base(node.Addr)
		{
			ExtractInstructions(node);
		}

		public void ExtractInstructions (ControlFlowNode node)
		{
			node.Instructions.ForEach(Instructions.Add);
			node.Instructions.Clear();
		}
	}

	public class ConditionalNode : InstructionNode
	{
		public CollapsedNode Then = null;
		public CollapsedNode Else = null;

		public ConditionalNode (uint key) : base(key) { }
		public ConditionalNode (ControlFlowNode node) : base(node) { }
	}

	public class LoopNode : InstructionNode
	{
		public readonly List<CollapsedNode> Body = new();

		public LoopNode (uint key) : base(key) { }
		public LoopNode (ControlFlowNode node) : base(node) { }

		public T AddNode<T> (T node) where T : CollapsedNode
		{
			if (node != null)
			{
				Body.Add(node);
			}

			return node;
		}

		public void AddNodes<T> (params T[] nodes) where T : CollapsedNode => Body.AddRange(nodes);
	}

	public class ElseNode : InstructionNode
	{
		public ElseNode (uint key) : base(key) { }
		public ElseNode (ControlFlowNode node) : base(node) { }
	}

	public class BreakNode : InstructionNode
	{
		public BreakNode (uint key) : base(key) { }
		public BreakNode (ControlFlowNode node) : base(node) { }
	}

	public class GotoNode : InstructionNode
	{
		public uint Target { get; }

		public GotoNode (uint key, uint target) : base(key) => Target = target;
		public GotoNode (ControlFlowNode node, uint target) : base(node) => Target = target;
	}

	public class SequenceNode : CollapsedNode
	{
		public readonly List<CollapsedNode> Nodes = new();

		public SequenceNode (uint key) : base(key) { }

		/// <summary>
		/// Adds <paramref name="node"/>, if it's not null, to the list of nodes.<br/><br/>
		///
		/// If <paramref name="node"/> is a <see cref="SequenceNode"/>, we add the nodes from its
		/// list instead.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="node"></param>
		/// <returns></returns>
		public T AddNode<T> (T node) where T : CollapsedNode
		{
			if (node != null)
			{
				if (node is SequenceNode sequence)
				{
					// Extract nodes from existing sequence node.
					sequence.Nodes.ForEach(Nodes.Add);
				}
				else
				{
					Nodes.Add(node);
				}
			}

			return node;
		}
	}
}
