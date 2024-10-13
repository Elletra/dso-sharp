using DSO.Decompiler.Loader;
using DSO.Opcodes;
using System.Reflection.PortableExecutable;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DSO.Disassembler
{
	public class DisassemblerException : Exception
	{
		public DisassemblerException() { }
		public DisassemblerException(string message) : base(message) { }
		public DisassemblerException(string message, Exception inner) : base(message, inner) { }
	}

	public class Disassembler
	{
		private uint _index = 0;
		private FileData _data = null;

		private bool IsAtEnd => _index >= _data.Code.Length;

		/**
		 * There's probably some stupid way to nest function declarations inside each other, but that
		 * would be much more complicated, so let's just keep it simple for now.
		 *
		 * TODO: Maybe someday.
		 */
		private FunctionInstruction? _function = null;
		private bool InFunction => _function != null;

		/// <summary>
		/// For emulating the STR object used in Torque to return values from functions.
		/// </summary>
		private bool _returnableValue = false;

		public Disassembly Disassemble(FileData data)
		{
			_index = 0;
			_data = data;

			return Disassemble();
		}

		private Disassembly Disassemble()
		{
			var disassembly = new Disassembly();

			while (!IsAtEnd)
			{
				ProcessAddress(_index);

				var instruction = Disassemble(_index, Read());

				ProcessInstruction(instruction);

				disassembly.AddInstruction(instruction);
			}

			return disassembly;
		}

		private void ProcessAddress(uint address)
		{
			if (InFunction && address >= _function.EndAddress)
			{
				_function = null;
			}
		}

		private Instruction Disassemble(uint address, uint op)
		{
			var opcode = Opcode.Create(op);

			return opcode?.Value switch
			{
				Ops.OP_FUNC_DECL => DisassembleFunction(address, opcode),

				Ops.OP_CREATE_OBJECT => new CreateObjectInstruction(
					opcode,
					address,
					parent: ReadIdentifier(),
					isDataBlock: ReadBool(),
					failJumpAddress: Read()
				),

				Ops.OP_ADD_OBJECT => new AddObjectInstruction(opcode, address, placeAtRoot: ReadBool()),
				Ops.OP_END_OBJECT => new EndObjectInstruction(opcode, address, value: ReadBool()),

				Ops.OP_JMP or
				Ops.OP_JMPIF_NP or Ops.OP_JMPIFNOT_NP or
				Ops.OP_JMPIF or Ops.OP_JMPIFF or
				Ops.OP_JMPIFNOT or Ops.OP_JMPIFFNOT => new BranchInstruction(opcode, address, targetAddress: Read()),

				Ops.OP_RETURN => new ReturnInstruction(opcode, address, _returnableValue),

				Ops.OP_CMPEQ or Ops.OP_CMPNE or
				Ops.OP_CMPGR or Ops.OP_CMPGE or Ops.OP_CMPLT or Ops.OP_CMPLE or
				Ops.OP_XOR or Ops.OP_BITAND or Ops.OP_BITOR or Ops.OP_SHR or Ops.OP_SHL or
				Ops.OP_AND or Ops.OP_OR or
				Ops.OP_ADD or Ops.OP_SUB or Ops.OP_MUL or Ops.OP_DIV or Ops.OP_MOD => new BinaryInstruction(opcode, address),

				Ops.OP_COMPARE_STR => new BinaryStringInstruction(opcode, address),

				Ops.OP_NOT or Ops.OP_NOTF or Ops.OP_ONESCOMPLEMENT or Ops.OP_NEG => new UnaryInstruction(opcode, address),

				Ops.OP_SETCURVAR or Ops.OP_SETCURVAR_CREATE => new VariableInstruction(opcode, address, name: ReadIdentifier()),
				Ops.OP_SETCURVAR_ARRAY or Ops.OP_SETCURVAR_ARRAY_CREATE => new VariableArrayInstruction(opcode, address),

				Ops.OP_LOADVAR_UINT or Ops.OP_LOADVAR_FLT or Ops.OP_LOADVAR_STR => new LoadVariableInstruction(opcode, address),
				Ops.OP_SAVEVAR_UINT or Ops.OP_SAVEVAR_FLT or Ops.OP_SAVEVAR_STR => new SaveVariableInstruction(opcode, address),

				Ops.OP_SETCUROBJECT => new ObjectInstruction(opcode, address),
				Ops.OP_SETCUROBJECT_NEW => new ObjectNewInstruction(opcode, address),
				Ops.OP_SETCURFIELD => new FieldInstruction(opcode, address, name: ReadIdentifier()),
				Ops.OP_SETCURFIELD_ARRAY => new FieldArrayInstruction(opcode, address),

				Ops.OP_LOADFIELD_UINT or Ops.OP_LOADFIELD_FLT or Ops.OP_LOADFIELD_STR => new LoadFieldInstruction(opcode, address),
				Ops.OP_SAVEFIELD_UINT or Ops.OP_SAVEFIELD_FLT or Ops.OP_SAVEFIELD_STR => new SaveFieldInstruction(opcode, address),

				Ops.OP_STR_TO_UINT or Ops.OP_STR_TO_FLT or Ops.OP_STR_TO_NONE or
				Ops.OP_FLT_TO_UINT or Ops.OP_FLT_TO_STR or Ops.OP_FLT_TO_NONE or
				Ops.OP_UINT_TO_FLT or Ops.OP_UINT_TO_STR or Ops.OP_UINT_TO_NONE => new ConvertToTypeInstruction(opcode, address),

				Ops.OP_LOADIMMED_UINT => new ImmediateInstruction<uint>(opcode, address, value: Read()),
				Ops.OP_LOADIMMED_FLT => new ImmediateInstruction<double>(opcode, address, value: ReadDouble()),

				Ops.OP_TAG_TO_STR or Ops.OP_LOADIMMED_STR => new ImmediateInstruction<string>(opcode, address, value: ReadString()),
				Ops.OP_LOADIMMED_IDENT => new ImmediateInstruction<string>(opcode, address, value: ReadIdentifier()),

				Ops.OP_CALLFUNC or Ops.OP_CALLFUNC_RESOLVE => new CallInstruction(
					opcode,
					address,
					name: ReadIdentifier(),
					ns: ReadIdentifier(),
					callType: Read()
				),

				Ops.OP_ADVANCE_STR => new AdvanceStringInstruction(opcode, address),
				Ops.OP_ADVANCE_STR_APPENDCHAR => new AdvanceAppendInstruction(opcode, address, ReadChar()),
				Ops.OP_ADVANCE_STR_COMMA => new AdvanceCommaInstruction(opcode, address),
				Ops.OP_ADVANCE_STR_NUL => new AdvanceNullInstruction(opcode, address),
				Ops.OP_REWIND_STR => new RewindStringInstruction(opcode, address),
				Ops.OP_TERMINATE_REWIND_STR => new TerminateRewindInstruction(opcode, address),

				Ops.OP_PUSH => new PushInstruction(opcode, address),
				Ops.OP_PUSH_FRAME => new PushFrameInstruction(opcode, address),

				Ops.OP_BREAK => new DebugBreakInstruction(opcode, address),
				Ops.OP_UNUSED1 or Ops.OP_UNUSED2 or Ops.OP_UNUSED3 => new UnusedInstruction(opcode, address),

				_ => throw new DisassemblerException($"Invalid opcode {op} at {address}"),
			}; ;
		}

		private FunctionInstruction DisassembleFunction(uint address, Opcode opcode)
		{
			var instruction = new FunctionInstruction(
				opcode,
				address,
				name: ReadIdentifier(),
				ns: ReadIdentifier(),
				package: ReadIdentifier(),
				hasBody : ReadBool(),
				endAddr: Read()
			);

			var args = Read();

			for (uint i = 0; i < args; i++)
			{
				instruction.Arguments.Add(ReadIdentifier());
			}

			return instruction;
        }

		private void ProcessInstruction(Instruction instruction)
		{
			ValidateInstruction(instruction);

			if (instruction.Opcode.ReturnValue != ReturnValue.NoChange)
			{
				_returnableValue = instruction.Opcode.ReturnValue == ReturnValue.ToTrue;
			}

			if (instruction is FunctionInstruction function)
			{
				_function = function;
			}
		}

		private void ValidateInstruction(Instruction instruction)
		{
			switch (instruction)
			{
				case FunctionInstruction func:
					if (func.HasBody)
					{
						// TODO: Maybe support nested functions someday??
						if (InFunction)
						{
							throw new DisassemblerException($"Nested function at {func.Address}");
						}

						if (func.EndAddress >= _data.Code.Length)
						{
							throw new DisassemblerException($"Function at {func.Address} has invalid end address {func.EndAddress}");
						}

						_function = func;
					}

					break;

				case BranchInstruction branch:
					if (InFunction)
					{
						var addr = branch.Address;
						var target = branch.TargetAddress;

						if (target <= _function.Address || target >= _function.EndAddress)
						{
							throw new DisassemblerException($"Branch at {addr} jumps out of function to {target}");
						}
					}

					break;

				default:
					break;
			}
		}

		private uint Read() => _data.Code[_index++];
		private bool ReadBool() => Read() != 0;
		private char ReadChar() => (char) Read();

		private string? ReadIdentifier()
		{
			var identifierIndex = _index;
			var stringIndex = Read();

			return _data.IdentifierTable.ContainsKey(identifierIndex) ? _data.GlobalStringTable[stringIndex] : null;
		}

		private string ReadString() => (InFunction ? _data.FunctionStringTable : _data.GlobalStringTable).Get(Read());
		private double ReadDouble() => (InFunction ? _data.FunctionFloatTable : _data.GlobalFloatTable)[Read()];
	}
}
