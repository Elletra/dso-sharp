using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow.Structure.Regions
{
	public class Region
	{
		public uint Addr { get; }
		public FunctionInstruction FunctionHeader { get; } = null;
		public List<Instruction> Instructions { get; } = new();

		public bool IsFunction => FunctionHeader != null;

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;
		public Instruction LastInstruction => Instructions.Count > 0 ? Instructions[^1] : null;

		public Region (ControlFlowNode node)
		{
			Addr = node.Addr;

			foreach (var instruction in node.Instructions)
			{
				if (instruction is FunctionInstruction func)
				{
					FunctionHeader = func;
				}
				else
				{
					Instructions.Add(instruction);
				}
			};
		}
	}
}
