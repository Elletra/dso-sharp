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

		public InstructionNode (uint key) : base(key) {}
	}

	public class ConditionalNode : InstructionNode
	{
		public CollapsedNode Then = null;
		public CollapsedNode Else = null;

		public ConditionalNode (uint key) : base(key) {}
	}

	public class LoopNode : InstructionNode
	{
		public readonly List<CollapsedNode> Body = new();

		public LoopNode (uint key) : base(key) {}

		public T AddNode<T> (T node) where T : CollapsedNode
		{
			Body.Add(node);

			return node;
		}

		public void AddNodes<T> (params T[] nodes) where T : CollapsedNode => Body.AddRange(nodes);
	}

	public class SequenceNode : CollapsedNode
	{
		public readonly List<CollapsedNode> Nodes = new();

		public SequenceNode (uint key) : base(key) {}

		public T AddNode<T> (T node) where T : CollapsedNode
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

			return node;
		}

		public void AddNodes<T> (params T[] nodes) where T : CollapsedNode => Nodes.AddRange(nodes);
	}
}
