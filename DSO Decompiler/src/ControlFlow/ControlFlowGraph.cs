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

		public Instruction FirstInstruction => Instructions[0];
		public Instruction LastInstruction => Instructions[^1];

		public ControlFlowNode (uint addr) => Addr = addr;
	}

	public class ControlFlowGraph : DirectedGraph<uint, ControlFlowNode>
	{
		public override ControlFlowNode EntryPoint => Get(0);

		public ControlFlowNode AddOrGet (uint addr) => Has(addr) ? Get(addr) : Add(addr, new ControlFlowNode(addr));
	}
}
