namespace DSO.Versions.TFD
{
	public class Ops : Opcodes.Ops
	{
		public override uint OP_FUNC_DECL => 0x00;

		public override uint OP_CREATE_OBJECT => 0x01;
		public override uint OP_ADD_OBJECT => 0x04;
		public override uint OP_END_OBJECT => 0x05;

		public override uint OP_JMPIFFNOT => 0x06;
		public override uint OP_JMPIFNOT => 0x07;
		public override uint OP_JMPIFF => 0x08;
		public override uint OP_JMPIF => 0x0B;
		public override uint OP_JMPIFNOT_NP => 0x0C;
		public override uint OP_JMPIF_NP => 0x0D;
		public override uint OP_JMP => 0x0E;

		public override uint OP_RETURN => 0x12;

		public override uint OP_CMPEQ => 0x0F;
		public override uint OP_CMPGR => 0x11;
		public override uint OP_CMPGE => 0x10;
		public override uint OP_CMPLT => 0x0A;
		public override uint OP_CMPLE => 0x09;
		public override uint OP_CMPNE => 0x17;

		public override uint OP_XOR => 0x13;
		public override uint OP_MOD => 0x14;
		public override uint OP_BITAND => 0x15;
		public override uint OP_BITOR => 0x16;
		public override uint OP_NOT => 0x18;
		public override uint OP_NOTF => 0x19;
		public override uint OP_ONESCOMPLEMENT => 0x1A;

		public override uint OP_SHR => 0x1B;
		public override uint OP_SHL => 0x1C;
		public override uint OP_AND => 0x1D;
		public override uint OP_OR => 0x1E;

		public override uint OP_ADD => 0x1F;
		public override uint OP_SUB => 0x20;
		public override uint OP_MUL => 0x21;
		public override uint OP_DIV => 0x22;
		public override uint OP_NEG => 0x23;

		public override uint OP_SETCURVAR => 0x24;
		public override uint OP_SETCURVAR_CREATE => 0x25;
		public override uint OP_SETCURVAR_ARRAY => 0x26;
		public override uint OP_SETCURVAR_ARRAY_CREATE => 0x27;

		public override uint OP_LOADVAR_UINT => 0x28;
		public override uint OP_LOADVAR_FLT => 0x29;
		public override uint OP_LOADVAR_STR => 0x2A;

		public override uint OP_SAVEVAR_UINT => 0x2B;
		public override uint OP_SAVEVAR_FLT => 0x2C;
		public override uint OP_SAVEVAR_STR => 0x2D;

		public override uint OP_SETCUROBJECT => 0x2E;
		public override uint OP_SETCUROBJECT_NEW => 0x2F;

		public override uint OP_SETCURFIELD => 0x30;
		public override uint OP_SETCURFIELD_ARRAY => 0x31;

		public override uint OP_LOADFIELD_UINT => 0x32;
		public override uint OP_LOADFIELD_FLT => 0x33;
		public override uint OP_LOADFIELD_STR => 0x34;

		public override uint OP_SAVEFIELD_UINT => 0x35;
		public override uint OP_SAVEFIELD_FLT => 0x36;
		public override uint OP_SAVEFIELD_STR => 0x37;

		public override uint OP_STR_TO_UINT => 0x38;
		public override uint OP_STR_TO_FLT => 0x39;
		public override uint OP_STR_TO_NONE => 0x3A;
		public override uint OP_FLT_TO_UINT => 0x3B;
		public override uint OP_FLT_TO_STR => 0x3C;
		public override uint OP_FLT_TO_NONE => 0x3D;
		public override uint OP_UINT_TO_FLT => 0x3E;
		public override uint OP_UINT_TO_STR => 0x3F;
		public override uint OP_UINT_TO_NONE => 0x40;

		public override uint OP_LOADIMMED_UINT => 0x41;
		public override uint OP_LOADIMMED_FLT => 0x42;
		public override uint OP_TAG_TO_STR => 0x43;
		public override uint OP_LOADIMMED_STR => 0x44;
		public override uint OP_LOADIMMED_IDENT => 0x45;

		public override uint OP_CALLFUNC_RESOLVE => 0x46;
		public override uint OP_CALLFUNC => 0x47;

		public override uint OP_ADVANCE_STR => 0x49;
		public override uint OP_ADVANCE_STR_APPENDCHAR => 0x4A;
		public override uint OP_ADVANCE_STR_COMMA => 0x4B;
		public override uint OP_ADVANCE_STR_NUL => 0x4C;
		public override uint OP_REWIND_STR => 0x4D;
		public override uint OP_TERMINATE_REWIND_STR => 0x4E;
		public override uint OP_COMPARE_STR => 0x4F;

		public override uint OP_PUSH => 0x50;
		public override uint OP_PUSH_FRAME => 0x51;

		public override uint OP_BREAK => 0x52;

		public override uint OP_UNUSED1 => 0x02;
		public override uint OP_UNUSED2 => 0x03;
		public override uint OP_UNUSED3 => 0x48;

		public override uint OP_INVALID => 0x53;
	}
}
