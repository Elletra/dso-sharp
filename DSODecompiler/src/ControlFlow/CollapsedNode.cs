using System;
using System.Collections.Generic;

using DSODecompiler.Disassembly;

namespace DSODecompiler.ControlFlow
{
	public abstract class CollapsedNode { }

	public class InstructionNode : CollapsedNode
	{
		public readonly List<Instruction> Instructions = new();

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[^1] : null;

		public InstructionNode (ControlFlowNode node) => ExtractInstructions(node);

		public void ExtractInstructions (ControlFlowNode node)
		{
			node.Instructions.ForEach(Instructions.Add);
			node.Instructions.Clear();
		}
	}

	public class ConditionalNode : CollapsedNode
	{
		public CollapsedNode Then = null;
		public CollapsedNode Else = null;
	}

	public class LoopNode : CollapsedNode
	{
		public readonly List<CollapsedNode> Body = new();

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

	public abstract class JumpNode : CollapsedNode { }
	public class ElseNode : JumpNode { }
	public class BreakNode : JumpNode { }
	public class ContinueNode : JumpNode { }

	public class SequenceNode : CollapsedNode
	{
		public readonly List<CollapsedNode> Nodes = new();

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
