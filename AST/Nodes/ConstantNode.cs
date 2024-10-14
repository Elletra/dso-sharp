using DSO.CodeGenerator;
using DSO.Disassembler;

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
		public override void Visit(TokenStream stream, bool isExpression) => stream.Write(Value.ToString());
	}

	public class ConstantUIntNode(uint value) : ConstantNode<uint>(value)
	{
		public ConstantUIntNode(ImmediateInstruction<uint> instruction) : this(instruction.Value) { }
	}

	public class ConstantDoubleNode(double value) : ConstantNode<double>(value)
	{
		public ConstantDoubleNode(ImmediateInstruction<double> instruction) : this(instruction.Value) { }
	}

	public class ConstantStringNode : ConstantNode<string>
	{
		/// <summary>
		/// For escaping special characters.
		/// </summary>
		static private readonly Dictionary<string, string> _escapeCharMap = new()
		{
			{ "\\", "\\\\" },
			{ "\x00", "\\x00" },
			{ "\x01", "\\c0" },
			{ "\x02", "\\c1" },
			{ "\x03", "\\c2" },
			{ "\x04", "\\c3" },
			{ "\x05", "\\c4" },
			{ "\x06", "\\c5" },
			{ "\x07", "\\c6" },
			{ "\x08", "\\x08" },
			{ "\t", "\\t" },
			{ "\n", "\\n" },
			{ "\x0B", "\\c7" },
			{ "\x0C", "\\c8" },
			{ "\r", "\\r" },
			{ "\x0E", "\\c9" },
			{ "\x0F", "\\cr" },
			{ "\x10", "\\cp" },
			{ "\x11", "\\co" },
			{ "\x12", "\\x12" },
			{ "\x13", "\\x13" },
			{ "\x14", "\\x14" },
			{ "\x15", "\\x15" },
			{ "\x16", "\\x16" },
			{ "\x17", "\\x17" },
			{ "\x18", "\\x18" },
			{ "\x19", "\\x19" },
			{ "\x1A", "\\x1A" },
			{ "\x1B", "\\x1B" },
			{ "\x1C", "\\x1C" },
			{ "\x1D", "\\x1D" },
			{ "\x1E", "\\x1E" },
			{ "\x1F", "\\x1F" },
			{ "\x7F", "\\x7F" },
			{ "\xA0", "\\xA0" },
		};

		public readonly StringType StringType;

		public ConstantStringNode(ImmediateInstruction<string> instruction) : base(instruction)
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

		public override void Visit(TokenStream stream, bool isExpression)
		{
			var str = Value;

			// Since tagged strings start with the 0x01 character, and \c0 maps to 0x01, any string
			// that starts with \c0 automatically gets a 0x02 character prepended to avoid errors.
			if (str.StartsWith("\x02\x01"))
			{
				str = str[1..];
			}

			foreach (var (find, replace) in _escapeCharMap)
			{
				str = str.Replace(find, replace);
			}

			if (StringType == StringType.String)
			{
				str = $"\"{str.Replace("\"", "\\\"")}\"";
			}
			else if (StringType == StringType.Tagged)
			{
				str = $"'{str.Replace("'", "\\\'")}'";
			}

			stream.Write(str);
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
