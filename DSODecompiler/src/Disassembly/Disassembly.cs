using System.Collections.Generic;

namespace DSODecompiler.Disassembly
{
	public class Disassembly
	{
		protected Dictionary<uint, Instruction> instructions = new();
		protected HashSet<uint> branchTargets = new();

		/**
		 * Instruction methods
		 */

		public Instruction AddInstruction (Instruction instruction)
		{
			instructions[instruction.Addr] = instruction;

			return instruction;
		}

		public Instruction GetInstruction (uint addr) => HasInstruction(addr) ? instructions[addr] : null;
		public bool HasInstruction (uint addr) => instructions.ContainsKey(addr);

		public List<Instruction> GetInstructions ()
		{
			var values = new List<Instruction>(instructions.Values);

			// TODO: If I ever implement recursive descent disassembly, this will not work.
			values.Sort((insn1, insn2) => insn1.Addr.CompareTo(insn2.Addr));

			return values;
		}

		/**
		 * Branch target methods
		 */

		public void AddBranchTarget (uint addr) => branchTargets.Add(addr);
		public bool HasBranchTarget (uint addr) => branchTargets.Contains(addr);
	}
}
