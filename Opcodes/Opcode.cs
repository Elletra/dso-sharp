/**
 * Opcodes.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace DSO.Opcodes
{
	public class OpcodeException : Exception
	{
		public OpcodeException() { }
		public OpcodeException(string message) : base(message) { }
		public OpcodeException(string message, Exception inner) : base(message, inner) { }
	}

	public class OpcodeData
	{
		public OpcodeTag Tag { get; set; } = OpcodeTag.OP_INVALID;
		public ReturnValue ReturnValue { get; set; }
		public TypeReq TypeReq { get; set; }

		public override bool Equals(object? obj) => obj is OpcodeData data
			&& Equals(data.Tag, Tag)
			&& Equals(data.ReturnValue, ReturnValue)
			&& Equals(data.TypeReq, TypeReq);

		public override int GetHashCode() => ReturnValue.GetHashCode() ^ TypeReq.GetHashCode() ^ Tag.GetHashCode();
	}


	public class Opcode(uint value, OpcodeData data)
	{
		static public Opcode? Create(uint value, Ops ops) => !ops.IsValid(value) ? null : new(value, new()
		{
			Tag = ops.GetOpcodeTag(value),
			ReturnValue = ops.GetReturnValue(value),
			TypeReq = ops.GetTypeReq(value),
		});

		public readonly uint Value = value;
		private readonly OpcodeData Data = data;

		public OpcodeTag Tag => Data.Tag;
		public ReturnValue ReturnValue => Data.ReturnValue;
		public TypeReq TypeReq => Data.TypeReq;

		public override bool Equals(object? obj) => obj is Opcode opcode && Equals(opcode.Value, Value) && opcode.Data.Equals(Data);
		public override int GetHashCode() => Value.GetHashCode() ^ Data.GetHashCode();
	}
}
