using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Disassembly
	{
		protected Dictionary<uint, Instruction> instructions = new();
		protected HashSet<uint> jumpTargets = new();

		public Instruction EntryPoint { get => Get(0); }

		public bool Has (uint addr) => instructions.ContainsKey(addr);
		public Instruction Get (uint addr) => Has(addr) ? instructions[addr] : null;
		public Instruction this[uint addr] => Get(addr);

		public void AddJumpTarget (uint addr) => jumpTargets.Add(addr);
		public bool IsJumpTarget (uint addr) => jumpTargets.Contains(addr);

		public bool Add (Instruction instruction)
		{
			if (Has(instruction.Addr))
			{
				return false;
			}

			instructions.Add(instruction.Addr, instruction);

			return true;
		}
	}

	public class DisassemblyTraverser
	{
		public delegate void VisitFn (Instruction instruction);

		protected Queue<Instruction> queue = null;
		protected HashSet<Instruction> visited = null;

		public void Traverse (Disassembly disassembly, VisitFn visitFunc)
		{
			queue = new();
			visited = new();

			StartTraverse(disassembly, visitFunc);
		}

		protected void StartTraverse (Disassembly disassembly, VisitFn visitFunc)
		{
			queue.Enqueue(disassembly.EntryPoint);

			while (queue.Count > 0 && !HasVisited(queue.Peek()))
			{
				TraverseFrom(queue.Dequeue(), disassembly, visitFunc);
			}
		}

		protected void TraverseFrom (Instruction fromInsn, Disassembly disassembly, VisitFn visitFunc)
		{
			var instruction = fromInsn;

			while (instruction != null)
			{
				Visit(instruction, disassembly, visitFunc);

				instruction = instruction.Next;
			}
		}

		protected void Visit (Instruction instruction, Disassembly disassembly, VisitFn visitFunc)
		{
			visitFunc(instruction);

			if (instruction is BranchInsn branch && disassembly.Has(branch.TargetAddr))
			{
				queue.Enqueue(disassembly[branch.TargetAddr]);
			}

			visited.Add(instruction);
		}

		protected bool HasVisited (Instruction instruction) => visited.Contains(instruction);
	}
}
