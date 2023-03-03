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
				var op = Read();
				var instruction = DisassembleOp((Opcodes.Ops) op, addr);

				if (instruction == null)
				{
					throw new Exception($"Invalid opcode {op} at {addr}");
				}

				ProcessInstruction(instruction);
			}
		}

		protected Instruction DisassembleOp (Opcodes.Ops op, uint addr)
		{
			switch (op)
			{
				case Opcodes.Ops.OP_FUNC_DECL:
				{
					var instruction = new FuncDeclInsn(op, addr)
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

				case Opcodes.Ops.OP_CREATE_OBJECT:
				{
					var instruction = new CreateObjectInsn(op, addr)
					{
						ParentName = ReadIdent(),
						IsDataBlock = ReadBool(),
						FailJumpAddr = Read(),
					};

					return instruction;
				}

				case Opcodes.Ops.OP_ADD_OBJECT:
				{
					return new AddObjectInsn(op, addr, ReadBool());
				}

				case Opcodes.Ops.OP_END_OBJECT:
				{
					return new EndObjectInsn(op, addr, ReadBool());
				}

				case Opcodes.Ops.OP_JMP:
				case Opcodes.Ops.OP_JMPIF:
				case Opcodes.Ops.OP_JMPIFF:
				case Opcodes.Ops.OP_JMPIFNOT:
				case Opcodes.Ops.OP_JMPIFFNOT:
				case Opcodes.Ops.OP_JMPIF_NP:
				case Opcodes.Ops.OP_JMPIFNOT_NP:
				{
					var type = Opcodes.GetBranchType(op);

					if (type == Opcodes.BranchType.Invalid)
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

					return new BranchInsn(op, addr, target, type);
				}

				case Opcodes.Ops.OP_RETURN:
				{
					var instruction = new ReturnInsn(op, addr, returnsValue: returnableValue);
					returnableValue = false;

					return instruction;
				}

				case Opcodes.Ops.OP_CMPEQ:
				case Opcodes.Ops.OP_CMPGR:
				case Opcodes.Ops.OP_CMPGE:
				case Opcodes.Ops.OP_CMPLT:
				case Opcodes.Ops.OP_CMPLE:
				case Opcodes.Ops.OP_CMPNE:
				case Opcodes.Ops.OP_XOR:
				case Opcodes.Ops.OP_MOD:
				case Opcodes.Ops.OP_BITAND:
				case Opcodes.Ops.OP_BITOR:
				case Opcodes.Ops.OP_SHR:
				case Opcodes.Ops.OP_SHL:
				case Opcodes.Ops.OP_AND:
				case Opcodes.Ops.OP_OR:
				case Opcodes.Ops.OP_ADD:
				case Opcodes.Ops.OP_SUB:
				case Opcodes.Ops.OP_MUL:
				case Opcodes.Ops.OP_DIV:
				{
					return new BinaryInsn(op, addr);
				}

				case Opcodes.Ops.OP_COMPARE_STR:
				{
					return new StringCompareInsn(op, addr);
				}

				case Opcodes.Ops.OP_NEG:
				case Opcodes.Ops.OP_NOT:
				case Opcodes.Ops.OP_NOTF:
				case Opcodes.Ops.OP_ONESCOMPLEMENT:
				{
					return new UnaryInsn(op, addr);
				}

				case Opcodes.Ops.OP_SETCURVAR:
				case Opcodes.Ops.OP_SETCURVAR_CREATE:
				{
					return new SetCurVarInsn(op, addr, ReadIdent());
				}

				case Opcodes.Ops.OP_SETCURVAR_ARRAY:
				case Opcodes.Ops.OP_SETCURVAR_ARRAY_CREATE:
				{
					return new SetCurVarArrayInsn(op, addr);
				}

				case Opcodes.Ops.OP_LOADVAR_STR:
				{
					returnableValue = true;

					return new LoadVarInsn(op, addr);
				}

				case Opcodes.Ops.OP_LOADVAR_UINT:
				case Opcodes.Ops.OP_LOADVAR_FLT:
				{
					return new LoadVarInsn(op, addr);
				}

				case Opcodes.Ops.OP_SAVEVAR_UINT:
				case Opcodes.Ops.OP_SAVEVAR_FLT:
				case Opcodes.Ops.OP_SAVEVAR_STR:
				{
					returnableValue = true;

					return new SaveVarInsn(op, addr);
				}

				case Opcodes.Ops.OP_SETCUROBJECT:
				case Opcodes.Ops.OP_SETCUROBJECT_NEW:
				{
					return new SetCurObjectInsn(op, addr, op == Opcodes.Ops.OP_SETCUROBJECT_NEW);
				}

				case Opcodes.Ops.OP_SETCURFIELD:
				{
					return new SetCurFieldInsn(op, addr, ReadIdent());
				}

				case Opcodes.Ops.OP_SETCURFIELD_ARRAY:
				{
					return new SetCurFieldArrayInsn(op, addr);
				}

				case Opcodes.Ops.OP_LOADFIELD_STR:
				{
					returnableValue = true;

					return new LoadFieldInsn(op, addr);
				}

				case Opcodes.Ops.OP_LOADFIELD_UINT:
				case Opcodes.Ops.OP_LOADFIELD_FLT:
				{
					return new LoadFieldInsn(op, addr);
				}

				case Opcodes.Ops.OP_SAVEFIELD_UINT:
				case Opcodes.Ops.OP_SAVEFIELD_FLT:
				case Opcodes.Ops.OP_SAVEFIELD_STR:
				{
					returnableValue = true;

					return new SaveFieldInsn(op, addr);
				}

				case Opcodes.Ops.OP_STR_TO_UINT:
				case Opcodes.Ops.OP_FLT_TO_UINT:
				case Opcodes.Ops.OP_STR_TO_FLT:
				case Opcodes.Ops.OP_UINT_TO_FLT:
				{
					return new ConvertToTypeInsn(op, addr, Opcodes.ConvertToType.Float);
				}

				case Opcodes.Ops.OP_FLT_TO_STR:
				case Opcodes.Ops.OP_UINT_TO_STR:
				{
					returnableValue = true;

					return new ConvertToTypeInsn(op, addr, Opcodes.ConvertToType.String);
				}

				case Opcodes.Ops.OP_STR_TO_NONE:
				case Opcodes.Ops.OP_STR_TO_NONE_2:
				case Opcodes.Ops.OP_FLT_TO_NONE:
				case Opcodes.Ops.OP_UINT_TO_NONE:
				{
					returnableValue = false;

					return new ConvertToTypeInsn(op, addr, Opcodes.ConvertToType.None);
				}

				case Opcodes.Ops.OP_LOADIMMED_UINT:
				case Opcodes.Ops.OP_LOADIMMED_FLT:
				case Opcodes.Ops.OP_TAG_TO_STR:
				case Opcodes.Ops.OP_LOADIMMED_STR:
				case Opcodes.Ops.OP_LOADIMMED_IDENT:
				{
					returnableValue = true;

					return new LoadImmedInsn(op, addr, Read());
				}

				case Opcodes.Ops.OP_CALLFUNC:
				case Opcodes.Ops.OP_CALLFUNC_RESOLVE:
				{
					returnableValue = true;

					return new FuncCallInsn(op, addr)
					{
						Name = ReadIdent(),
						Namespace = ReadIdent(),
						CallType = Read(),
					};
				}

				case Opcodes.Ops.OP_ADVANCE_STR:
				case Opcodes.Ops.OP_ADVANCE_STR_APPENDCHAR:
				case Opcodes.Ops.OP_ADVANCE_STR_COMMA:
				case Opcodes.Ops.OP_ADVANCE_STR_NUL:
				{
					var type = Opcodes.GetAdvanceStringType(op);

					if (type == Opcodes.AdvanceStringType.Invalid)
					{
						throw new Exception($"Invalid advance string type at {addr}");
					}

					if (type == Opcodes.AdvanceStringType.Append)
					{
						return new AdvanceStringInsn(op, addr, type, ReadChar());
					}

					return new AdvanceStringInsn(op, addr, type);
				}

				case Opcodes.Ops.OP_REWIND_STR:
				case Opcodes.Ops.OP_TERMINATE_REWIND_STR:
				{
					returnableValue = true;

					return new RewindInsn(op, addr, op == Opcodes.Ops.OP_TERMINATE_REWIND_STR);
				}

				case Opcodes.Ops.OP_PUSH:
				{
					return new PushInsn(op, addr);
				}

				case Opcodes.Ops.OP_PUSH_FRAME:
				{
					return new PushFrameInsn(op, addr);
				}

				case Opcodes.Ops.OP_BREAK:
				{
					return new DebugBreakInsn(op, addr);
				}

				case Opcodes.Ops.UNUSED1:
				case Opcodes.Ops.UNUSED2:
				{
					return new UnusedInsn(op, addr);
				}

				case Opcodes.Ops.OP_INVALID:
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
