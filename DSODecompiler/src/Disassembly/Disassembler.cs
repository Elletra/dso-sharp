using DSODecompiler.Loader;
using DSODecompiler.Opcodes;

namespace DSODecompiler.Disassembly
{
	/// <summary>
	/// Transforms raw bytecode into instructions.
	/// </summary>
	public class Disassembler
	{
		public class Exception : System.Exception
		{
			public Exception () {}
			public Exception (string message) : base(message) {}
			public Exception (string message, Exception inner) : base(message, inner) {}
		}

		protected OpcodeFactory factory;
		protected BytecodeReader reader;
		protected Disassembly disassembly;

		/// <summary>
		/// For emulating the STR object used in Torque to return values from functions.
		/// </summary>
		protected bool returnableValue = false;

		public Disassembler (OpcodeFactory opcodeFactory)
		{
			factory = opcodeFactory;
		}

		public Disassembly Disassemble (FileData fileData)
		{
			reader = new(fileData);
			disassembly = new();

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

		protected void DisassembleNext ()
		{
			var addr = reader.Index;

			ProcessAddress(addr);

			var value = reader.Read();
			var opcode = factory.CreateOpcode(value);

			if (!opcode.IsValid)
			{
				throw new Exception($"Invalid opcode {value} at {addr}");
			}

			var instruction = DisassembleOpcode(opcode, addr);

			if (instruction == null)
			{
				throw new Exception($"Failed to disassemble opcode {opcode.Value} at {addr}");
			}

			ProcessInstruction(instruction);
		}

		protected void ProcessAddress (uint addr)
		{
			if (reader.InFunction && addr >= reader.Function.EndAddr)
			{
				reader.Function = null;
			}
		}

		/// <summary>
		/// Disassembles the next opcode.<br/><br/>
		///
		/// Right now we're just doing a linear sweep, but that doesn't handle certain anti-disassembly
		/// techniques like jumping to the middle of an instruction.<br/><br/>
		///
		/// No DSO files currently do this to my knowledge, and probably never will, but it would be
		/// nice to support it eventually. At the moment, I just want to write a functional decompiler
		/// first and then worry about edge cases later.<br/><br/>
		///
		/// TODO: Maybe someday.
		/// </summary>
		/// <param name="op"></param>
		/// <param name="addr"></param>
		/// <returns></returns>
		protected Instruction DisassembleOpcode (Opcode opcode, uint addr)
		{
			switch (opcode.StringValue)
			{
				case "OP_FUNC_DECL":
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

				case "OP_CREATE_OBJECT":
				{
					return new CreateObjectInstruction(
						opcode,
						addr,
						parent: reader.ReadIdent(),
						isDataBlock: reader.ReadBool(),
						failJumpAddr: reader.Read()
					);
				}

				case "OP_ADD_OBJECT":
					return new AddObjectInstruction(opcode, addr, placeAtRoot: reader.ReadBool());

				case "OP_END_OBJECT":
					return new EndObjectInstruction(opcode, addr, value: reader.ReadBool());

				case "OP_JMP":
				case "OP_JMPIF":
				case "OP_JMPIFF":
				case "OP_JMPIFNOT":
				case "OP_JMPIFFNOT":
				case "OP_JMPIF_NP":
				case "OP_JMPIFNOT_NP":
					return new BranchInstruction(opcode, addr, targetAddr: reader.Read());

				case "OP_RETURN":
					return new ReturnInstruction(opcode, addr, returnableValue);

				case "OP_SETCURVAR":
				case "OP_SETCURVAR_CREATE":
					return new VariableInstruction(opcode, addr, name: reader.ReadIdent());

				case "OP_SETCURFIELD":
					return new FieldInstruction(opcode, addr, name: reader.ReadIdent());

				case "OP_LOADIMMED_UINT":
					return new ImmediateInstruction<uint>(opcode, addr, value: reader.Read());

				case "OP_LOADIMMED_FLT":
					return new ImmediateInstruction<double>(opcode, addr, value: reader.ReadDouble());

				case "OP_TAG_TO_STR":
				case "OP_LOADIMMED_STR":
					return new ImmediateInstruction<string>(opcode, addr, value: reader.ReadString());

				case "OP_LOADIMMED_IDENT":
					return new ImmediateInstruction<string>(opcode, addr, value: reader.ReadIdent());

				case "OP_CALLFUNC":
				case "OP_CALLFUNC_RESOLVE":
				{
					return new CallInstruction(
						opcode,
						addr,
						name: reader.ReadIdent(),
						ns: reader.ReadIdent(),
						callType: reader.Read()
					);
				}

				case "OP_ADVANCE_STR_APPENDCHAR":
					return new AppendStringInstruction(opcode, addr, reader.ReadChar());

				case "OP_INVALID":
					return null;

				default:
					return new Instruction(opcode, addr);
			}
		}

		protected void ProcessInstruction (Instruction instruction)
		{
			ValidateInstruction(instruction);
			SetReturnableValue(instruction);
			AddInstruction(instruction);
		}

		protected void ValidateInstruction (Instruction instruction)
		{
			switch (instruction)
			{
				case FunctionInstruction func:
				{
					if (func.HasBody)
					{
						// TODO: Maybe support nested functions someday??
						if (reader.InFunction)
						{
							throw new Exception($"Nested function at {func.Addr}");
						}

						if (func.EndAddr >= reader.Size)
						{
							throw new Exception($"Function at {func.Addr} has invalid end address {func.EndAddr}");
						}

						reader.Function = func;
					}

					break;
				}

				case BranchInstruction branch:
				{
					if (reader.InFunction)
					{
						var addr = branch.Addr;
						var target = branch.TargetAddr;

						if (target <= reader.Function.Addr || target >= reader.Function.EndAddr)
						{
							throw new Exception($"Branch at {addr} jumps out of function to {target}");
						}
					}

					break;
				}

				default:
					break;
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
					break;
			}
		}

		protected void AddInstruction (Instruction instruction)
		{
			disassembly.AddInstruction(instruction);
		}

		protected void MarkBranchTargets ()
		{
			foreach (var instruction in disassembly.GetInstructions())
			{
				if (instruction is BranchInstruction branch)
				{
					disassembly.AddBranchTarget(branch.TargetAddr);
				}
			}
		}
	}
}
