using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace DSODecompiler.Opcodes
{
	public class OpcodeFactory
	{
		private readonly ImmutableArray<string> opcodeStrings;

		public OpcodeFactory(ImmutableArray<string> opcodes) => opcodeStrings = opcodes;

		public Opcode CreateOpcode(uint value)
		{
			var str = value < opcodeStrings.Length ? opcodeStrings[(int) value] : null;

			return new(str != null ? value : null, str, GetReturnValue(str), GetTypeReq(str));
		}

		protected virtual ReturnValue GetReturnValue(string opcodeString) => opcodeString switch
		{
			"OP_STR_TO_NONE" or
			"OP_FLT_TO_NONE" or
			"OP_UINT_TO_NONE" or
			"OP_RETURN" or
			"OP_JMPIF" or
			"OP_JMPIFF" or
			"OP_JMPIFNOT" or
			"OP_JMPIFFNOT" => ReturnValue.ToFalse,

			"OP_LOADVAR_STR" or
			"OP_SAVEVAR_UINT" or
			"OP_SAVEVAR_FLT" or
			"OP_SAVEVAR_STR" or
			"OP_LOADFIELD_STR" or
			"OP_SAVEFIELD_UINT" or
			"OP_SAVEFIELD_FLT" or
			"OP_SAVEFIELD_STR" or
			"OP_FLT_TO_STR" or
			"OP_UINT_TO_STR" or
			"OP_LOADIMMED_UINT" or
			"OP_LOADIMMED_FLT" or
			"OP_TAG_TO_STR" or
			"OP_LOADIMMED_STR" or
			"OP_LOADIMMED_IDENT" or
			"OP_CALLFUNC" or
			"OP_CALLFUNC_RESOLVE" or
			"OP_REWIND_STR" => ReturnValue.ToTrue,

			_ => ReturnValue.NoChange,
		};

		protected virtual TypeReq GetTypeReq(string opcodeString) => opcodeString switch
		{
			"OP_STR_TO_UINT" or "OP_FLT_TO_UINT" => TypeReq.UInt,
			"OP_STR_TO_FLT" or "OP_UINT_TO_FLT" => TypeReq.Float,
			"OP_FLT_TO_STR" or "OP_UINT_TO_STR" => TypeReq.String,
			"OP_STR_TO_NONE" or "OP_FLT_TO_NONE" or "OP_UINT_TO_NONE" => TypeReq.None,

			_ => TypeReq.Invalid,
		};
	}
}
