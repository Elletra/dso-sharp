using System.Collections.Generic;

using DSODecompiler.Util;

namespace DSODecompiler.Disassembler
{
	public class Branch
	{
		public uint Source { get; }
		public uint Target { get; }

		public Branch (uint source, uint target)
		{
			Source = source;
			Target = target;
		}

		public override string ToString () => $"{GetType().Name}({Source}=>{Target})";
	}

	public class Disassembly
	{
		protected List<Instruction> instructions = new();
		protected Dictionary<uint, Instruction> addrToInstruction = new();
		protected Dictionary<uint, Branch> branches = new();
		protected Multidictionary<uint, Branch> branchTargets = new();

		public int NumInstructions => instructions.Count;
		public int NumBranches => branches.Count;

		public bool AddInstruction (Instruction instruction)
		{
			if (HasInstruction(instruction.Addr))
			{
				return false;
			}

			instructions.Add(instruction);
			addrToInstruction.Add(instruction.Addr, instruction);

			return true;
		}

		public bool HasInstruction (uint addr) => addrToInstruction.ContainsKey(addr);
		public Instruction GetInstruction (uint addr) => HasInstruction(addr) ? addrToInstruction[addr] : null;

		public IEnumerator<Instruction> GetEnumerator ()
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}

		public bool AddBranch (uint source, uint target)
		{
			if (HasBranch(source))
			{
				return false;
			}

			var branch = new Branch(source, target);

			branches.Add(source, branch);
			branchTargets.Add(target, branch);

			return true;
		}

		public bool HasBranch (uint source) => branches.ContainsKey(source);
		public Branch GetBranch (uint source) => HasBranch(source) ? branches[source] : null;

		public bool HasBranchTarget (uint target) => branchTargets.ContainsKey(target);

		public IEnumerable<Branch> GetBranches () => branches.Values;
		public IEnumerable<Branch> GetBranchesTo (uint target) => branchTargets.GetValues(target);
	}
}
