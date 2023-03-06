using DSODecompiler.Loader;
using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembler
{
	public class Disassembler
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base(message) {}
			public Exception (string message, System.Exception inner) : base(message, inner) {}
		}

		protected Disassembly disassembly = null;
		protected BytecodeReader reader = null;

		// This is used to emulate the STR object used in Torque to return values from files/functions.
		protected bool returnableValue = false;

		public Disassembly Disassemble (FileData data)
		{
			reader = new(data);
			disassembly = new();
			returnableValue = false;
			reader.FunctionEnd = 0;

			InitialDisassembly();
			MarkBranchTargets();

			return disassembly;
		}

		protected void InitialDisassembly ()
		{
			while (!reader.IsAtEnd)
			{
				DisassembleNext();
			}
		}

		/* Since branches can jump backwards, we have to do this on a second pass. */
		protected void MarkBranchTargets()
		{
			foreach (var instruction in disassembly.GetInstructions())
			{
				if (instruction is BranchInsn branch)
				{
					/* See TODO below. */
					if (!disassembly.Has(branch.TargetAddr))
					{
						throw new Exception($"Invalid branch target {branch.TargetAddr}");
					}

					disassembly[branch.TargetAddr].IsBranchTarget = true;
				}
			}
		}

		/**
		 * Disassembles the next instruction.
		 *
		 * Right now we're just doing a linear sweep, but that doesn't handle certain anti-disassembly
		 * techniques like jumping to the middle of an instruction.
		 *
		 * No DSO files do this currently to my knowledge, and probably never will, but I do want
		 * to support that eventually. I tried to with the initial version of this decompiler, but
		 * it became too complicated too quickly, so I'm going to hold off on it for now. I just want
		 * a working decompiler that works with what we have now (e.g. Blockland and The Forgettable Dungeon).
		 *
		 * Maybe someday I will implement a recursive descent method instead, because right now it's
		 * very easy to break this decompiler, but for now I'll just keep it simple...
		 *
		 * Tentative TODO: Maybe someday.
		 */
		protected void DisassembleNext()
		{
			var addr = reader.Index;
			var opcode = reader.Read();
			var instruction = DisassembleOp((Opcode) opcode, addr);

			if (instruction == null)
			{
				throw new Exception($"Invalid opcode {opcode} at {addr}");
			}

			ProcessInstruction(instruction);
		}

		protected Instruction DisassembleOp (Opcode opcode, uint addr)
		{
			ProcessAddress(addr);

			return InstructionFactory.Create(opcode, addr, reader, returnableValue);
		}

		protected void ProcessAddress (uint addr)
		{
			if (addr >= reader.FunctionEnd)
			{
				reader.FunctionEnd = 0;
			}
		}

		protected void ProcessInstruction (Instruction instruction)
		{
			ValidateInstruction(instruction);
			SetReturnableValue(instruction);

			disassembly.Add(instruction);
		}

		protected void ValidateInstruction (Instruction instruction)
		{
			var opcode = instruction.Opcode;
			var addr = instruction.Addr;

			switch (instruction)
			{
				case FuncDeclInsn func:
				{
					if (func.HasBody)
					{
						if (reader.InFunction)
						{
							throw new Exception($"Nested function declaration at {addr}");
						}

						reader.FunctionEnd = func.EndAddr;
					}

					break;
				}

				case BranchInsn branch:
				{
					if (opcode.BranchType == BranchType.Invalid)
					{
						throw new Exception($"Invalid branch type at {addr}");
					}

					if (branch.TargetAddr >= reader.CodeSize)
					{
						throw new Exception($"Branch at {addr} jumps to invalid address {branch.TargetAddr}");
					}

					break;
				}

				case AdvanceStringInsn str:
				{
					var type = str.Opcode.AdvanceStringType;

					if (type == AdvanceStringType.Invalid)
					{
						throw new Exception($"Invalid advance string type at {addr}");
					}

					break;
				}

				default:
				{
					break;
				}
			}
		}

		protected void SetReturnableValue (Instruction instruction)
		{
			switch (instruction.Opcode.ReturnValue)
			{
				case ReturnValue.ToFalse:
				{
					returnableValue = false;
					break;
				}

				case ReturnValue.ToTrue:
				{
					returnableValue = true;
					break;
				}

				case ReturnValue.NoChange:
				default:
				{
					break;
				}
			}
		}
	}
}
