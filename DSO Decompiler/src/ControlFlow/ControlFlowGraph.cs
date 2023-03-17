using System;
using System.Collections.Generic;

using DSODecompiler.Disassembler;
using DSODecompiler.Util;

namespace DSODecompiler.ControlFlow
{
	public class ControlFlowNode : DirectedGraph<uint, ControlFlowNode>.Node
	{
		public uint Addr { get; }
		public List<Instruction> Instructions { get; } = new();

		public bool IsLoopStart { get; set; } = false;
		public bool IsLoopEnd { get; set; } = false;

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[^1] : null;

		public ControlFlowNode (uint addr) => Addr = addr;
	}

	public class ControlFlowGraph : DirectedGraph<uint, ControlFlowNode>
	{
		public FunctionInstruction FunctionHeader { get; } = null;

		public bool IsFunction => FunctionHeader != null;

		public ControlFlowGraph (FunctionInstruction func = null) => FunctionHeader = func;
		public ControlFlowNode AddOrGet (uint addr) => Has(addr) ? Get(addr) : Add(addr, new ControlFlowNode(addr));

		// TODO: A better way would perhaps be to cache the value instead of looping every time...
		public uint GetLoopCount ()
		{
			uint count = 0;

			foreach (var node in nodes.Values)
			{
				// We use `IsLoopEnd` because multiple loops can share the same start block.
				if (node.IsLoopEnd)
				{
					count++;
				}
			}

			return count;
		}
	}
}
