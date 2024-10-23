/**
 * Ops.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace DSO.Opcodes
{
	public enum ReturnValue : byte
	{
		ToFalse,
		ToTrue,
		NoChange,
	}

	public enum TypeReq : byte
	{
		None,
		UInt,
		Float,
		String,
		Invalid,
	}

	// We have to have some sort of identifier beyond the actual value.
	public enum OpcodeTag : uint
	{
		OP_FUNC_DECL,

		OP_CREATE_OBJECT,
		OP_ADD_OBJECT,
		OP_END_OBJECT,

		OP_JMPIFFNOT,
		OP_JMPIFNOT,
		OP_JMPIFF,
		OP_JMPIF,
		OP_JMPIFNOT_NP,
		OP_JMPIF_NP,
		OP_JMP,

		OP_RETURN,

		OP_CMPEQ,
		OP_CMPGR,
		OP_CMPGE,
		OP_CMPLT,
		OP_CMPLE,
		OP_CMPNE,

		OP_XOR,
		OP_MOD,
		OP_BITAND,
		OP_BITOR,
		OP_NOT,
		OP_NOTF,
		OP_ONESCOMPLEMENT,

		OP_SHR,
		OP_SHL,
		OP_AND,
		OP_OR,

		OP_ADD,
		OP_SUB,
		OP_MUL,
		OP_DIV,
		OP_NEG,

		OP_SETCURVAR,
		OP_SETCURVAR_CREATE,
		OP_SETCURVAR_ARRAY,
		OP_SETCURVAR_ARRAY_CREATE,

		OP_LOADVAR_UINT,
		OP_LOADVAR_FLT,
		OP_LOADVAR_STR,

		OP_SAVEVAR_UINT,
		OP_SAVEVAR_FLT,
		OP_SAVEVAR_STR,

		OP_SETCUROBJECT,
		OP_SETCUROBJECT_NEW,

		OP_SETCURFIELD,
		OP_SETCURFIELD_ARRAY,

		OP_LOADFIELD_UINT,
		OP_LOADFIELD_FLT,
		OP_LOADFIELD_STR,

		OP_SAVEFIELD_UINT,
		OP_SAVEFIELD_FLT,
		OP_SAVEFIELD_STR,

		OP_STR_TO_UINT,
		OP_STR_TO_FLT,
		OP_STR_TO_NONE,
		OP_FLT_TO_UINT,
		OP_FLT_TO_STR,
		OP_FLT_TO_NONE,
		OP_UINT_TO_FLT,
		OP_UINT_TO_STR,
		OP_UINT_TO_NONE,

		OP_LOADIMMED_UINT,
		OP_LOADIMMED_FLT,
		OP_TAG_TO_STR,
		OP_LOADIMMED_STR,
		OP_LOADIMMED_IDENT,

		OP_CALLFUNC_RESOLVE,
		OP_CALLFUNC,

		OP_ADVANCE_STR,
		OP_ADVANCE_STR_APPENDCHAR,
		OP_ADVANCE_STR_COMMA,
		OP_ADVANCE_STR_NUL,
		OP_REWIND_STR,
		OP_TERMINATE_REWIND_STR,
		OP_COMPARE_STR,

		OP_PUSH,
		OP_PUSH_FRAME,

		OP_BREAK,

		OP_UNUSED1,
		OP_UNUSED2,
		OP_UNUSED3,

		OP_INVALID,
	}

	/// <summary>
	/// Base `Ops` class uses opcodes for Torque Game Engine 1.0-1.3.
	/// </summary>
	public class Ops
	{
		public virtual uint OP_FUNC_DECL => 0x00;

		public virtual uint OP_CREATE_OBJECT => 0x01;
		public virtual uint OP_ADD_OBJECT => 0x04;
		public virtual uint OP_END_OBJECT => 0x05;

		public virtual uint OP_JMPIFFNOT => 0x06;
		public virtual uint OP_JMPIFNOT => 0x07;
		public virtual uint OP_JMPIFF => 0x08;
		public virtual uint OP_JMPIF => 0x09;
		public virtual uint OP_JMPIFNOT_NP => 0x0A;
		public virtual uint OP_JMPIF_NP => 0x0B;
		public virtual uint OP_JMP => 0x0C;

		public virtual uint OP_RETURN => 0x0D;

		public virtual uint OP_CMPEQ => 0x0E;
		public virtual uint OP_CMPGR => 0x0F;
		public virtual uint OP_CMPGE => 0x10;
		public virtual uint OP_CMPLT => 0x11;
		public virtual uint OP_CMPLE => 0x12;
		public virtual uint OP_CMPNE => 0x13;

		public virtual uint OP_XOR => 0x14;
		public virtual uint OP_MOD => 0x15;
		public virtual uint OP_BITAND => 0x16;
		public virtual uint OP_BITOR => 0x17;
		public virtual uint OP_NOT => 0x18;
		public virtual uint OP_NOTF => 0x19;
		public virtual uint OP_ONESCOMPLEMENT => 0x1A;

		public virtual uint OP_SHR => 0x1B;
		public virtual uint OP_SHL => 0x1C;
		public virtual uint OP_AND => 0x1D;
		public virtual uint OP_OR => 0x1E;

		public virtual uint OP_ADD => 0x1F;
		public virtual uint OP_SUB => 0x20;
		public virtual uint OP_MUL => 0x21;
		public virtual uint OP_DIV => 0x22;
		public virtual uint OP_NEG => 0x23;

		public virtual uint OP_SETCURVAR => 0x24;
		public virtual uint OP_SETCURVAR_CREATE => 0x25;
		public virtual uint OP_SETCURVAR_ARRAY => 0x26;
		public virtual uint OP_SETCURVAR_ARRAY_CREATE => 0x27;

		public virtual uint OP_LOADVAR_UINT => 0x28;
		public virtual uint OP_LOADVAR_FLT => 0x29;
		public virtual uint OP_LOADVAR_STR => 0x2A;

		public virtual uint OP_SAVEVAR_UINT => 0x2B;
		public virtual uint OP_SAVEVAR_FLT => 0x2C;
		public virtual uint OP_SAVEVAR_STR => 0x2D;

		public virtual uint OP_SETCUROBJECT => 0x2E;
		public virtual uint OP_SETCUROBJECT_NEW => 0x2F;

		public virtual uint OP_SETCURFIELD => 0x30;
		public virtual uint OP_SETCURFIELD_ARRAY => 0x31;

		public virtual uint OP_LOADFIELD_UINT => 0x32;
		public virtual uint OP_LOADFIELD_FLT => 0x33;
		public virtual uint OP_LOADFIELD_STR => 0x34;

		public virtual uint OP_SAVEFIELD_UINT => 0x35;
		public virtual uint OP_SAVEFIELD_FLT => 0x36;
		public virtual uint OP_SAVEFIELD_STR => 0x37;

		public virtual uint OP_STR_TO_UINT => 0x38;
		public virtual uint OP_STR_TO_FLT => 0x39;
		public virtual uint OP_STR_TO_NONE => 0x3A;
		public virtual uint OP_FLT_TO_UINT => 0x3B;
		public virtual uint OP_FLT_TO_STR => 0x3C;
		public virtual uint OP_FLT_TO_NONE => 0x3D;
		public virtual uint OP_UINT_TO_FLT => 0x3E;
		public virtual uint OP_UINT_TO_STR => 0x3F;
		public virtual uint OP_UINT_TO_NONE => 0x40;

		public virtual uint OP_LOADIMMED_UINT => 0x41;
		public virtual uint OP_LOADIMMED_FLT => 0x42;
		public virtual uint OP_TAG_TO_STR => 0x43;
		public virtual uint OP_LOADIMMED_STR => 0x44;
		public virtual uint OP_LOADIMMED_IDENT => 0x45;

		public virtual uint OP_CALLFUNC_RESOLVE => 0x46;
		public virtual uint OP_CALLFUNC => 0x47;

		public virtual uint OP_ADVANCE_STR => 0x49;
		public virtual uint OP_ADVANCE_STR_APPENDCHAR => 0x4A;
		public virtual uint OP_ADVANCE_STR_COMMA => 0x4B;
		public virtual uint OP_ADVANCE_STR_NUL => 0x4C;
		public virtual uint OP_REWIND_STR => 0x4D;
		public virtual uint OP_TERMINATE_REWIND_STR => 0x4E;
		public virtual uint OP_COMPARE_STR => 0x4F;

		public virtual uint OP_PUSH => 0x50;
		public virtual uint OP_PUSH_FRAME => 0x51;

		public virtual uint OP_BREAK => 0x52;

		public virtual uint OP_UNUSED1 => 0x02;
		public virtual uint OP_UNUSED2 => 0x03;
		public virtual uint OP_UNUSED3 => 0x48;

		public virtual uint OP_INVALID => 0x53;

		protected Dictionary<uint, OpcodeTag> _tags = [];

		public Ops()
		{
			_tags = new()
			{
				{ OP_FUNC_DECL, OpcodeTag.OP_FUNC_DECL },

				{ OP_CREATE_OBJECT, OpcodeTag.OP_CREATE_OBJECT },
				{ OP_ADD_OBJECT, OpcodeTag.OP_ADD_OBJECT },
				{ OP_END_OBJECT, OpcodeTag.OP_END_OBJECT },

				{ OP_JMPIFFNOT, OpcodeTag.OP_JMPIFFNOT },
				{ OP_JMPIFNOT, OpcodeTag.OP_JMPIFNOT },
				{ OP_JMPIFF, OpcodeTag.OP_JMPIFF },
				{ OP_JMPIF, OpcodeTag.OP_JMPIF },
				{ OP_JMPIFNOT_NP, OpcodeTag.OP_JMPIFNOT_NP },
				{ OP_JMPIF_NP, OpcodeTag.OP_JMPIF_NP },
				{ OP_JMP, OpcodeTag.OP_JMP },

				{ OP_RETURN, OpcodeTag.OP_RETURN },

				{ OP_CMPEQ, OpcodeTag.OP_CMPEQ },
				{ OP_CMPGR, OpcodeTag.OP_CMPGR },
				{ OP_CMPGE, OpcodeTag.OP_CMPGE },
				{ OP_CMPLT, OpcodeTag.OP_CMPLT },
				{ OP_CMPLE, OpcodeTag.OP_CMPLE },
				{ OP_CMPNE, OpcodeTag.OP_CMPNE },

				{ OP_XOR, OpcodeTag.OP_XOR },
				{ OP_MOD, OpcodeTag.OP_MOD },
				{ OP_BITAND, OpcodeTag.OP_BITAND },
				{ OP_BITOR, OpcodeTag.OP_BITOR },
				{ OP_NOT, OpcodeTag.OP_NOT },
				{ OP_NOTF, OpcodeTag.OP_NOTF },
				{ OP_ONESCOMPLEMENT, OpcodeTag.OP_ONESCOMPLEMENT },

				{ OP_SHR, OpcodeTag.OP_SHR },
				{ OP_SHL, OpcodeTag.OP_SHL },
				{ OP_AND, OpcodeTag.OP_AND },
				{ OP_OR, OpcodeTag.OP_OR },

				{ OP_ADD, OpcodeTag.OP_ADD },
				{ OP_SUB, OpcodeTag.OP_SUB },
				{ OP_MUL, OpcodeTag.OP_MUL },
				{ OP_DIV, OpcodeTag.OP_DIV },
				{ OP_NEG, OpcodeTag.OP_NEG },

				{ OP_SETCURVAR, OpcodeTag.OP_SETCURVAR },
				{ OP_SETCURVAR_CREATE, OpcodeTag.OP_SETCURVAR_CREATE },
				{ OP_SETCURVAR_ARRAY, OpcodeTag.OP_SETCURVAR_ARRAY },
				{ OP_SETCURVAR_ARRAY_CREATE, OpcodeTag.OP_SETCURVAR_ARRAY_CREATE },

				{ OP_LOADVAR_UINT, OpcodeTag.OP_LOADVAR_UINT },
				{ OP_LOADVAR_FLT, OpcodeTag.OP_LOADVAR_FLT },
				{ OP_LOADVAR_STR, OpcodeTag.OP_LOADVAR_STR },

				{ OP_SAVEVAR_UINT, OpcodeTag.OP_SAVEVAR_UINT },
				{ OP_SAVEVAR_FLT, OpcodeTag.OP_SAVEVAR_FLT },
				{ OP_SAVEVAR_STR, OpcodeTag.OP_SAVEVAR_STR },

				{ OP_SETCUROBJECT, OpcodeTag.OP_SETCUROBJECT },
				{ OP_SETCUROBJECT_NEW, OpcodeTag.OP_SETCUROBJECT_NEW },

				{ OP_SETCURFIELD, OpcodeTag.OP_SETCURFIELD },
				{ OP_SETCURFIELD_ARRAY, OpcodeTag.OP_SETCURFIELD_ARRAY },

				{ OP_LOADFIELD_UINT, OpcodeTag.OP_LOADFIELD_UINT },
				{ OP_LOADFIELD_FLT, OpcodeTag.OP_LOADFIELD_FLT },
				{ OP_LOADFIELD_STR, OpcodeTag.OP_LOADFIELD_STR },

				{ OP_SAVEFIELD_UINT, OpcodeTag.OP_SAVEFIELD_UINT },
				{ OP_SAVEFIELD_FLT, OpcodeTag.OP_SAVEFIELD_FLT },
				{ OP_SAVEFIELD_STR, OpcodeTag.OP_SAVEFIELD_STR },

				{ OP_STR_TO_UINT, OpcodeTag.OP_STR_TO_UINT },
				{ OP_STR_TO_FLT, OpcodeTag.OP_STR_TO_FLT },
				{ OP_STR_TO_NONE, OpcodeTag.OP_STR_TO_NONE },
				{ OP_FLT_TO_UINT, OpcodeTag.OP_FLT_TO_UINT },
				{ OP_FLT_TO_STR, OpcodeTag.OP_FLT_TO_STR },
				{ OP_FLT_TO_NONE, OpcodeTag.OP_FLT_TO_NONE },
				{ OP_UINT_TO_FLT, OpcodeTag.OP_UINT_TO_FLT },
				{ OP_UINT_TO_STR, OpcodeTag.OP_UINT_TO_STR },
				{ OP_UINT_TO_NONE, OpcodeTag.OP_UINT_TO_NONE },

				{ OP_LOADIMMED_UINT, OpcodeTag.OP_LOADIMMED_UINT },
				{ OP_LOADIMMED_FLT, OpcodeTag.OP_LOADIMMED_FLT },
				{ OP_TAG_TO_STR, OpcodeTag.OP_TAG_TO_STR },
				{ OP_LOADIMMED_STR, OpcodeTag.OP_LOADIMMED_STR },
				{ OP_LOADIMMED_IDENT, OpcodeTag.OP_LOADIMMED_IDENT },

				{ OP_CALLFUNC_RESOLVE, OpcodeTag.OP_CALLFUNC_RESOLVE },
				{ OP_CALLFUNC, OpcodeTag.OP_CALLFUNC },

				{ OP_ADVANCE_STR, OpcodeTag.OP_ADVANCE_STR },
				{ OP_ADVANCE_STR_APPENDCHAR, OpcodeTag.OP_ADVANCE_STR_APPENDCHAR },
				{ OP_ADVANCE_STR_COMMA, OpcodeTag.OP_ADVANCE_STR_COMMA },
				{ OP_ADVANCE_STR_NUL, OpcodeTag.OP_ADVANCE_STR_NUL },
				{ OP_REWIND_STR, OpcodeTag.OP_REWIND_STR },
				{ OP_TERMINATE_REWIND_STR, OpcodeTag.OP_TERMINATE_REWIND_STR },
				{ OP_COMPARE_STR, OpcodeTag.OP_COMPARE_STR },

				{ OP_PUSH, OpcodeTag.OP_PUSH },
				{ OP_PUSH_FRAME, OpcodeTag.OP_PUSH_FRAME },

				{ OP_BREAK, OpcodeTag.OP_BREAK },

				{ OP_INVALID, OpcodeTag.OP_INVALID },
			};

			if (!_tags.ContainsKey(OP_UNUSED1))
			{
				_tags[OP_UNUSED1] = OpcodeTag.OP_UNUSED1;
			}

			if (!_tags.ContainsKey(OP_UNUSED2))
			{
				_tags[OP_UNUSED2] = OpcodeTag.OP_UNUSED2;
			}

			if (!_tags.ContainsKey(OP_UNUSED3))
			{
				_tags[OP_UNUSED3] = OpcodeTag.OP_UNUSED3;
			}
		}

		public bool IsValid(uint value) => _tags.ContainsKey(value) && value != OP_INVALID;

		public OpcodeTag GetOpcodeTag(uint op) => _tags.TryGetValue(op, out OpcodeTag tag) ? tag : OpcodeTag.OP_INVALID;

		public ReturnValue GetReturnValue(uint op) => GetOpcodeTag(op) switch
		{
			OpcodeTag.OP_STR_TO_NONE or OpcodeTag.OP_FLT_TO_NONE or OpcodeTag.OP_UINT_TO_NONE or
			OpcodeTag.OP_JMPIF or OpcodeTag.OP_JMPIFF or
			OpcodeTag.OP_JMPIFNOT or OpcodeTag.OP_JMPIFFNOT or
			OpcodeTag.OP_RETURN => ReturnValue.ToFalse,

			OpcodeTag.OP_SAVEVAR_UINT or OpcodeTag.OP_SAVEVAR_FLT or OpcodeTag.OP_SAVEVAR_STR or
			OpcodeTag.OP_SAVEFIELD_UINT or OpcodeTag.OP_SAVEFIELD_FLT or OpcodeTag.OP_SAVEFIELD_STR or
			OpcodeTag.OP_LOADVAR_STR or OpcodeTag.OP_LOADFIELD_STR or
			OpcodeTag.OP_FLT_TO_STR or OpcodeTag.OP_UINT_TO_STR or
			OpcodeTag.OP_LOADIMMED_UINT or OpcodeTag.OP_LOADIMMED_FLT or
			OpcodeTag.OP_TAG_TO_STR or OpcodeTag.OP_LOADIMMED_STR or OpcodeTag.OP_LOADIMMED_IDENT or
			OpcodeTag.OP_CALLFUNC or OpcodeTag.OP_CALLFUNC_RESOLVE or
			OpcodeTag.OP_REWIND_STR => ReturnValue.ToTrue,

			_ => ReturnValue.NoChange,
		};

		public TypeReq GetTypeReq(uint op) => GetOpcodeTag(op) switch
		{
			OpcodeTag.OP_STR_TO_UINT or OpcodeTag.OP_FLT_TO_UINT => TypeReq.UInt,
			OpcodeTag.OP_STR_TO_FLT or OpcodeTag.OP_UINT_TO_FLT => TypeReq.Float,
			OpcodeTag.OP_FLT_TO_STR or OpcodeTag.OP_UINT_TO_STR => TypeReq.String,
			OpcodeTag.OP_STR_TO_NONE or OpcodeTag.OP_FLT_TO_NONE or OpcodeTag.OP_UINT_TO_NONE => TypeReq.None,

			_ => TypeReq.Invalid,
		};
	}
}
