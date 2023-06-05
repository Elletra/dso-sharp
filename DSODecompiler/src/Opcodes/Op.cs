namespace DSODecompiler.Opcodes
{
	public enum ReturnValue : sbyte
	{
		ToFalse,
		ToTrue,
		NoChange,
	}

	public enum TypeReq : sbyte
	{
		Invalid = -1,
		None,
		UInt,
		Float,
		String,
	}

	public class Op
	{
		public Opcode? Opcode { get; }
		public ReturnValue ReturnValue { get; }
		public TypeReq TypeReq { get; }

		public bool IsValid => Opcode != null && Opcode != Opcodes.Opcode.OP_INVALID;

		public Op (Opcode? opcode, ReturnValue? returnValue, TypeReq? typeReq)
		{
			Opcode = opcode;
			ReturnValue = returnValue ?? ReturnValue.NoChange;
			TypeReq = typeReq ?? TypeReq.Invalid;
		}
	}
}
