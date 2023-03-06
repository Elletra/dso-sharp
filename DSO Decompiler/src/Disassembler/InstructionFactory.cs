using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembler
{
	public static class InstructionFactory
	{
		public static Instruction Create (Opcode opcode, uint addr, BytecodeReader reader, bool returnableValue)
		{
			switch (opcode.Type)
			{
				case OpcodeType.FunctionDecl:
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

				case OpcodeType.CreateObject:
				{
					return new CreateObjectInsn(opcode, addr)
					{
						ParentName = reader.ReadIdent(),
						IsDataBlock = reader.ReadBool(),
						FailJumpAddr = reader.Read(),
					};
				}

				case OpcodeType.AddObject:
				{
					return new AddObjectInsn(opcode, addr, reader.ReadBool());
				}

				case OpcodeType.EndObject:
				{
					return new EndObjectInsn(opcode, addr, reader.ReadBool());
				}

				case OpcodeType.Branch:
				{
					return new BranchInsn(opcode, addr, reader.Read());
				}

				case OpcodeType.Return:
				{
					return new ReturnInsn(opcode, addr, returnableValue);
				}

				case OpcodeType.Binary:
				{
					return new BinaryInsn(opcode, addr);
				}

				case OpcodeType.BinaryString:
				{
					return new StringCompareInsn(opcode, addr);
				}

				case OpcodeType.Unary:
				{
					return new UnaryInsn(opcode, addr);
				}

				case OpcodeType.SetCurVar:
				{
					return new SetCurVarInsn(opcode, addr, reader.ReadIdent());
				}

				case OpcodeType.SetCurVarArray:
				{
					return new SetCurVarArrayInsn(opcode, addr);
				}

				case OpcodeType.LoadVar:
				{
					return new LoadVarInsn(opcode, addr);
				}

				case OpcodeType.SaveVar:
				{
					return new SaveVarInsn(opcode, addr);
				}

				case OpcodeType.SetCurObject:
				case OpcodeType.SetCurObjectNew:
				{
					return new SetCurObjectInsn(opcode, addr);
				}

				case OpcodeType.SetCurField:
				{
					return new SetCurFieldInsn(opcode, addr, reader.ReadIdent());
				}

				case OpcodeType.SetCurFieldArray:
				{
					return new SetCurFieldArrayInsn(opcode, addr);
				}

				case OpcodeType.LoadField:
				{
					return new LoadFieldInsn(opcode, addr);
				}

				case OpcodeType.SaveField:
				{
					return new SaveFieldInsn(opcode, addr);
				}

				case OpcodeType.ConvertToUInt:
				case OpcodeType.ConvertToFloat:
				case OpcodeType.ConvertToString:
				case OpcodeType.ConvertToNone:
				{
					return new ConvertToTypeInsn(opcode, addr);
				}

				case OpcodeType.LoadImmedUInt:
				{
					return new LoadImmedInsn<uint>(opcode, addr, reader.Read());
				}

				case OpcodeType.LoadImmedFloat:
				{
					return new LoadImmedInsn<double>(opcode, addr, reader.ReadDouble());
				}

				case OpcodeType.LoadImmedString:
				{
					return new LoadImmedInsn<string>(opcode, addr, reader.ReadString());
				}

				case OpcodeType.LoadImmedIdent:
				{
					return new LoadImmedInsn<string>(opcode, addr, reader.ReadIdent());
				}

				case OpcodeType.FunctionCall:
				{
					return new FuncCallInsn(opcode, addr)
					{
						Name = reader.ReadIdent(),
						Namespace = reader.ReadIdent(),
						CallType = reader.Read(),
					};
				}

				case OpcodeType.AdvanceString:
				{
					return new AdvanceStringInsn(opcode, addr);
				}

				case OpcodeType.AdvanceAppendString:
				{
					return new AdvanceStringInsn(opcode, addr, reader.ReadChar());
				}

				case OpcodeType.RewindString:
				case OpcodeType.TerminateRewindString:
				{
					return new RewindInsn(opcode, addr);
				}

				case OpcodeType.Push:
				{
					return new PushInsn(opcode, addr);
				}

				case OpcodeType.PushFrame:
				{
					return new PushFrameInsn(opcode, addr);
				}

				case OpcodeType.DebugBreak:
				{
					return new DebugBreakInsn(opcode, addr);
				}

				case OpcodeType.Unused:
				{
					return new UnusedInsn(opcode, addr);
				}

				case OpcodeType.Invalid:
				default:
				{
					return null;
				}
			}
		}
	}
}
