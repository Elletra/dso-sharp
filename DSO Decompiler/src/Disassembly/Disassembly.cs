using System.Collections.Generic;

namespace DSODecompiler.Disassembly
{
	public class Disassembly
	{
		protected Dictionary<uint, Instruction> instructions = new();

		public Instruction AddInstruction (Instruction instruction)
		{
			instructions[instruction.Addr] = instruction;

			return instruction;
		}

		public Instruction GetInstruction (uint addr) => HasInstruction(addr) ? instructions[addr] : null;
		public bool HasInstruction (uint addr) => instructions.ContainsKey(addr);

		public IEnumerable<Instruction> GetInstructions ()
		{
			foreach (var (_, instruction) in instructions)
			{
				yield return instruction;
			}
		}
	}
}
