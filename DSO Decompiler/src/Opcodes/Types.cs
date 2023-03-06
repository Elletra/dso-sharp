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
}
