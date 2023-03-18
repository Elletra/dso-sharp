using System;

namespace DSODecompiler.Opcodes
{
	// Whether an instruction modifies the return value of a function or file.
	public enum ReturnValue : sbyte
	{
		NoChange = -1,
		ToFalse,
		ToTrue,
	}

	public enum TypeReq : sbyte
	{
		Invalid = -1,
		None,
		UInt,
		Float,
		String,
	}

	public class Opcode
	{
		public enum Value : uint
		{
			OP_UINT_TO_FLT,             /* 0x00 */
			OP_ADVANCE_STR_NUL,         /* 0x01 */
			OP_UINT_TO_STR,             /* 0x02 */
			OP_UINT_TO_NONE,            /* 0x03 */
			UNUSED1,                    /* 0x04 */
			OP_ADD_OBJECT,              /* 0x05 */
			UNUSED2,                    /* 0x06 */
			OP_CALLFUNC_RESOLVE,        /* 0x07 */
			OP_FLT_TO_UINT,             /* 0x08 */
			OP_FLT_TO_STR,              /* 0x09 */
			OP_STR_TO_NONE_2,           /* 0x0A */
			OP_LOADVAR_UINT,            /* 0x0B */
			OP_SAVEVAR_STR,             /* 0x0C */
			OP_JMPIFNOT,                /* 0x0D */
			OP_SAVEVAR_FLT,             /* 0x0E */
			OP_LOADIMMED_UINT,          /* 0x0F */
			OP_LOADIMMED_FLT,           /* 0x10 */
			OP_LOADIMMED_IDENT,         /* 0x11 */
			OP_TAG_TO_STR,              /* 0x12 */
			OP_LOADIMMED_STR,           /* 0x13 */
			OP_ADVANCE_STR_APPENDCHAR,  /* 0x14 */
			OP_TERMINATE_REWIND_STR,    /* 0x15 */
			OP_ADVANCE_STR,             /* 0x16 */
			OP_CMPLE,                   /* 0x17 */
			OP_SETCURFIELD,             /* 0x18 */
			OP_SETCURFIELD_ARRAY,       /* 0x19 */
			OP_JMPIF_NP,                /* 0x1A */
			OP_JMPIFF,                  /* 0x1B */
			OP_JMP,                     /* 0x1C */
			OP_BITOR,                   /* 0x1D */
			OP_SHL,                     /* 0x1E */
			OP_SHR,                     /* 0x1F */
			OP_STR_TO_NONE,             /* 0x20 */
			OP_COMPARE_STR,             /* 0x21 */
			OP_CMPEQ,                   /* 0x22 */
			OP_CMPGR,                   /* 0x23 */
			OP_CMPNE,                   /* 0x24 */
			OP_OR,                      /* 0x25 */
			OP_STR_TO_UINT,             /* 0x26 */
			OP_SETCUROBJECT,            /* 0x27 */
			OP_PUSH_FRAME,              /* 0x28 */
			OP_REWIND_STR,              /* 0x29 */
			OP_SAVEFIELD_UINT,          /* 0x2A */
			OP_CALLFUNC,                /* 0x2B */
			OP_LOADVAR_STR,             /* 0x2C */
			OP_LOADVAR_FLT,             /* 0x2D */
			OP_SAVEFIELD_FLT,           /* 0x2E */
			OP_LOADFIELD_FLT,           /* 0x2F */
			OP_MOD,                     /* 0x30 */
			OP_LOADFIELD_UINT,          /* 0x31 */
			OP_JMPIFFNOT,               /* 0x32 */
			OP_JMPIF,                   /* 0x33 */
			OP_SAVEVAR_UINT,            /* 0x34 */
			OP_SUB,                     /* 0x35 */
			OP_MUL,                     /* 0x36 */
			OP_DIV,                     /* 0x37 */
			OP_NEG,                     /* 0x38 */
			OP_INVALID,                 /* 0x39 */
			OP_STR_TO_FLT,              /* 0x3A */
			OP_END_OBJECT,              /* 0x3B */
			OP_CMPLT,                   /* 0x3C */
			OP_BREAK,                   /* 0x3D (Debugger breakpoint -- NOT a break statement) */
			OP_SETCURVAR_CREATE,        /* 0x3E */
			OP_SETCUROBJECT_NEW,        /* 0x3F */
			OP_NOT,                     /* 0x40 */
			OP_NOTF,                    /* 0x41 */
			OP_SETCURVAR,               /* 0x42 */
			OP_SETCURVAR_ARRAY,         /* 0x43 */
			OP_ADD,                     /* 0x44 */
			OP_SETCURVAR_ARRAY_CREATE,  /* 0x45 */
			OP_JMPIFNOT_NP,             /* 0x46 */
			OP_AND,                     /* 0x47 */
			OP_RETURN,                  /* 0x48 */
			OP_XOR,                     /* 0x49 */
			OP_CMPGE,                   /* 0x4A */
			OP_LOADFIELD_STR,           /* 0x4B */
			OP_SAVEFIELD_STR,           /* 0x4C */
			OP_BITAND,                  /* 0x4D */
			OP_ONESCOMPLEMENT,          /* 0x4E */
			OP_ADVANCE_STR_COMMA,       /* 0x4F */
			OP_PUSH,                    /* 0x50 */
			OP_FLT_TO_NONE,             /* 0x51 */
			OP_CREATE_OBJECT,           /* 0x52 */
			OP_FUNC_DECL,               /* 0x53 */
		};

