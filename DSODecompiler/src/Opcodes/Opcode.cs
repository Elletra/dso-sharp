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

		/// <summary>
		/// The reason this exists at all is for extensibility. Values in switch cases <em>must</em>
		/// be constant, and we don't want a giant if-else chain, so we use strings.<br/><br/>
		///
		/// This lets other classes extend this one and add more opcode values if they wish.
		/// </summary>
		public string StringValue { get; }
		public ReturnValue ReturnValue { get; }
		public TypeReq TypeReq { get; }

		public bool HasValue => _value.HasValue;
		public bool IsValid => HasValue && StringValue != null;

		public Opcode (uint? value, string stringValue, ReturnValue returnValue, TypeReq typeReq)
		{
			_value = value;
			StringValue = stringValue;
			ReturnValue = returnValue;
			TypeReq = typeReq;
		}
	}
}
