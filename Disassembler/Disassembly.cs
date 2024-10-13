using System.Collections;

namespace DSO.Disassembler
{
	public class Disassembly : IEnumerable<Instruction>
	{
		private readonly List<Instruction> _list = [];
		private readonly Dictionary<uint, Instruction> _dictionary = [];

		public Instruction? First => _list.Count > 0 ? _list[0] : null;
		public Instruction? Last => _list.Count > 0 ? _list[^1] : null;

		public readonly List<BranchInstruction> Branches = [];

		public Instruction AddInstruction(Instruction instruction)
		{
			var last = Last;

			if (last != null)
			{
				last.Next = instruction;
				instruction.Prev = last;
			}

			if (instruction is BranchInstruction branch)
			{
				Branches.Add(branch);
			}

			_list.Add(instruction);
			_dictionary[instruction.Address] = instruction;

			return instruction;
		}

		public bool HasInstruction(uint address) => _dictionary.ContainsKey(address);
		public Instruction? GetInstruction(uint address) => HasInstruction(address) ? _dictionary[address] : null;
		public List<Instruction> GetInstructions() => [.._list];

		public List<Instruction> SliceInstructions(uint fromAddress, uint toAddress)
		{
			var slice = new List<Instruction>();

			for (var instruction = _dictionary[fromAddress]; instruction != null && instruction.Address <= toAddress; instruction = instruction.Next)
			{
				slice.Add(instruction);
			}

			return slice;
		}

		public IEnumerator<Instruction> GetEnumerator()
		{
			for (var instruction = First; instruction != null; instruction = instruction.Next)
			{
				yield return instruction;
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