		public static bool IsValid (uint op) => Enum.IsDefined(typeof(Value), op);

		protected static ReturnValue GetReturnValue (Value op)
		{
			switch (op)
			{
				case Value.OP_RETURN:
				case Value.OP_JMP:
				case Value.OP_JMPIF:
				case Value.OP_JMPIFF:
				case Value.OP_JMPIFNOT:
				case Value.OP_JMPIFFNOT:
				case Value.OP_JMPIF_NP:
				case Value.OP_JMPIFNOT_NP:
				case Value.OP_STR_TO_NONE:
				case Value.OP_STR_TO_NONE_2:
				case Value.OP_FLT_TO_NONE:
				case Value.OP_UINT_TO_NONE:
				{
					return ReturnValue.ToFalse;
				}

				case Value.OP_LOADVAR_STR:
				case Value.OP_SAVEVAR_UINT:
				case Value.OP_SAVEVAR_FLT:
				case Value.OP_SAVEVAR_STR:
				case Value.OP_LOADFIELD_STR:
				case Value.OP_SAVEFIELD_UINT:
				case Value.OP_SAVEFIELD_FLT:
				case Value.OP_SAVEFIELD_STR:
				case Value.OP_FLT_TO_STR:
				case Value.OP_UINT_TO_STR:
				case Value.OP_LOADIMMED_UINT:
				case Value.OP_LOADIMMED_FLT:
				case Value.OP_TAG_TO_STR:
				case Value.OP_LOADIMMED_STR:
				case Value.OP_LOADIMMED_IDENT:
				case Value.OP_CALLFUNC:
				case Value.OP_CALLFUNC_RESOLVE:
				case Value.OP_REWIND_STR:
				{
					return ReturnValue.ToTrue;
				}

				default:
				{
					return ReturnValue.NoChange;
				}
			}
		}

		/* I believe there are TypeReq for opcodes other than type conversions, but those aren't
		   necessary for our purposes. TODO: If the need arises. */
		protected static TypeReq GetTypeReq (Value op)
		{
			switch (op)
			{
				case Value.OP_STR_TO_UINT:
				case Value.OP_FLT_TO_UINT:
				{
					return TypeReq.UInt;
				}

				case Value.OP_STR_TO_FLT:
				case Value.OP_UINT_TO_FLT:
				{
					return TypeReq.Float;
				}

				case Value.OP_FLT_TO_STR:
				case Value.OP_UINT_TO_STR:
				{
					return TypeReq.String;
				}

				case Value.OP_STR_TO_NONE:
				case Value.OP_STR_TO_NONE_2:
				case Value.OP_FLT_TO_NONE:
				case Value.OP_UINT_TO_NONE:
				{
					return TypeReq.None;
				}

				default:
				{
					return TypeReq.Invalid;
				}
			}
		}

		public static explicit operator Opcode (uint op)
		{
			if (!IsValid(op))
			{
				throw new InvalidCastException($"Cannot convert uint to Opcode: {op} is not a valid value");
			}

			return new Opcode((Value) op);
		}

		public Value Op { get; }
		public ReturnValue ReturnValue { get; }
		public TypeReq TypeReq { get; }

		public Opcode (Value op)
		{
			Op = op;
			ReturnValue = GetReturnValue(op);
			TypeReq = GetTypeReq(op);
		}

		public override string ToString () => IsValid((uint) Op) ? Op.ToString() : "<UNKNOWN>";
	}
}
