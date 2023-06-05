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
			// Enumerating over dictionaries isn't reliable, so we want to make sure we're iterating
			// in the proper order.
			var values = new List<Instruction>(instructions.Values);
			values.Sort((insn1, insn2) => insn1.Addr.CompareTo(insn2.Addr));

			foreach (var instruction in values)
			{
				yield return instruction;
			}
		}
	}
}
