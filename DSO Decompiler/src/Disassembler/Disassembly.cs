﻿using System.Collections.Generic;

namespace DSODecompiler.Disassembler
{
	public class Disassembly
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base (message) {}
			public Exception (string message, System.Exception inner) : base (message, inner) {}
		}

		public Instruction EntryPoint => instructions.Count > 0 ? instructions[0] : null;

		protected Dictionary<uint, Instruction> addrToInsn = new Dictionary<uint, Instruction> ();
		protected List<Instruction> instructions = new List<Instruction> ();

		public int Count => instructions.Count;

		public Instruction Add (Instruction instruction)
		{
			var addr = instruction.Addr;

			if (addrToInsn.ContainsKey (addr) && addrToInsn[addr] != instruction)
			{
				throw new Exception ($"Instruction already exists at ${addr}");
			}

			addrToInsn[addr] = instruction;
			instructions.Add (instruction);

			return instruction;
		}

		public IEnumerable<Instruction> GetInstructions ()
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}

		public bool Has (uint addr) => addrToInsn.ContainsKey (addr);
		public Instruction Get (uint addr) => Has (addr) ? addrToInsn[addr] : null;
		public Instruction At (int index) => index < 0 ? instructions[index] : null;
	}
}