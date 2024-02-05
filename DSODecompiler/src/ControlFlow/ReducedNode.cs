using System;
using System.Collections.Generic;

using DSODecompiler.Disassembly;


namespace DSODecompiler.ControlFlow
{
	public abstract class ReducedNode
	{
		public virtual uint Addr => 0;
	}

	public class InstructionsNode : ReducedNode
	{
		public List<Instruction> Instructions = new();

		public Instruction First => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction Last => Instructions.Count > 0 ? Instructions[^1] : null;

		public override uint Addr => First?.Addr ?? 0;

		public InstructionsNode(ControlFlowNode node) => ExtractFrom(node);

		public void Add(Instruction instruction) => Instructions.Add(instruction);

		public void ExtractFrom(ControlFlowNode node)
		{
			node.Instructions.ForEach(Add);
			node.Instructions.Clear();
		}
	}

	public class SequenceNode : ReducedNode
	{
		public List<ReducedNode> Nodes = new();

		public override uint Addr => Nodes.Count > 0 ? Nodes[0].Addr : 0;

		public SequenceNode(ReducedNode node) => Add(node);
		public SequenceNode(params ReducedNode[] nodes) => Add(nodes);

		public void Add(ReducedNode node)
		{
			if (node is SequenceNode sequence)
			{
				sequence.Nodes.ForEach(Add);
			}
			else if (node != null)
			{
				Nodes.Add(node);
			}
		}

		public void Add(params ReducedNode[] nodes) => Array.ForEach(nodes, Add);
	}

	public class ConditionalNode : ReducedNode
	{
		public ReducedNode If = null;
		public ReducedNode Then = null;
		public ReducedNode Else { get; set; } = null;

		public override uint Addr => If?.Addr ?? 0;
	}

	public class LoopNode : ReducedNode
	{
		public SequenceNode Body = new();

		public override uint Addr => Body.Addr;
	}
}
