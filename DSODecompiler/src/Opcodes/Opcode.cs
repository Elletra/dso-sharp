using System.Collections.Generic;
using System.Reflection.Emit;

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

	public class Opcode
	{
		private readonly uint? _value = null;
		public uint Value { get => _value ?? 0; }

		public string StringValue { get; }
		public ReturnValue ReturnValue { get; }
		public TypeReq TypeReq { get; }

		public bool HasValue => _value.HasValue;
		public bool IsValid => HasValue && StringValue != null && StringValue != "OP_INVALID";

		public Opcode (uint? value, string stringValue, ReturnValue returnValue, TypeReq typeReq)
		{
			_value = value;
			StringValue = stringValue;
			ReturnValue = returnValue;
			TypeReq = typeReq;
		}
	}
}
