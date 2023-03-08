using System;

using DSODecompiler.Loader;
using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembler
{
	public class Disassembler
	{
		public class DisassemblerException : Exception
		{
			public DisassemblerException () {}
			public DisassemblerException (string message) : base(message) {}
			public DisassemblerException (string message, Exception inner) : base(message, inner) {}
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

			Disassemble();
			MarkBranchTargets();

			return disassembly;
		}

		protected void Disassemble ()
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
						throw new DisassemblerException($"Invalid branch target {branch.TargetAddr}");
					}

					disassembly.Get(branch.TargetAddr).NumBranchesTo++;
				}
			}
		}

		/**
		 * Disassembles the next instruction.
		 *
		 * Right now we're just doing a linear sweep, but that doesn't handle certain anti-disassembly
		 * techniques like jumping to the middle of an instruction.
		 *
		 * No DSO files currently do this to my knowledge, and probably never will, but I do want
		 * to support it eventually. I tried to with the initial version of this decompiler, but it
		 * became too complicated too quickly, so I'm going to hold off on it for now. I just want a
		 * working decompiler that works with what we have (e.g. Blockland and The Forgettable Dungeon).
		 *
		 * Maybe someday I will implement a recursive descent method instead, because right now it's
		 * very easy to break this decompiler, but for now I'll just keep it simple...
		 *
		 * Tentative TODO: Maybe someday.
		 */
		protected void DisassembleNext()
		{
			var addr = reader.Index;
			var op = reader.Read();
			var instruction = DisassembleOp(op, addr);

			if (instruction == null)
			{
				throw new DisassemblerException($"Invalid opcode {op} at {addr}");
			}

			ProcessInstruction(instruction);
		}

		protected Instruction DisassembleOp (uint op, uint addr)
		{
			Opcode opcode;

			try
			{
				opcode = (Opcode) op;
			}
			catch (InvalidCastException)
			{
				return null;
			}

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

			if (instruction is FuncDeclInsn func)
			{
				ProcessFunctionDecl(func);
			}

			SetReturnableValue(instruction);
			AddInstruction(instruction);
		}

		protected void ValidateInstruction (Instruction instruction)
		{
			var addr = instruction.Addr;
			var opcode = instruction.Opcode;

			// Just in case it wasn't caught during creation.
			if (opcode.Type == OpcodeType.Invalid)
			{
				throw new DisassemblerException($"Opcode with invalid type at {addr}");
			}

			switch (instruction)
			{
				case FuncDeclInsn func:
				{
					/* See TODO in BytecodeReader class. */
					if (func.HasBody && reader.InFunction)
					{
						throw new DisassemblerException($"Nested function declaration at {addr}");
					}

					break;
				}

				case BranchInsn branch:
				{
					if (opcode.BranchType == BranchType.Invalid)
					{
						throw new DisassemblerException($"Invalid BranchType at {addr}");
					}

					if (branch.TargetAddr >= reader.Size)
					{
						throw new DisassemblerException($"Branch at {addr} jumps to invalid address {branch.TargetAddr}");
					}

					break;
				}

				case AdvanceStringInsn str:
				{
					var type = str.Opcode.AdvanceStringType;

					if (type == AdvanceStringType.Invalid)
					{
						throw new DisassemblerException($"Invalid AdvanceStringType at {addr}");
					}

					break;
				}

				default:
				{
					break;
				}
			}
		}

		protected void ProcessFunctionDecl (FuncDeclInsn func)
		{
			if (func.HasBody)
			{
				reader.FunctionEnd = func.EndAddr;
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

		protected void AddInstruction (Instruction instruction)
		{
			disassembly.Add(instruction);
		}
	}
}
