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
		protected BytecodeReader reader = null;

		// This is used to emulate the STR object used in Torque to return values from files/functions.
		protected bool returnableValue = false;

		protected uint functionEnd = 0;

		/* There's probably a stupid way to nest function declarations inside each other, and that
		   would require having a function stack instead, but since we're keeping it simple for now,
		   let's just do it this way.

		   Tentative TODO: Maybe someday. */
		protected bool InFunction => functionEnd > 0;

		public Disassembly Disassemble (FileData data)
		{
			reader = new(data);
			disassembly = new();
			returnableValue = false;
			functionEnd = 0;

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

			switch (opcode.Op)
			{
				case Opcode.Ops.OP_FUNC_DECL:
				{
					var instruction = new FuncDeclInsn(opcode, addr)
					{
						Name = reader.ReadIdent(),
						Namespace = reader.ReadIdent(),
						Package = reader.ReadIdent(),
						HasBody = reader.ReadBool(),
						EndAddr = reader.Read(),
					};

					var args = reader.Read();

					for (uint i = 0; i < args; i++)
					{
						instruction.Arguments.Add(reader.ReadIdent());
					}

					if (instruction.HasBody)
					{
						if (InFunction)
						{
							throw new Exception($"Nested function declaration at {addr}");
						}

						functionEnd = instruction.EndAddr;
					}

					return instruction;
				}

				case Opcode.Ops.OP_CREATE_OBJECT:
				{
					var instruction = new CreateObjectInsn(opcode, addr)
					{
						ParentName = reader.ReadIdent(),
						IsDataBlock = reader.ReadBool(),
						FailJumpAddr = reader.Read(),
					};

					return instruction;
				}

				case Opcode.Ops.OP_ADD_OBJECT:
				{
					return new AddObjectInsn(opcode, addr, reader.ReadBool());
				}

				case Opcode.Ops.OP_END_OBJECT:
				{
					return new EndObjectInsn(opcode, addr, reader.ReadBool());
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

					var target = reader.Read();

					if (target >= reader.CodeSize)
					{
						throw new Exception($"Branch at {addr} jumps to invalid address {target}");
					}

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
					return new SetCurVarInsn(opcode, addr, reader.ReadIdent());
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
					return new SetCurFieldInsn(opcode, addr, reader.ReadIdent());
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
				{
					returnableValue = true;

					return new LoadImmedInsn<uint>(opcode, addr, reader.Read());
				}

				case Opcode.Ops.OP_LOADIMMED_FLT:
				{
					returnableValue = true;

					return new LoadImmedInsn<double>(opcode, addr, reader.ReadDouble(global: !InFunction));
				}

				case Opcode.Ops.OP_TAG_TO_STR:
				case Opcode.Ops.OP_LOADIMMED_STR:
				{
					returnableValue = true;

					return new LoadImmedInsn<string>(opcode, addr, reader.ReadString(global: !InFunction));
				}

				case Opcode.Ops.OP_LOADIMMED_IDENT:
				{
					returnableValue = true;

					return new LoadImmedInsn<string>(opcode, addr, reader.ReadIdent());
				}

				case Opcode.Ops.OP_CALLFUNC:
				case Opcode.Ops.OP_CALLFUNC_RESOLVE:
				{
					returnableValue = true;

					return new FuncCallInsn(opcode, addr)
					{
						Name = reader.ReadIdent(),
						Namespace = reader.ReadIdent(),
						CallType = reader.Read(),
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
						return new AdvanceStringInsn(opcode, addr, type, reader.ReadChar());
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

		protected void ProcessAddress (uint addr)
		{
			if (addr == functionEnd)
			{
				functionEnd = 0;
			}
		}

		protected void ProcessInstruction (Instruction instruction)
		{
			disassembly.Add(instruction);
		}
	}
}
