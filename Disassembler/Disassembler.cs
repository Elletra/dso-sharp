/**
 * Disassembler.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Loader;
using DSO.Opcodes;

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
		private Ops _ops = null;

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

		public Disassembly Disassemble(FileData data, Ops ops)
		{
			_index = 0;
			_data = data;
			_ops = ops;
			_function = null;
			_returnableValue = false;

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
			var opcode = Opcode.Create(op, _ops);

			return opcode?.Tag switch
			{
				OpcodeTag.OP_FUNC_DECL => DisassembleFunction(address, opcode),

				OpcodeTag.OP_CREATE_OBJECT => new CreateObjectInstruction(
					opcode,
					address,
					parent: ReadIdentifier(),
					isDataBlock: ReadBool(),
					failJumpAddress: Read()
				),

				OpcodeTag.OP_ADD_OBJECT => new AddObjectInstruction(opcode, address, placeAtRoot: ReadBool()),
				OpcodeTag.OP_END_OBJECT => new EndObjectInstruction(opcode, address, value: ReadBool()),

				OpcodeTag.OP_JMP or
				OpcodeTag.OP_JMPIF_NP or OpcodeTag.OP_JMPIFNOT_NP or
				OpcodeTag.OP_JMPIF or OpcodeTag.OP_JMPIFF or
				OpcodeTag.OP_JMPIFNOT or OpcodeTag.OP_JMPIFFNOT => new BranchInstruction(opcode, address, targetAddress: Read()),

				OpcodeTag.OP_RETURN => new ReturnInstruction(opcode, address, _returnableValue),

				OpcodeTag.OP_CMPEQ or OpcodeTag.OP_CMPNE or
				OpcodeTag.OP_CMPGR or OpcodeTag.OP_CMPGE or OpcodeTag.OP_CMPLT or OpcodeTag.OP_CMPLE or
				OpcodeTag.OP_XOR or OpcodeTag.OP_BITAND or OpcodeTag.OP_BITOR or OpcodeTag.OP_SHR or OpcodeTag.OP_SHL or
				OpcodeTag.OP_AND or OpcodeTag.OP_OR or
				OpcodeTag.OP_ADD or OpcodeTag.OP_SUB or OpcodeTag.OP_MUL or OpcodeTag.OP_DIV or OpcodeTag.OP_MOD => new BinaryInstruction(opcode, address),

				OpcodeTag.OP_COMPARE_STR => new BinaryStringInstruction(opcode, address),

				OpcodeTag.OP_NOT or OpcodeTag.OP_NOTF or OpcodeTag.OP_ONESCOMPLEMENT or OpcodeTag.OP_NEG => new UnaryInstruction(opcode, address),

				OpcodeTag.OP_SETCURVAR or OpcodeTag.OP_SETCURVAR_CREATE => new VariableInstruction(opcode, address, name: ReadIdentifier()),
				OpcodeTag.OP_SETCURVAR_ARRAY or OpcodeTag.OP_SETCURVAR_ARRAY_CREATE => new VariableArrayInstruction(opcode, address),

				OpcodeTag.OP_LOADVAR_UINT or OpcodeTag.OP_LOADVAR_FLT or OpcodeTag.OP_LOADVAR_STR => new LoadVariableInstruction(opcode, address),
				OpcodeTag.OP_SAVEVAR_UINT or OpcodeTag.OP_SAVEVAR_FLT or OpcodeTag.OP_SAVEVAR_STR => new SaveVariableInstruction(opcode, address),

				OpcodeTag.OP_SETCUROBJECT => new ObjectInstruction(opcode, address),
				OpcodeTag.OP_SETCUROBJECT_NEW => new ObjectNewInstruction(opcode, address),
				OpcodeTag.OP_SETCURFIELD => new FieldInstruction(opcode, address, name: ReadIdentifier()),
				OpcodeTag.OP_SETCURFIELD_ARRAY => new FieldArrayInstruction(opcode, address),

				OpcodeTag.OP_LOADFIELD_UINT or OpcodeTag.OP_LOADFIELD_FLT or OpcodeTag.OP_LOADFIELD_STR => new LoadFieldInstruction(opcode, address),
				OpcodeTag.OP_SAVEFIELD_UINT or OpcodeTag.OP_SAVEFIELD_FLT or OpcodeTag.OP_SAVEFIELD_STR => new SaveFieldInstruction(opcode, address),

				OpcodeTag.OP_STR_TO_UINT or OpcodeTag.OP_STR_TO_FLT or OpcodeTag.OP_STR_TO_NONE or
				OpcodeTag.OP_FLT_TO_UINT or OpcodeTag.OP_FLT_TO_STR or OpcodeTag.OP_FLT_TO_NONE or
				OpcodeTag.OP_UINT_TO_FLT or OpcodeTag.OP_UINT_TO_STR or OpcodeTag.OP_UINT_TO_NONE => new ConvertToTypeInstruction(opcode, address),

				OpcodeTag.OP_LOADIMMED_UINT => new ImmediateInstruction<uint>(opcode, address, value: Read()),
				OpcodeTag.OP_LOADIMMED_FLT => new ImmediateInstruction<double>(opcode, address, value: ReadDouble()),

				OpcodeTag.OP_TAG_TO_STR or OpcodeTag.OP_LOADIMMED_STR => new ImmediateInstruction<StringTableEntry>(opcode, address, value: ReadString()),
				OpcodeTag.OP_LOADIMMED_IDENT => new ImmediateInstruction<StringTableEntry>(opcode, address, value: ReadIdentifier()),

				OpcodeTag.OP_CALLFUNC or OpcodeTag.OP_CALLFUNC_RESOLVE => new CallInstruction(
					opcode,
					address,
					name: ReadIdentifier(),
					ns: ReadIdentifier(),
					callType: Read()
				),

				OpcodeTag.OP_ADVANCE_STR => new AdvanceStringInstruction(opcode, address),
				OpcodeTag.OP_ADVANCE_STR_APPENDCHAR => new AdvanceAppendInstruction(opcode, address, ReadChar()),
				OpcodeTag.OP_ADVANCE_STR_COMMA => new AdvanceCommaInstruction(opcode, address),
				OpcodeTag.OP_ADVANCE_STR_NUL => new AdvanceNullInstruction(opcode, address),
				OpcodeTag.OP_REWIND_STR => new RewindStringInstruction(opcode, address),
				OpcodeTag.OP_TERMINATE_REWIND_STR => new TerminateRewindInstruction(opcode, address),

				OpcodeTag.OP_PUSH => new PushInstruction(opcode, address),
				OpcodeTag.OP_PUSH_FRAME => new PushFrameInstruction(opcode, address),

				OpcodeTag.OP_BREAK => new DebugBreakInstruction(opcode, address),
				OpcodeTag.OP_UNUSED1 or OpcodeTag.OP_UNUSED2 or OpcodeTag.OP_UNUSED3 => new UnusedInstruction(opcode, address),

				_ => throw new DisassemblerException($"Invalid opcode 0x{op:X2} at {address}"),
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
				hasBody: ReadBool(),
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

		private StringTableEntry? ReadIdentifier()
		{
			var identifierIndex = _index;
			var stringIndex = Read();

			return _data.IdentifierTable.ContainsKey(identifierIndex) ? _data.GlobalStringTable[stringIndex] : null;
		}

		private StringTableEntry ReadString() => (InFunction ? _data.FunctionStringTable : _data.GlobalStringTable).Get(Read());
		private double ReadDouble() => (InFunction ? _data.FunctionFloatTable : _data.GlobalFloatTable)[Read()];
	}
}
