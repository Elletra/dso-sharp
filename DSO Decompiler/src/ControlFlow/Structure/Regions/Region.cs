using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow.Structure.Regions
{
	public class Region
	{
		public uint Addr { get; }

		public List<Instruction> Instructions { get; } = new();

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[^1] : null;

		public Region (ControlFlowNode node)
		{
			Addr = node.Addr;

			node.Instructions.ForEach(instruction => Instructions.Add(instruction));
		}
	}
}
