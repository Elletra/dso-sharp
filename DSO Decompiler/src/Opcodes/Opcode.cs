using System;

namespace DSODecompiler.Opcodes
{
	public class Opcode
	{
		public class Data
		{
			public static explicit operator Data (Ops.Value op)
			{
				return new Data()
				{
					BranchType = Ops.GetBranchType(op),
					ConvertToType = Ops.GetConvertToType(op),
					AdvanceStringType = Ops.GetAdvanceStringType(op),
					ReturnValue = Ops.GetReturnValueChange(op),
					Type = Ops.GetOpcodeType(op),
				};
			}

			public BranchType BranchType { get; set; } = BranchType.Invalid;
			public ConvertToType ConvertToType { get; set; } = ConvertToType.Invalid;
			public AdvanceStringType AdvanceStringType { get; set; } = AdvanceStringType.Invalid;
			public ReturnValue ReturnValue { get; set; } = ReturnValue.NoChange;
			public OpcodeType Type { get; set; } = OpcodeType.Invalid;
		}

		public static explicit operator Opcode (uint op)
		{
			if (!Ops.IsValid(op))
			{
				throw new InvalidCastException($"Cannot convert uint to Opcode: {op} is not a valid value");
			}

			return new Opcode((Ops.Value) op);
		}

		public Ops.Value Op { get; }
		public BranchType BranchType => data.BranchType;
		public ConvertToType ConvertToType => data.ConvertToType;
		public AdvanceStringType AdvanceStringType => data.AdvanceStringType;
		public ReturnValue ReturnValue => data.ReturnValue;
		public OpcodeType Type => data.Type;

		protected Data data = new();

		public Opcode (Ops.Value op)
		{
			Op = op;
			data = (Data) op;
		}

		public override string ToString () => Ops.IsValid((uint) Op) ? Op.ToString() : "<UNKNOWN>";
	}
}
