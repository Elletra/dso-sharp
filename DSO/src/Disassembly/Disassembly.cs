using System.Collections.Generic;

namespace DSO.Decompiler.Disassembly
{
	public class Disassembly
	{
		private readonly Dictionary<uint, Instruction> instructions = new();
		private readonly HashSet<uint> branchTargets = new();

		/**
		 * Instruction methods
		 */

		public Instruction AddInstruction(Instruction instruction)
		{
			instructions[instruction.Addr] = instruction;

			return instruction;
		}

		public Instruction GetInstruction(uint addr) => HasInstruction(addr) ? instructions[addr] : null;
		public bool HasInstruction(uint addr) => instructions.ContainsKey(addr);

		public List<Instruction> GetInstructions()
		{
			var values = new List<Instruction>(instructions.Values);

			// TODO: If I ever implement recursive descent disassembly, this will not work.
			values.Sort((insn1, insn2) => insn1.Addr.CompareTo(insn2.Addr));

			return values;
		}

		/// <summary>
		/// Returns the instructions split up by function/block.
		/// </summary>
		/// <returns></returns>
		public List<InstructionBlock> GetSplitInstructions() => new DisassemblySplitter().Split(this);

		/**
		 * Branch target methods
		 */

		public void AddBranchTarget(uint addr) => branchTargets.Add(addr);
		public bool HasBranchTarget(uint addr) => branchTargets.Contains(addr);
	}
}
