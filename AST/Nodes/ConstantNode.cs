using DSO.CodeGenerator;
using DSO.Disassembler;

namespace DSO.AST.Nodes
{
	public enum StringType
	{
		None,
		Identifier,
		String,
		Tagged,
	}

	public class ConstantNode<T> : Node
	{
		public readonly T Value;
		public readonly StringType StringType;

		public ConstantNode(T value) : base(NodeType.Expression)
		{
			Value = value;

			if (typeof(T) != typeof(string))
			{
				StringType = StringType.None;
			}
			else
			{
				var str = value.ToString();

				if (str.StartsWith('"'))
				{
					StringType = StringType.String;
				}
				else if (str.StartsWith('\''))
				{
					StringType = StringType.Tagged;
				}
				else
				{
					StringType = StringType.Identifier;
				}
			}
		}

		public ConstantNode(ImmediateInstruction<T> instruction) : base(NodeType.Expression)
		{
			Value = instruction.Value;

			if (instruction.IsIdentifier)
			{
				StringType = StringType.Identifier;
			}
			else if (instruction.IsTaggedString)
			{
				StringType = StringType.Tagged;
			}
			else if (typeof(T) == typeof(string))
			{
				StringType = StringType.String;
			}
			else
			{
				StringType = StringType.None;
			}
		}

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ConstantNode<T> node
			&& Equals(node.Value, Value) && node.StringType.Equals(StringType);

		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0) ^ StringType.GetHashCode();

		/// <summary>
		/// For escaping special characters.
		/// </summary>
		private readonly Dictionary<string, string> _escapeCharMap = new()
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

		public override void Visit(TokenStream stream, bool isExpression)
		{
			if (Value == null)
			{
				return;
			}

			var str = Value.ToString();

			if (typeof(T) == typeof(string))
			{
				str = Value.ToString();

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
			}

			stream.Write(str);
		}
	}
}
