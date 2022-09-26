using System.Collections.Generic;
using DSODecompiler.Util;

namespace DSODecompiler.Disassembler
{
	public class InstructionGraph : Graph<uint, Instruction>
	{
		public delegate void DFSCallbackFn (Instruction instruction, InstructionGraph graph);

		public Instruction EntryPoint => Get (0);

		public void PostorderDFS (DFSCallbackFn callback)
		{
			PostorderDFS (EntryPoint.Key, new HashSet<uint> (), (Instruction instruction) => callback (instruction, this));
		}

		public void PreorderDFS (DFSCallbackFn callback)
		{
			PreorderDFS (EntryPoint.Key, new HashSet<uint> (), (Instruction instruction) => callback (instruction, this));
		}
	}
}
