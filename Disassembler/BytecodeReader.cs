/**
 * BytecodeReader.cs
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
	public class BytecodeReader
	{
		private uint _index = 0;
		private FileData _data = null;
		private Ops _ops = null;

		public uint Index => _index;
		public int CodeSize => _data.Code.Length;
		public bool IsAtEnd => _index >= _data.Code.Length;

		/// <summary>
		/// For emulating the STR object used in Torque to return values from functions.
		/// </summary>
		public bool ReturnableValue { get; set; } = false;


		/**
		 * There's probably some stupid way to nest function declarations inside each other, but that
		 * would be much more complicated, so let's just keep it simple for now.
		 *
		 * TODO: Maybe someday.
		 */
		public FunctionInstruction? Function { get; set; } = null;
		public bool InFunction => Function != null;

		public BytecodeReader()
		{
			_data = null;
			_ops = null;
			_index = 0;
			Function = null;
			ReturnableValue = false;
		}

		public BytecodeReader(FileData data, Ops ops) : this()
		{
			_data = data;
			_ops = ops;
		}

		public Instruction ReadInstruction()
		{
			var address = _index;

			return ReadInstruction(address, ReadUInt());
		}

		private Instruction ReadInstruction(uint address, uint op)
		{
			var opcode = Opcode.Create(op, _ops);

			return opcode?.Tag switch
			{
				OpcodeTag.OP_FUNC_DECL => new FunctionInstruction(opcode, address, this),

				OpcodeTag.OP_CREATE_OBJECT => new CreateObjectInstruction(opcode, address, this),

				OpcodeTag.OP_ADD_OBJECT => new AddObjectInstruction(opcode, address, this),
				OpcodeTag.OP_END_OBJECT => new EndObjectInstruction(opcode, address, this),

				OpcodeTag.OP_JMP or
				OpcodeTag.OP_JMPIF_NP or OpcodeTag.OP_JMPIFNOT_NP or
				OpcodeTag.OP_JMPIF or OpcodeTag.OP_JMPIFF or
				OpcodeTag.OP_JMPIFNOT or OpcodeTag.OP_JMPIFFNOT => new BranchInstruction(opcode, address, this),

				OpcodeTag.OP_RETURN => new ReturnInstruction(opcode, address, this),

				OpcodeTag.OP_CMPEQ or OpcodeTag.OP_CMPNE or
				OpcodeTag.OP_CMPGR or OpcodeTag.OP_CMPGE or OpcodeTag.OP_CMPLT or OpcodeTag.OP_CMPLE or
				OpcodeTag.OP_XOR or OpcodeTag.OP_BITAND or OpcodeTag.OP_BITOR or OpcodeTag.OP_SHR or OpcodeTag.OP_SHL or
				OpcodeTag.OP_AND or OpcodeTag.OP_OR or
				OpcodeTag.OP_ADD or OpcodeTag.OP_SUB or OpcodeTag.OP_MUL or OpcodeTag.OP_DIV or OpcodeTag.OP_MOD => new BinaryInstruction(opcode, address, this),

				OpcodeTag.OP_COMPARE_STR => new BinaryStringInstruction(opcode, address, this),

				OpcodeTag.OP_NOT or OpcodeTag.OP_NOTF or OpcodeTag.OP_ONESCOMPLEMENT or OpcodeTag.OP_NEG => new UnaryInstruction(opcode, address, this),

				OpcodeTag.OP_SETCURVAR or OpcodeTag.OP_SETCURVAR_CREATE => new VariableInstruction(opcode, address, this),
				OpcodeTag.OP_SETCURVAR_ARRAY or OpcodeTag.OP_SETCURVAR_ARRAY_CREATE => new VariableArrayInstruction(opcode, address, this),

				OpcodeTag.OP_LOADVAR_UINT or OpcodeTag.OP_LOADVAR_FLT or OpcodeTag.OP_LOADVAR_STR => new LoadVariableInstruction(opcode, address, this),
				OpcodeTag.OP_SAVEVAR_UINT or OpcodeTag.OP_SAVEVAR_FLT or OpcodeTag.OP_SAVEVAR_STR => new SaveVariableInstruction(opcode, address, this),

				OpcodeTag.OP_SETCUROBJECT => new ObjectInstruction(opcode, address, this),
				OpcodeTag.OP_SETCUROBJECT_NEW => new ObjectNewInstruction(opcode, address, this),
				OpcodeTag.OP_SETCURFIELD => new FieldInstruction(opcode, address, this),
				OpcodeTag.OP_SETCURFIELD_ARRAY => new FieldArrayInstruction(opcode, address, this),

				OpcodeTag.OP_LOADFIELD_UINT or OpcodeTag.OP_LOADFIELD_FLT or OpcodeTag.OP_LOADFIELD_STR => new LoadFieldInstruction(opcode, address, this),
				OpcodeTag.OP_SAVEFIELD_UINT or OpcodeTag.OP_SAVEFIELD_FLT or OpcodeTag.OP_SAVEFIELD_STR => new SaveFieldInstruction(opcode, address, this),

				OpcodeTag.OP_STR_TO_UINT or OpcodeTag.OP_STR_TO_FLT or OpcodeTag.OP_STR_TO_NONE or
				OpcodeTag.OP_FLT_TO_UINT or OpcodeTag.OP_FLT_TO_STR or OpcodeTag.OP_FLT_TO_NONE or
				OpcodeTag.OP_UINT_TO_FLT or OpcodeTag.OP_UINT_TO_STR or OpcodeTag.OP_UINT_TO_NONE => new ConvertToTypeInstruction(opcode, address, this),

				OpcodeTag.OP_LOADIMMED_UINT => new ImmediateUIntInstruction(opcode, address, this),
				OpcodeTag.OP_LOADIMMED_FLT => new ImmediateDoubleInstruction(opcode, address, this),

				OpcodeTag.OP_TAG_TO_STR or OpcodeTag.OP_LOADIMMED_STR => new ImmediateStringInstruction(opcode, address, this),
				OpcodeTag.OP_LOADIMMED_IDENT => new ImmediateStringInstruction(opcode, address, this),

				OpcodeTag.OP_CALLFUNC or OpcodeTag.OP_CALLFUNC_RESOLVE => new CallInstruction(opcode, address, this),

				OpcodeTag.OP_ADVANCE_STR => new AdvanceStringInstruction(opcode, address, this),
				OpcodeTag.OP_ADVANCE_STR_APPENDCHAR => new AdvanceAppendInstruction(opcode, address, this),
				OpcodeTag.OP_ADVANCE_STR_COMMA => new AdvanceCommaInstruction(opcode, address, this),
				OpcodeTag.OP_ADVANCE_STR_NUL => new AdvanceNullInstruction(opcode, address, this),
				OpcodeTag.OP_REWIND_STR => new RewindStringInstruction(opcode, address, this),
				OpcodeTag.OP_TERMINATE_REWIND_STR => new TerminateRewindInstruction(opcode, address, this),

				OpcodeTag.OP_PUSH => new PushInstruction(opcode, address, this),
				OpcodeTag.OP_PUSH_FRAME => new PushFrameInstruction(opcode, address, this),

				OpcodeTag.OP_BREAK => new DebugBreakInstruction(opcode, address, this),
				OpcodeTag.OP_UNUSED1 or OpcodeTag.OP_UNUSED2 or OpcodeTag.OP_UNUSED3 => new UnusedInstruction(opcode, address, this),

				_ => throw new DisassemblerException($"Invalid opcode 0x{op:X2} at {address}"),
			};
		}

		public uint ReadUInt() => _data.Code[_index++];
		public bool ReadBool() => ReadUInt() != 0;
		public char ReadChar() => (char) ReadUInt();

		public StringTableEntry? ReadIdentifier()
		{
			var identifierIndex = _index;
			var stringIndex = ReadUInt();

			return _data.IdentifierTable.ContainsKey(identifierIndex) ? _data.GlobalStringTable[stringIndex] : null;
		}

		public StringTableEntry ReadString() => (InFunction ? _data.FunctionStringTable : _data.GlobalStringTable).Get(ReadUInt());
		public double ReadDouble() => (InFunction ? _data.FunctionFloatTable : _data.GlobalFloatTable)[ReadUInt()];
	}
}
