using System;

namespace DSODecompiler.Opcodes
{
	public static class Ops
	{
		public enum Value
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

		public static readonly uint MaxValue;
		public static readonly uint MinValue;

		static Ops ()
		{
			var values = Enum.GetValues(typeof(Value));
			var min = uint.MaxValue;
			var max = uint.MinValue;

			foreach (var op in values)
			{
				var value = Convert.ToUInt32(op);

				if (value < min)
				{
					min = value;
				}

				if (value > max)
				{
					max = value;
				}
			}

			MinValue = min;
			MaxValue = max;
		}

		public static bool IsValid (uint op) => op >= MinValue && op <= MaxValue;

		public static BranchType GetBranchType (Value op)
		{
			switch (op)
			{
				case Value.OP_JMP:
				{
					return BranchType.Unconditional;
				}

				case Value.OP_JMPIF:
				case Value.OP_JMPIFF:
				case Value.OP_JMPIFNOT:
				case Value.OP_JMPIFFNOT:
				{
					return BranchType.Conditional;
				}

				case Value.OP_JMPIF_NP:
				case Value.OP_JMPIFNOT_NP:
				{
					return BranchType.LogicalBranch;
				}

				default:
				{
					return BranchType.Invalid;
				}
			}
		}

		public static ConvertToType GetConvertToType (Value op)
		{
			switch (op)
			{
				case Value.OP_STR_TO_UINT:
				case Value.OP_FLT_TO_UINT:
				{
					return ConvertToType.UInt;
				}

				case Value.OP_STR_TO_FLT:
				case Value.OP_UINT_TO_FLT:
				{
					return ConvertToType.Float;
				}

				case Value.OP_FLT_TO_STR:
				case Value.OP_UINT_TO_STR:
				{
					return ConvertToType.String;
				}

				case Value.OP_STR_TO_NONE:
				case Value.OP_STR_TO_NONE_2:
				case Value.OP_FLT_TO_NONE:
				case Value.OP_UINT_TO_NONE:
				{
					return ConvertToType.None;
				}

				default:
				{
					return ConvertToType.Invalid;
				}
			}
		}

		public static AdvanceStringType GetAdvanceStringType (Value op)
		{
			switch (op)
			{
				case Value.OP_ADVANCE_STR:
				{
					return AdvanceStringType.Default;
				}

				case Value.OP_ADVANCE_STR_APPENDCHAR:
				{
					return AdvanceStringType.Append;
				}

				case Value.OP_ADVANCE_STR_COMMA:
				{
					return AdvanceStringType.Comma;
				}

				case Value.OP_ADVANCE_STR_NUL:
				{
					return AdvanceStringType.Null;
				}

				default:
				{
					return AdvanceStringType.Invalid;
				}
			}
		}

		public static ReturnValue GetReturnValueChange (Value op)
		{
			switch (op)
			{
				case Value.OP_RETURN:
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

				case Value.OP_STR_TO_NONE:
				case Value.OP_STR_TO_NONE_2:
				case Value.OP_FLT_TO_NONE:
				case Value.OP_UINT_TO_NONE:
				{
					return ReturnValue.ToFalse;
				}

				default:
				{
					return ReturnValue.NoChange;
				}
			}
		}

