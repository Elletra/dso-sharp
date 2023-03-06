namespace DSODecompiler.Opcodes
{
	public enum BranchType
	{
		Invalid = -1,
		Unconditional, // OP_JMP
		Conditional,   // OP_JMPIF(F), OP_JMPIF(F)NOT
		LogicalBranch, // OP_JMPIF_NP, OPJMPIFNOT_NP (for logical and/or)
	}

	public enum ConvertToType
	{
		Invalid = -1,
		UInt,
		Float,
		String,
		None,
	}

	public enum AdvanceStringType
	{
		Invalid = -1,
		Default,
		Append,
		Comma,
		Null,
	}

	// Whether an instruction modifies the return value of a function or file.
	public enum ReturnValue
	{
		NoChange = -1,
		ToFalse,
		ToTrue,
	}

	public enum OpcodeType
	{
		Invalid = -1,
		FunctionDecl,
		CreateObject,
		AddObject,
		EndObject,
		Branch,
		Return,
		Binary,
		BinaryString,
		Unary,
		SetCurVar,
		SetCurVarArray,
		LoadVar,
		SaveVar,
		SetCurObject,
		SetCurObjectNew,
		SetCurField,
		SetCurFieldArray,
		LoadField,
		SaveField,
		ConvertToUInt,
		ConvertToFloat,
		ConvertToString,
		ConvertToNone,
		LoadImmedUInt,
		LoadImmedFloat,
		LoadImmedString,
		LoadImmedIdent,
		FunctionCall,
		AdvanceString,
		AdvanceAppendString,
		RewindString,
		TerminateRewindString,
		Push,
		PushFrame,
		DebugBreak,
		Unused,
	}
}
