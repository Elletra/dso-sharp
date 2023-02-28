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
	}

	public class DisassemblyTraverser
	{
		public delegate void VisitFn (Instruction instruction, Disassembly disassembly);
		public delegate void TraverseFromFn (Instruction instruction, Disassembly disassembly);

		protected Queue<Instruction> queue = null;
		protected HashSet<Instruction> visited = null;

		protected VisitFn visitFunc;
		protected TraverseFromFn traverseFromFunc;

		public void Traverse (Disassembly disassembly, VisitFn visitFn, TraverseFromFn traverseFromFn)
		{
			visitFunc = visitFn ?? DefaultVisitFunc;
			traverseFromFunc = traverseFromFn ?? DefaultTraverseFromFunc;
			queue = new();
			visited = new();

			StartTraverse(disassembly);
		}

		protected void StartTraverse (Disassembly disassembly)
		{
			queue.Enqueue(disassembly.EntryPoint);

			while (queue.Count > 0 && !HasVisited(queue.Peek()))
			{
				TraverseFrom(queue.Dequeue(), disassembly);
			}
		}

		protected void TraverseFrom (Instruction fromInsn, Disassembly disassembly)
		{
			traverseFromFunc(fromInsn, disassembly);

			var instruction = fromInsn;

			while (instruction != null)
			{
				Visit(instruction, disassembly);

				instruction = instruction.Next;
			}
		}

		protected void Visit (Instruction instruction, Disassembly disassembly)
		{
			visitFunc(instruction, disassembly);

			if (instruction is BranchInsn branch && disassembly.Has(branch.TargetAddr))
			{
				queue.Enqueue(disassembly[branch.TargetAddr]);
			}

			visited.Add(instruction);
		}

		protected bool HasVisited (Instruction instruction) => visited.Contains(instruction);

		protected void DefaultVisitFunc (Instruction instruction, Disassembly disassembly) {}
		protected void DefaultTraverseFromFunc (Instruction instruction, Disassembly disassembly) {}
	}
}
