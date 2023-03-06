using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Disassembly
	{
		protected Dictionary<uint, Instruction> instructions = new();
		protected HashSet<uint> branchTargets = new();

		public Instruction EntryPoint { get => Get(0); }

		public bool Has (uint addr) => instructions.ContainsKey(addr);
		public Instruction Get (uint addr) => Has(addr) ? instructions[addr] : null;
		public Instruction this[uint addr] => Get(addr);

		public void AddBranchTarget (uint addr) => branchTargets.Add(addr);
		public bool IsBranchTarget (uint addr) => branchTargets.Contains(addr);

		public bool Add (Instruction instruction)
		{
			if (Has(instruction.Addr))
			{
				return false;
			}

			instructions.Add(instruction.Addr, instruction);

			return true;
		}

		public IEnumerable<Instruction> GetInstructions ()
		{
			var queue = new Queue<Instruction>();
			var visited = new HashSet<Instruction>();

			queue.Enqueue(EntryPoint);

			while (queue.Count > 0 && !visited.Contains(queue.Peek()))
			{
				var instruction = queue.Dequeue();

				while (instruction != null)
				{
					yield return instruction;

					visited.Add(instruction);

					if (instruction is BranchInsn branch && Has(branch.Addr))
					{
						queue.Enqueue(Get(branch.Addr));
					}

					instruction = instruction.Next;
				}
			}
		}
	}
}
