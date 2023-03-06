using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembler
{
	public static class InstructionFactory
	{
		public static Instruction Create (Opcode opcode, uint addr, BytecodeReader reader, bool returnableValue)
		{
			switch (opcode.Op)
			{
				case Ops.Value.OP_FUNC_DECL:
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

					return instruction;
				}

				case Ops.Value.OP_CREATE_OBJECT:
				{
					return new CreateObjectInsn(opcode, addr)
					{
						ParentName = reader.ReadIdent(),
						IsDataBlock = reader.ReadBool(),
						FailJumpAddr = reader.Read(),
					};
				}

				case Ops.Value.OP_ADD_OBJECT:
				{
					return new AddObjectInsn(opcode, addr, reader.ReadBool());
				}

				case Ops.Value.OP_END_OBJECT:
				{
					return new EndObjectInsn(opcode, addr, reader.ReadBool());
				}

				case Ops.Value.OP_JMP:
				case Ops.Value.OP_JMPIF:
				case Ops.Value.OP_JMPIFF:
				case Ops.Value.OP_JMPIFNOT:
				case Ops.Value.OP_JMPIFFNOT:
				case Ops.Value.OP_JMPIF_NP:
				case Ops.Value.OP_JMPIFNOT_NP:
				{
					return new BranchInsn(opcode, addr, reader.Read());
				}

				case Ops.Value.OP_RETURN:
				{
					return new ReturnInsn(opcode, addr, returnableValue);
				}

				case Ops.Value.OP_CMPEQ:
				case Ops.Value.OP_CMPGR:
				case Ops.Value.OP_CMPGE:
				case Ops.Value.OP_CMPLT:
				case Ops.Value.OP_CMPLE:
				case Ops.Value.OP_CMPNE:
				case Ops.Value.OP_XOR:
				case Ops.Value.OP_MOD:
				case Ops.Value.OP_BITAND:
				case Ops.Value.OP_BITOR:
				case Ops.Value.OP_SHR:
				case Ops.Value.OP_SHL:
				case Ops.Value.OP_AND:
				case Ops.Value.OP_OR:
				case Ops.Value.OP_ADD:
				case Ops.Value.OP_SUB:
				case Ops.Value.OP_MUL:
				case Ops.Value.OP_DIV:
				{
					return new BinaryInsn(opcode, addr);
				}

				case Ops.Value.OP_COMPARE_STR:
				{
					return new StringCompareInsn(opcode, addr);
				}

				case Ops.Value.OP_NEG:
				case Ops.Value.OP_NOT:
				case Ops.Value.OP_NOTF:
				case Ops.Value.OP_ONESCOMPLEMENT:
				{
					return new UnaryInsn(opcode, addr);
				}

				case Ops.Value.OP_SETCURVAR:
				case Ops.Value.OP_SETCURVAR_CREATE:
				{
					return new SetCurVarInsn(opcode, addr, reader.ReadIdent());
				}

				case Ops.Value.OP_SETCURVAR_ARRAY:
				case Ops.Value.OP_SETCURVAR_ARRAY_CREATE:
				{
					return new SetCurVarArrayInsn(opcode, addr);
				}

				case Ops.Value.OP_LOADVAR_UINT:
				case Ops.Value.OP_LOADVAR_FLT:
				case Ops.Value.OP_LOADVAR_STR:
				{
					return new LoadVarInsn(opcode, addr);
				}

				case Ops.Value.OP_SAVEVAR_UINT:
				case Ops.Value.OP_SAVEVAR_FLT:
				case Ops.Value.OP_SAVEVAR_STR:
				{
					return new SaveVarInsn(opcode, addr);
				}

				case Ops.Value.OP_SETCUROBJECT:
				case Ops.Value.OP_SETCUROBJECT_NEW:
				{
					return new SetCurObjectInsn(opcode, addr);
				}

				case Ops.Value.OP_SETCURFIELD:
				{
					return new SetCurFieldInsn(opcode, addr, reader.ReadIdent());
				}

				case Ops.Value.OP_SETCURFIELD_ARRAY:
				{
					return new SetCurFieldArrayInsn(opcode, addr);
				}

				case Ops.Value.OP_LOADFIELD_UINT:
				case Ops.Value.OP_LOADFIELD_FLT:
				case Ops.Value.OP_LOADFIELD_STR:
				{
					return new LoadFieldInsn(opcode, addr);
				}

				case Ops.Value.OP_SAVEFIELD_UINT:
				case Ops.Value.OP_SAVEFIELD_FLT:
				case Ops.Value.OP_SAVEFIELD_STR:
				{
					return new SaveFieldInsn(opcode, addr);
				}

				case Ops.Value.OP_STR_TO_UINT:
				case Ops.Value.OP_FLT_TO_UINT:
				case Ops.Value.OP_STR_TO_FLT:
				case Ops.Value.OP_UINT_TO_FLT:
				case Ops.Value.OP_FLT_TO_STR:
				case Ops.Value.OP_UINT_TO_STR:
				case Ops.Value.OP_STR_TO_NONE:
				case Ops.Value.OP_STR_TO_NONE_2:
				case Ops.Value.OP_FLT_TO_NONE:
				case Ops.Value.OP_UINT_TO_NONE:
				{
					return new ConvertToTypeInsn(opcode, addr);
				}

				case Ops.Value.OP_LOADIMMED_UINT:
				{
					return new LoadImmedInsn<uint>(opcode, addr, reader.Read());
				}

				case Ops.Value.OP_LOADIMMED_FLT:
				{
					return new LoadImmedInsn<double>(opcode, addr, reader.ReadDouble());
				}

				case Ops.Value.OP_TAG_TO_STR:
				case Ops.Value.OP_LOADIMMED_STR:
				{
					return new LoadImmedInsn<string>(opcode, addr, reader.ReadString());
				}

				case Ops.Value.OP_LOADIMMED_IDENT:
				{
					return new LoadImmedInsn<string>(opcode, addr, reader.ReadIdent());
				}

				case Ops.Value.OP_CALLFUNC:
				case Ops.Value.OP_CALLFUNC_RESOLVE:
				{
					return new FuncCallInsn(opcode, addr)
					{
						Name = reader.ReadIdent(),
						Namespace = reader.ReadIdent(),
						CallType = reader.Read(),
					};
				}

				case Ops.Value.OP_ADVANCE_STR:
				case Ops.Value.OP_ADVANCE_STR_COMMA:
				case Ops.Value.OP_ADVANCE_STR_NUL:
				{
					return new AdvanceStringInsn(opcode, addr);
				}

				case Ops.Value.OP_ADVANCE_STR_APPENDCHAR:
				{
					return new AdvanceStringInsn(opcode, addr, reader.ReadChar());
				}

				case Ops.Value.OP_REWIND_STR:
				case Ops.Value.OP_TERMINATE_REWIND_STR:
				{
					return new RewindInsn(opcode, addr);
				}

				case Ops.Value.OP_PUSH:
				{
					return new PushInsn(opcode, addr);
				}

				case Ops.Value.OP_PUSH_FRAME:
				{
					return new PushFrameInsn(opcode, addr);
				}

				case Ops.Value.OP_BREAK:
				{
					return new DebugBreakInsn(opcode, addr);
				}

				case Ops.Value.UNUSED1:
				case Ops.Value.UNUSED2:
				{
					return new UnusedInsn(opcode, addr);
				}

				case Ops.Value.OP_INVALID:
				default:
				{
					return null;
				}
			}
		}
	}
}
