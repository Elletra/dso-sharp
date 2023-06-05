namespace DSODecompiler.Opcodes
{
	public class OpFactory
	{
		// TODO: Change this when support for other DSO versions is implemented.
		const uint DEFAULT_MAX_OPCODE = (uint) Opcode.OP_FUNC_DECL;

		protected virtual uint MaxValidOpcode => DEFAULT_MAX_OPCODE;

		public Op CreateOp (uint value)
		{
			if (value > MaxValidOpcode)
			{
				return new Op(null, null, null);
			}

			Opcode opcode = (Opcode) value;

			return new Op(opcode, GetReturnValue(opcode), GetTypeReq(opcode));
		}

		protected ReturnValue GetReturnValue (Opcode opcode)
		{
			switch (opcode)
			{
				case Opcode.OP_RETURN:
				case Opcode.OP_JMP:
				case Opcode.OP_JMPIF:
				case Opcode.OP_JMPIFF:
				case Opcode.OP_JMPIFNOT:
				case Opcode.OP_JMPIFFNOT:
				case Opcode.OP_JMPIF_NP:
				case Opcode.OP_JMPIFNOT_NP:
				case Opcode.OP_STR_TO_NONE:
				case Opcode.OP_FLT_TO_NONE:
				case Opcode.OP_UINT_TO_NONE:
					return ReturnValue.ToFalse;

				case Opcode.OP_LOADVAR_STR:
				case Opcode.OP_SAVEVAR_UINT:
				case Opcode.OP_SAVEVAR_FLT:
				case Opcode.OP_SAVEVAR_STR:
				case Opcode.OP_LOADFIELD_STR:
				case Opcode.OP_SAVEFIELD_UINT:
				case Opcode.OP_SAVEFIELD_FLT:
				case Opcode.OP_SAVEFIELD_STR:
				case Opcode.OP_FLT_TO_STR:
				case Opcode.OP_UINT_TO_STR:
				case Opcode.OP_LOADIMMED_UINT:
				case Opcode.OP_LOADIMMED_FLT:
				case Opcode.OP_TAG_TO_STR:
				case Opcode.OP_LOADIMMED_STR:
				case Opcode.OP_LOADIMMED_IDENT:
				case Opcode.OP_CALLFUNC:
				case Opcode.OP_CALLFUNC_RESOLVE:
				case Opcode.OP_REWIND_STR:
					return ReturnValue.ToTrue;

				default:
					return ReturnValue.NoChange;
			}
		}

		protected TypeReq GetTypeReq (Opcode opcode)
		{
			switch (opcode)
			{
				case Opcode.OP_STR_TO_UINT:
				case Opcode.OP_FLT_TO_UINT:
					return TypeReq.UInt;

				case Opcode.OP_STR_TO_FLT:
				case Opcode.OP_UINT_TO_FLT:
					return TypeReq.Float;

				case Opcode.OP_FLT_TO_STR:
				case Opcode.OP_UINT_TO_STR:
					return TypeReq.String;

				case Opcode.OP_STR_TO_NONE:
				case Opcode.OP_FLT_TO_NONE:
				case Opcode.OP_UINT_TO_NONE:
					return TypeReq.None;

				default:
					return TypeReq.Invalid;
			}
		}
	}
}
