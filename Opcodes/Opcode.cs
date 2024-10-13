namespace DSO.Opcodes
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

	public class Opcode(Ops value, ReturnValue returnValue, TypeReq typeReq)
	{
		static public Opcode? Create(uint value)
		{
			if (!Enum.IsDefined(typeof(Ops), value))
			{
				return null;
			}

			var op = (Ops) value;

			var returnValue = op switch
			{
				Ops.OP_STR_TO_NONE or Ops.OP_FLT_TO_NONE or Ops.OP_UINT_TO_NONE or
				Ops.OP_JMPIF or Ops.OP_JMPIFF or
				Ops.OP_JMPIFNOT or Ops.OP_JMPIFFNOT or
				Ops.OP_RETURN => ReturnValue.ToFalse,

				Ops.OP_SAVEVAR_UINT or Ops.OP_SAVEVAR_FLT or Ops.OP_SAVEVAR_STR or
				Ops.OP_SAVEFIELD_UINT or Ops.OP_SAVEFIELD_FLT or Ops.OP_SAVEFIELD_STR or
				Ops.OP_LOADVAR_STR or Ops.OP_LOADFIELD_STR or
				Ops.OP_FLT_TO_STR or Ops.OP_UINT_TO_STR or
				Ops.OP_LOADIMMED_UINT or Ops.OP_LOADIMMED_FLT or
				Ops.OP_TAG_TO_STR or Ops.OP_LOADIMMED_STR or Ops.OP_LOADIMMED_IDENT or
				Ops.OP_CALLFUNC or Ops.OP_CALLFUNC_RESOLVE or
				Ops.OP_REWIND_STR => ReturnValue.ToTrue,

				_ => ReturnValue.NoChange,
			};

			var typeReq = op switch
			{
				Ops.OP_STR_TO_UINT or Ops.OP_FLT_TO_UINT => TypeReq.UInt,
				Ops.OP_STR_TO_FLT or Ops.OP_UINT_TO_FLT => TypeReq.Float,
				Ops.OP_FLT_TO_STR or Ops.OP_UINT_TO_STR => TypeReq.String,
				Ops.OP_STR_TO_NONE or Ops.OP_FLT_TO_NONE or Ops.OP_UINT_TO_NONE => TypeReq.None,

				_ => TypeReq.Invalid,
			};

			return new(op, returnValue, typeReq);
		}

		public Ops Value { get; } = value;
		public ReturnValue ReturnValue { get; } = returnValue;
		public TypeReq TypeReq { get; } = typeReq;

		public override bool Equals(object? obj) => obj is Opcode opcode
			&& Equals(opcode.Value, Value)
			&& Equals(opcode.ReturnValue, ReturnValue)
			&& Equals(opcode.TypeReq, TypeReq);

		public override int GetHashCode() => Value.GetHashCode()
			^ ReturnValue.GetHashCode()
			^ TypeReq.GetHashCode();
	}
}
