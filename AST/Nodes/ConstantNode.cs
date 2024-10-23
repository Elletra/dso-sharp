/**
 * ConstantNode.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.CodeGenerator;
using DSO.Disassembler;
using DSO.Loader;

namespace DSO.AST.Nodes
{
	public enum StringType
	{
		Identifier,
		String,
		Tagged,
	}

	public abstract class ConstantNode<T>(T value) : Node(NodeType.Expression)
	{
		public readonly T Value = value;

		public ConstantNode(ImmediateInstruction<T> instruction) : this(instruction.Value) { }

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ConstantNode<T> node && Equals(node.Value, Value);
		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
		public override void Visit(CodeWriter writer, bool isExpression) => writer.Write(Value.ToString());
	}

	public class ConstantUIntNode(uint value) : ConstantNode<uint>(value)
	{
		public ConstantUIntNode(ImmediateInstruction<uint> instruction) : this(instruction.Value) { }
	}

	public class ConstantDoubleNode(double value) : ConstantNode<double>(value)
	{
		public ConstantDoubleNode(ImmediateInstruction<double> instruction) : this(instruction.Value) { }
	}

	public class ConstantStringNode : ConstantNode<StringTableEntry>
	{
		public readonly StringType StringType;

		public ConstantStringNode(ImmediateStringInstruction instruction) : base(instruction)
		{
			if (instruction.IsIdentifier)
			{
				StringType = StringType.Identifier;
			}
			else if (instruction.IsTaggedString)
			{
				StringType = StringType.Tagged;
			}
			else
			{
				StringType = StringType.String;
			}
		}

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ConstantStringNode node && node.StringType.Equals(StringType);
		public override int GetHashCode() => base.GetHashCode() ^ StringType.GetHashCode();

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			var str = Util.String.EscapeString(Value);

			if (StringType == StringType.String)
			{
				str = $"\"{str.Replace("\"", "\\\"")}\"";
			}
			else if (StringType == StringType.Tagged)
			{
				str = $"'{str.Replace("'", "\\\'")}'";
			}

			writer.Write(str);
		}

		public ConstantUIntNode? ConvertToUIntNode()
		{
			if (!uint.TryParse(Value, out uint number))
			{
				return null;
			}

			return new(number);
		}

		public ConstantDoubleNode? ConvertToDoubleNode()
		{
			if (!double.TryParse(Value, out double number))
			{
				return null;
			}

			return new(number);
		}
	}
}
