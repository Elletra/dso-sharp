using System.Collections.Generic;

using DSODecompiler.Loader;

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
		protected FileData fileData = null;
		protected Queue<uint> queue = null;
		protected Instruction prevInsn = null;

		// This is used to emulate the STR object used in Torque to return values from files/functions.
		protected bool returnableValue = false;

		protected uint index = 0;

		protected bool IsAtEnd => index >= fileData.CodeSize;

		public Disassembly Disassemble (FileData data)
		{
			disassembly = new();
			fileData = data;
			queue = new();
			index = 0;

			StartDisassemble();

			return disassembly;
		}

		protected void StartDisassemble ()
		{
			queue.Enqueue(0);

			/* The queue should typically only have 1 item in it. The reason we use a queue at all
			   is to disassemble instructions that jump to the middle of an instruction to try to
			   avoid disassembly.

			   I highly, *highly* doubt there are DSO files that actually do that, but it's still
			   good to do this... */
			while (queue.Count > 0)
			{
				DisassembleFromAddr(queue.Dequeue());
			}
		}

		protected void DisassembleFromAddr (uint fromAddr)
		{
			index = fromAddr;
			prevInsn = null;
			returnableValue = false;

			while (!IsAtEnd && !HasVisited(index))
			{
				var addr = index;
				var opcode = Read();
				var instruction = DisassembleOp((Opcode) opcode, addr);

				if (instruction == null)
				{
					throw new Exception($"Invalid opcode {opcode} at {addr}");
				}

				ProcessInstruction(instruction);
			}
		}

		protected Instruction DisassembleOp (Opcode opcode, uint addr)
		{
			switch (opcode.Op)
			{
				case Opcode.Ops.OP_FUNC_DECL:
				{
					var instruction = new FuncDeclInsn(opcode, addr)
					{
						Name = ReadIdent(),
						Namespace = ReadIdent(),
						Package = ReadIdent(),
						HasBody = ReadBool(),
						EndAddr = Read(),
					};

					var args = Read();

					for (uint i = 0; i < args; i++)
					{
						instruction.Arguments.Add(ReadIdent());
					}

					return instruction;
				}

				case Opcode.Ops.OP_CREATE_OBJECT:
				{
					var instruction = new CreateObjectInsn(opcode, addr)
					{
						ParentName = ReadIdent(),
						IsDataBlock = ReadBool(),
						FailJumpAddr = Read(),
					};

					return instruction;
				}

				case Opcode.Ops.OP_ADD_OBJECT:
				{
					return new AddObjectInsn(opcode, addr, ReadBool());
				}

				case Opcode.Ops.OP_END_OBJECT:
				{
					return new EndObjectInsn(opcode, addr, ReadBool());
				}

				case Opcode.Ops.OP_JMP:
				case Opcode.Ops.OP_JMPIF:
				case Opcode.Ops.OP_JMPIFF:
				case Opcode.Ops.OP_JMPIFNOT:
				case Opcode.Ops.OP_JMPIFFNOT:
				case Opcode.Ops.OP_JMPIF_NP:
				case Opcode.Ops.OP_JMPIFNOT_NP:
				{
					var type = opcode.GetBranchType();

					if (type == Opcode.BranchType.Invalid)
					{
						throw new Exception($"Invalid branch type at {addr}");
					}

					var target = Read();

					if (target >= fileData.CodeSize)
					{
						throw new Exception($"Branch at {addr} jumps to invalid address {target}");
					}

					queue.Enqueue(target);
					disassembly.AddBranchTarget(target);

					return new BranchInsn(opcode, addr, target, type);
				}

				case Opcode.Ops.OP_RETURN:
				{
					var instruction = new ReturnInsn(opcode, addr, returnsValue: returnableValue);
					returnableValue = false;

					return instruction;
				}

				case Opcode.Ops.OP_CMPEQ:
				case Opcode.Ops.OP_CMPGR:
				case Opcode.Ops.OP_CMPGE:
				case Opcode.Ops.OP_CMPLT:
				case Opcode.Ops.OP_CMPLE:
				case Opcode.Ops.OP_CMPNE:
				case Opcode.Ops.OP_XOR:
				case Opcode.Ops.OP_MOD:
				case Opcode.Ops.OP_BITAND:
				case Opcode.Ops.OP_BITOR:
				case Opcode.Ops.OP_SHR:
				case Opcode.Ops.OP_SHL:
				case Opcode.Ops.OP_AND:
				case Opcode.Ops.OP_OR:
				case Opcode.Ops.OP_ADD:
				case Opcode.Ops.OP_SUB:
				case Opcode.Ops.OP_MUL:
				case Opcode.Ops.OP_DIV:
				{
					return new BinaryInsn(opcode, addr);
				}

				case Opcode.Ops.OP_COMPARE_STR:
				{
					return new StringCompareInsn(opcode, addr);
				}

				case Opcode.Ops.OP_NEG:
				case Opcode.Ops.OP_NOT:
				case Opcode.Ops.OP_NOTF:
				case Opcode.Ops.OP_ONESCOMPLEMENT:
				{
					return new UnaryInsn(opcode, addr);
				}

				case Opcode.Ops.OP_SETCURVAR:
				case Opcode.Ops.OP_SETCURVAR_CREATE:
				{
					return new SetCurVarInsn(opcode, addr, ReadIdent());
				}

				case Opcode.Ops.OP_SETCURVAR_ARRAY:
				case Opcode.Ops.OP_SETCURVAR_ARRAY_CREATE:
				{
					return new SetCurVarArrayInsn(opcode, addr);
				}

				case Opcode.Ops.OP_LOADVAR_STR:
				{
					returnableValue = true;

					return new LoadVarInsn(opcode, addr);
				}

				case Opcode.Ops.OP_LOADVAR_UINT:
				case Opcode.Ops.OP_LOADVAR_FLT:
				{
					return new LoadVarInsn(opcode, addr);
				}

				case Opcode.Ops.OP_SAVEVAR_UINT:
				case Opcode.Ops.OP_SAVEVAR_FLT:
				case Opcode.Ops.OP_SAVEVAR_STR:
				{
					returnableValue = true;

					return new SaveVarInsn(opcode, addr);
				}

				case Opcode.Ops.OP_SETCUROBJECT:
				case Opcode.Ops.OP_SETCUROBJECT_NEW:
				{
					return new SetCurObjectInsn(opcode, addr, opcode.Op == Opcode.Ops.OP_SETCUROBJECT_NEW);
				}

				case Opcode.Ops.OP_SETCURFIELD:
				{
					return new SetCurFieldInsn(opcode, addr, ReadIdent());
				}

				case Opcode.Ops.OP_SETCURFIELD_ARRAY:
				{
					return new SetCurFieldArrayInsn(opcode, addr);
				}

				case Opcode.Ops.OP_LOADFIELD_STR:
				{
					returnableValue = true;

					return new LoadFieldInsn(opcode, addr);
				}

				case Opcode.Ops.OP_LOADFIELD_UINT:
				case Opcode.Ops.OP_LOADFIELD_FLT:
				{
					return new LoadFieldInsn(opcode, addr);
				}

				case Opcode.Ops.OP_SAVEFIELD_UINT:
				case Opcode.Ops.OP_SAVEFIELD_FLT:
				case Opcode.Ops.OP_SAVEFIELD_STR:
				{
					returnableValue = true;

					return new SaveFieldInsn(opcode, addr);
				}

				case Opcode.Ops.OP_STR_TO_UINT:
				case Opcode.Ops.OP_FLT_TO_UINT:
				case Opcode.Ops.OP_STR_TO_FLT:
				case Opcode.Ops.OP_UINT_TO_FLT:
				{
					return new ConvertToTypeInsn(opcode, addr, Opcode.ConvertToType.Float);
				}

				case Opcode.Ops.OP_FLT_TO_STR:
				case Opcode.Ops.OP_UINT_TO_STR:
				{
					returnableValue = true;

					return new ConvertToTypeInsn(opcode, addr, Opcode.ConvertToType.String);
				}

				case Opcode.Ops.OP_STR_TO_NONE:
				case Opcode.Ops.OP_STR_TO_NONE_2:
				case Opcode.Ops.OP_FLT_TO_NONE:
				case Opcode.Ops.OP_UINT_TO_NONE:
				{
					returnableValue = false;

					return new ConvertToTypeInsn(opcode, addr, Opcode.ConvertToType.None);
				}

				case Opcode.Ops.OP_LOADIMMED_UINT:
				case Opcode.Ops.OP_LOADIMMED_FLT:
				case Opcode.Ops.OP_TAG_TO_STR:
				case Opcode.Ops.OP_LOADIMMED_STR:
				case Opcode.Ops.OP_LOADIMMED_IDENT:
				{
					returnableValue = true;

					return new LoadImmedInsn(opcode, addr, Read());
				}

				case Opcode.Ops.OP_CALLFUNC:
				case Opcode.Ops.OP_CALLFUNC_RESOLVE:
				{
					returnableValue = true;

					return new FuncCallInsn(opcode, addr)
					{
						Name = ReadIdent(),
						Namespace = ReadIdent(),
						CallType = Read(),
					};
				}

				case Opcode.Ops.OP_ADVANCE_STR:
				case Opcode.Ops.OP_ADVANCE_STR_APPENDCHAR:
				case Opcode.Ops.OP_ADVANCE_STR_COMMA:
				case Opcode.Ops.OP_ADVANCE_STR_NUL:
				{
					var type = opcode.GetAdvanceStringType();

					if (type == Opcode.AdvanceStringType.Invalid)
					{
						throw new Exception($"Invalid advance string type at {addr}");
					}

					if (type == Opcode.AdvanceStringType.Append)
					{
						return new AdvanceStringInsn(opcode, addr, type, ReadChar());
					}

					return new AdvanceStringInsn(opcode, addr, type);
				}

				case Opcode.Ops.OP_REWIND_STR:
				case Opcode.Ops.OP_TERMINATE_REWIND_STR:
				{
					returnableValue = true;

					return new RewindInsn(opcode, addr, opcode.Op == Opcode.Ops.OP_TERMINATE_REWIND_STR);
				}

				case Opcode.Ops.OP_PUSH:
				{
					return new PushInsn(opcode, addr);
				}

				case Opcode.Ops.OP_PUSH_FRAME:
				{
					return new PushFrameInsn(opcode, addr);
				}

				case Opcode.Ops.OP_BREAK:
				{
					return new DebugBreakInsn(opcode, addr);
				}

				case Opcode.Ops.UNUSED1:
				case Opcode.Ops.UNUSED2:
				{
					return new UnusedInsn(opcode, addr);
				}

				case Opcode.Ops.OP_INVALID:
				default:
				{
					return null;
				}
			}
		}

		protected void ProcessInstruction (Instruction instruction)
		{
			if (prevInsn != null)
			{
				prevInsn.Next = instruction;
				instruction.Prev = prevInsn;

				/* We do this for instructions that jump in the middle of an instruction. If we're
				   disassembling from one, then there will eventually be an instruction we've
				   already visited that comes next, so we want to hook them up. */
				if (disassembly.Has(index))
				{
					instruction.Next = disassembly[index];
				}
			}

			disassembly.Add(instruction);

			prevInsn = instruction;
		}

		protected uint Read () => fileData.Op(index++);
		protected bool ReadBool () => Read() != 0;
		protected char ReadChar () => (char) Read();
		protected string ReadIdent () => fileData.Identifer(index, Read());

		protected bool HasVisited (uint addr) => disassembly.Has(addr);
	}
}