		public static OpcodeType GetOpcodeType (Value op)
		{
			switch (op)
			{
				case Value.OP_FUNC_DECL:
				{
					return OpcodeType.FunctionDecl;
				}

				case Value.OP_CREATE_OBJECT:
				{
					return OpcodeType.CreateObject;
				}

				case Value.OP_ADD_OBJECT:
				{
					return OpcodeType.AddObject;
				}

				case Value.OP_END_OBJECT:
				{
					return OpcodeType.EndObject;
				}

				case Value.OP_JMP:
				case Value.OP_JMPIF:
				case Value.OP_JMPIFF:
				case Value.OP_JMPIFNOT:
				case Value.OP_JMPIFFNOT:
				case Value.OP_JMPIF_NP:
				case Value.OP_JMPIFNOT_NP:
				{
					return OpcodeType.Branch;
				}

				case Value.OP_RETURN:
				{
					return OpcodeType.Return;
				}

				case Value.OP_CMPEQ:
				case Value.OP_CMPGR:
				case Value.OP_CMPGE:
				case Value.OP_CMPLT:
				case Value.OP_CMPLE:
				case Value.OP_CMPNE:
				case Value.OP_XOR:
				case Value.OP_MOD:
				case Value.OP_BITAND:
				case Value.OP_BITOR:
				case Value.OP_SHR:
				case Value.OP_SHL:
				case Value.OP_AND:
				case Value.OP_OR:
				case Value.OP_ADD:
				case Value.OP_SUB:
				case Value.OP_MUL:
				case Value.OP_DIV:
				{
					return OpcodeType.Binary;
				}

				case Value.OP_COMPARE_STR:
				{
					return OpcodeType.BinaryString;
				}

				case Value.OP_NEG:
				case Value.OP_NOT:
				case Value.OP_NOTF:
				case Value.OP_ONESCOMPLEMENT:
				{
					return OpcodeType.Unary;
				}

				case Value.OP_SETCURVAR:
				case Value.OP_SETCURVAR_CREATE:
				{
					return OpcodeType.SetCurVar;
				}

				case Value.OP_SETCURVAR_ARRAY:
				case Value.OP_SETCURVAR_ARRAY_CREATE:
				{
					return OpcodeType.SetCurVarArray;
				}

				case Value.OP_LOADVAR_UINT:
				case Value.OP_LOADVAR_FLT:
				case Value.OP_LOADVAR_STR:
				{
					return OpcodeType.LoadVar;
				}

				case Value.OP_SAVEVAR_UINT:
				case Value.OP_SAVEVAR_FLT:
				case Value.OP_SAVEVAR_STR:
				{
					return OpcodeType.SaveVar;
				}

				case Value.OP_SETCUROBJECT:
				{
					return OpcodeType.SetCurObject;
				}

				case Value.OP_SETCUROBJECT_NEW:
				{
					return OpcodeType.SetCurObjectNew;
				}

				case Value.OP_SETCURFIELD:
				{
					return OpcodeType.SetCurField;
				}

				case Value.OP_SETCURFIELD_ARRAY:
				{
					return OpcodeType.SetCurFieldArray;
				}

				case Value.OP_LOADFIELD_UINT:
				case Value.OP_LOADFIELD_FLT:
				case Value.OP_LOADFIELD_STR:
				{
					return OpcodeType.LoadField;
				}

				case Value.OP_SAVEFIELD_UINT:
				case Value.OP_SAVEFIELD_FLT:
				case Value.OP_SAVEFIELD_STR:
				{
					return OpcodeType.SaveField;
				}

				case Value.OP_STR_TO_UINT:
				case Value.OP_FLT_TO_UINT:
				{
					return OpcodeType.ConvertToUInt;
				}

				case Value.OP_STR_TO_FLT:
				case Value.OP_UINT_TO_FLT:
				{
					return OpcodeType.ConvertToFloat;
				}

				case Value.OP_FLT_TO_STR:
				case Value.OP_UINT_TO_STR:
				{
					return OpcodeType.ConvertToString;
				}

				case Value.OP_STR_TO_NONE:
				case Value.OP_STR_TO_NONE_2:
				case Value.OP_FLT_TO_NONE:
				case Value.OP_UINT_TO_NONE:
				{
					return OpcodeType.ConvertToNone;
				}

				case Value.OP_LOADIMMED_UINT:
				{
					return OpcodeType.LoadImmedUInt;
				}

				case Value.OP_LOADIMMED_FLT:
				{
					return OpcodeType.LoadImmedFloat;
				}

				case Value.OP_TAG_TO_STR:
				case Value.OP_LOADIMMED_STR:
				{
					return OpcodeType.LoadImmedString;
				}

				case Value.OP_LOADIMMED_IDENT:
				{
					return OpcodeType.LoadImmedIdent;
				}

				case Value.OP_CALLFUNC:
				case Value.OP_CALLFUNC_RESOLVE:
				{
					return OpcodeType.FunctionCall;
				}

				case Value.OP_ADVANCE_STR:
				case Value.OP_ADVANCE_STR_COMMA:
				case Value.OP_ADVANCE_STR_NUL:
				{
					return OpcodeType.AdvanceString;
				}

				case Value.OP_ADVANCE_STR_APPENDCHAR:
				{
					return OpcodeType.AdvanceAppendString;
				}

				case Value.OP_REWIND_STR:
				{
					return OpcodeType.RewindString;
				}

				case Value.OP_TERMINATE_REWIND_STR:
				{
					return OpcodeType.TerminateRewindString;
				}

				case Value.OP_PUSH:
				{
					return OpcodeType.Push;
				}

				case Value.OP_PUSH_FRAME:
				{
					return OpcodeType.PushFrame;
				}

				case Value.OP_BREAK:
				{
					return OpcodeType.DebugBreak;
				}

				case Value.UNUSED1:
				case Value.UNUSED2:
				{
					return OpcodeType.Unused;
				}

				case Value.OP_INVALID:
				default:
				{
					return OpcodeType.Invalid;
				}
			}
		}
	}
}
