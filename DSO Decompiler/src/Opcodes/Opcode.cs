using System;

namespace DSODecompiler.Opcodes
{
	public class Opcode
	{
		public static explicit operator Opcode (uint op)
		{
			if (!Ops.IsValid(op))
			{
				throw new InvalidCastException($"Cannot convert uint to Opcode: {op} is not a valid value");
			}

			return new Opcode((Ops.Value) op);
		}

		public Ops.Value Op { get; }
		public BranchType BranchType { get; }
		public ConvertToType ConvertToType { get; }
		public AdvanceStringType AdvanceStringType { get; }
		public ReturnValue ReturnValue { get; }
		public OpcodeType Type { get; }

		public Opcode (Ops.Value op)
		{
			Op = op;
			BranchType = Ops.GetBranchType(op);
			ConvertToType = Ops.GetConvertToType(op);
			AdvanceStringType = Ops.GetAdvanceStringType(op);
			ReturnValue = Ops.GetReturnValueChange(op);
			Type = Ops.GetOpcodeType(op);
		}

		public override string ToString () => Ops.IsValid((uint) Op) ? Op.ToString() : "<UNKNOWN>";
	}
}
