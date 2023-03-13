using System;
using System.Collections.Generic;

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

		protected BytecodeReader reader = null;
		protected Disassembly disassembly = null;

		// This is used to emulate the STR object used in Torque to return values from files/functions.
		protected bool returnableValue = false;

		public Disassembly Disassemble (FileData fileData)
		{
			reader = new(fileData);
			disassembly = new();

			Disassemble();
			CollectBranches();

			return disassembly;
		}

		protected void Disassemble ()
		{
			while (!reader.IsAtEnd)
			{
				DisassembleNext();
			}
		}

		protected void DisassembleNext ()
		{
			var addr = reader.Index;

			ProcessAddress(addr);

			var op = reader.Read();
			var instruction = DisassembleOpcode(op, addr);

			if (instruction == null)
			{
				throw new DisassemblerException($"Invalid opcode {op} at {addr}");
			}

			ProcessInstruction(instruction);
		}

		protected Instruction DisassembleOpcode(uint op, uint addr)
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

			return DisassembleOpcode(opcode, addr);
		}

		/**
		 * Disassembles the next opcode.
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
		protected Instruction DisassembleOpcode (Opcode opcode, uint addr)
		{
			switch (opcode.Op)
			{
				case Opcode.Value.OP_FUNC_DECL:
				{
					var instruction = new FunctionInstruction(
						opcode,
						addr,
						name: reader.ReadIdent(),
						ns: reader.ReadIdent(),
						package: reader.ReadIdent(),
						hasBody: reader.ReadBool(),
						endAddr: reader.Read()
					);

					var args = reader.Read();

					for (uint i = 0; i < args; i++)
					{
						instruction.Arguments.Add(reader.ReadIdent());
					}

					return instruction;
				}

				case Opcode.Value.OP_CREATE_OBJECT:
				{
					return new CreateObjectInstruction(
						opcode,
						addr,
						parent: reader.ReadIdent(),
						isDataBlock: reader.ReadBool(),
						failJumpAddr: reader.Read()
					);
				}

				case Opcode.Value.OP_ADD_OBJECT:
				{
					return new AddObjectInstruction(opcode, addr, placeAtRoot: reader.ReadBool());
				}

				case Opcode.Value.OP_END_OBJECT:
				{
					return new EndObjectInstruction(opcode, addr, value: reader.ReadBool());
				}

				case Opcode.Value.OP_JMP:
				case Opcode.Value.OP_JMPIF:
				case Opcode.Value.OP_JMPIFF:
				case Opcode.Value.OP_JMPIFNOT:
				case Opcode.Value.OP_JMPIFFNOT:
				case Opcode.Value.OP_JMPIF_NP:
				case Opcode.Value.OP_JMPIFNOT_NP:
				{
					return new BranchInstruction(opcode, addr, targetAddr: reader.Read());
				}

				case Opcode.Value.OP_RETURN:
				{
					return new ReturnInstruction(opcode, addr, returnableValue);
				}

				case Opcode.Value.OP_CMPEQ:
				case Opcode.Value.OP_CMPGR:
				case Opcode.Value.OP_CMPGE:
				case Opcode.Value.OP_CMPLT:
				case Opcode.Value.OP_CMPLE:
				case Opcode.Value.OP_CMPNE:
				case Opcode.Value.OP_XOR:
				case Opcode.Value.OP_MOD:
				case Opcode.Value.OP_BITAND:
				case Opcode.Value.OP_BITOR:
				case Opcode.Value.OP_SHR:
				case Opcode.Value.OP_SHL:
				case Opcode.Value.OP_AND:
				case Opcode.Value.OP_OR:
				case Opcode.Value.OP_ADD:
				case Opcode.Value.OP_SUB:
				case Opcode.Value.OP_MUL:
				case Opcode.Value.OP_DIV:
				{
					return new BinaryInstruction(opcode, addr);
				}

				case Opcode.Value.OP_COMPARE_STR:
				{
					return new BinaryStringInstruction(opcode, addr);
				}

				case Opcode.Value.OP_NEG:
				case Opcode.Value.OP_NOT:
				case Opcode.Value.OP_NOTF:
				case Opcode.Value.OP_ONESCOMPLEMENT:
				{
					return new UnaryInstruction(opcode, addr);
				}

				case Opcode.Value.OP_SETCURVAR:
				case Opcode.Value.OP_SETCURVAR_CREATE:
				{
					return new VariableInstruction(opcode, addr, name: reader.ReadIdent());
				}

				case Opcode.Value.OP_SETCURVAR_ARRAY:
				case Opcode.Value.OP_SETCURVAR_ARRAY_CREATE:
				{
					return new VariableArrayInstruction(opcode, addr);
				}

				case Opcode.Value.OP_LOADVAR_UINT:
				case Opcode.Value.OP_LOADVAR_FLT:
				case Opcode.Value.OP_LOADVAR_STR:
				{
					return new LoadVariableInstruction(opcode, addr);
				}

				case Opcode.Value.OP_SAVEVAR_UINT:
				case Opcode.Value.OP_SAVEVAR_FLT:
				case Opcode.Value.OP_SAVEVAR_STR:
				{
					return new SaveVariableInstruction(opcode, addr);
				}

				case Opcode.Value.OP_SETCUROBJECT:
				{
					return new ObjectInstruction(opcode, addr);
				}

				case Opcode.Value.OP_SETCUROBJECT_NEW:
				{
					return new ObjectNewInstruction(opcode, addr);
				}

				case Opcode.Value.OP_SETCURFIELD:
				{
					return new FieldInstruction(opcode, addr, name: reader.ReadIdent());
				}

				case Opcode.Value.OP_SETCURFIELD_ARRAY:
				{
					return new FieldArrayInstruction(opcode, addr);
				}

				case Opcode.Value.OP_LOADFIELD_UINT:
				case Opcode.Value.OP_LOADFIELD_FLT:
				case Opcode.Value.OP_LOADFIELD_STR:
				{
					return new LoadFieldInstruction(opcode, addr);
				}

				case Opcode.Value.OP_SAVEFIELD_UINT:
				case Opcode.Value.OP_SAVEFIELD_FLT:
				case Opcode.Value.OP_SAVEFIELD_STR:
				{
					return new SaveFieldInstruction(opcode, addr);
				}

				case Opcode.Value.OP_STR_TO_UINT:
				case Opcode.Value.OP_FLT_TO_UINT:
				case Opcode.Value.OP_STR_TO_FLT:
				case Opcode.Value.OP_UINT_TO_FLT:
				case Opcode.Value.OP_FLT_TO_STR:
				case Opcode.Value.OP_UINT_TO_STR:
				case Opcode.Value.OP_STR_TO_NONE:
				case Opcode.Value.OP_STR_TO_NONE_2:
				case Opcode.Value.OP_FLT_TO_NONE:
				case Opcode.Value.OP_UINT_TO_NONE:
				{
					return new ConvertToTypeInstruction(opcode, addr);
				}

				case Opcode.Value.OP_LOADIMMED_UINT:
				{
					return new ImmediateInstruction<uint>(opcode, addr, value: reader.Read());
				}

				case Opcode.Value.OP_LOADIMMED_FLT:
				{
					return new ImmediateInstruction<double>(opcode, addr, value: reader.ReadDouble());
				}

				case Opcode.Value.OP_TAG_TO_STR:
				case Opcode.Value.OP_LOADIMMED_STR:
				{
					return new ImmediateInstruction<string>(opcode, addr, value: reader.ReadString());
				}

				case Opcode.Value.OP_LOADIMMED_IDENT:
				{
					return new ImmediateInstruction<string>(opcode, addr, value: reader.ReadIdent());
				}

				case Opcode.Value.OP_CALLFUNC:
				case Opcode.Value.OP_CALLFUNC_RESOLVE:
				{
					return new CallInstruction(
						opcode,
						addr,
						name: reader.ReadIdent(),
						ns: reader.ReadIdent(),
						callType: reader.Read()
					);
				}

				case Opcode.Value.OP_ADVANCE_STR:
				{
					return new StringInstruction(opcode, addr);
				}

				case Opcode.Value.OP_ADVANCE_STR_APPENDCHAR:
				{
					return new AppendStringInstruction(opcode, addr, reader.ReadChar());
				}

				case Opcode.Value.OP_ADVANCE_STR_COMMA:
				{
					return new CommaStringInstruction(opcode, addr);
				}

				case Opcode.Value.OP_ADVANCE_STR_NUL:
				{
					return new NullStringInstruction(opcode, addr);
				}

				case Opcode.Value.OP_REWIND_STR:
				{
					return new RewindInstruction(opcode, addr);
				}

				case Opcode.Value.OP_TERMINATE_REWIND_STR:
				{
					return new TerminateRewindInstruction(opcode, addr);
				}

				case Opcode.Value.OP_PUSH:
				{
					return new PushInstruction(opcode, addr);
				}

				case Opcode.Value.OP_PUSH_FRAME:
				{
					return new PushFrameInstruction(opcode, addr);
				}

				case Opcode.Value.OP_BREAK:
				{
					return new DebugBreakInstruction(opcode, addr);
				}

				case Opcode.Value.UNUSED1:
				case Opcode.Value.UNUSED2:
				{
					return new UnusedInstruction(opcode, addr);
				}

				case Opcode.Value.OP_INVALID:
				default:
				{
					return null;
				}
			}
		}

		protected void ProcessAddress (uint addr)
		{
			if (reader.InFunction && addr >= reader.FunctionEnd)
			{
				reader.FunctionEnd = 0;
			}
		}

		protected void ProcessInstruction (Instruction instruction)
		{
			ValidateInstruction(instruction);
			SetReturnableValue(instruction);
			Push(instruction);
		}

		protected void ValidateInstruction (Instruction instruction)
		{
			switch (instruction)
			{
				case FunctionInstruction func:
				{
					if (func.HasBody)
					{
						/* See TODO in BytecodeReader. */
						if (reader.InFunction)
						{
							throw new DisassemblerException($"Nested function at {func.Addr}");
						}

						reader.FunctionEnd = func.EndAddr;
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


		// Since branches can jump backwards, we have to do this part in a second pass.
		protected void CollectBranches ()
		{
			foreach (var instruction in disassembly)
			{
				if (instruction is BranchInstruction branch)
				{
					CollectBranch(branch);
				}
			}
		}

		protected void CollectBranch (BranchInstruction branch)
		{
			var source = branch.Addr;
			var target = branch.TargetAddr;

			ValidateBranch(source, target);
			AddBranch(source, target);
		}

		protected void ValidateBranch (uint source, uint target)
		{
			if (disassembly.HasBranch(source))
			{
				throw new DisassemblerException($"Branch at {source} already exists");
			}

			if (target >= reader.Size)
			{
				throw new DisassemblerException($"Branch to invalid address {target}");
			}

			/* See TODO for DisassembleOpcode(). */
			if (!disassembly.HasInstruction(target))
			{
				throw new DisassemblerException($"Branch to non-existent instruction at {target}");
			}
		}

		protected void AddBranch (uint source, uint target)
		{
			disassembly.AddBranch(source, target);
		}

		protected void Push (Instruction instruction)
		{
			disassembly.AddInstruction(instruction);
		}
	}
}
