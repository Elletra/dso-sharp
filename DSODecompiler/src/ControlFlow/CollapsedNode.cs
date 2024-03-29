﻿using System;
using System.Collections.Generic;

using DSODecompiler.Disassembly;

namespace DSODecompiler.ControlFlow
{
	public abstract class CollapsedNode
	{
		/// <summary>
		/// TODO: A hack so I can get for loops working correctly. I hate to do this, but I just want
		/// this shit <em><strong>done</strong></em>.
		/// </summary>
		public bool IsContinuePoint { get; set; } = false;
	}

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

	public class FunctionNode : CollapsedNode
	{
		public FunctionInstruction Instruction { get; }
		public CollapsedNode Body { get; }

		public FunctionNode (FunctionInstruction instruction, CollapsedNode body)
		{
			Instruction = instruction;
			Body = body;
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

	public abstract class UnconditionalNode : CollapsedNode
	{
		public Instruction Instruction { get; }

		public UnconditionalNode (ControlFlowNode node)
		{
			Instruction = node.LastInstruction;

			node.Instructions.RemoveAt(node.Instructions.Count - 1);
		}
	}

	public class ElseNode : UnconditionalNode
	{
		public ElseNode (ControlFlowNode node) : base(node) { }
	}

	public class BreakNode : UnconditionalNode
	{
		public BreakNode (ControlFlowNode node) : base(node) { }
	}

	public class ContinueNode : UnconditionalNode
	{
		public ContinueNode (ControlFlowNode node) : base(node) { }
	}

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
