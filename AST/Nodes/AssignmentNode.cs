using DSO.CodeGenerator;
using DSO.Opcodes;

namespace DSO.AST.Nodes
{
	public class AssignmentNode(Node left, Node right, Opcode? op = null) : Node(NodeType.ExpressionStatement)
	{
		public readonly Node Left = left;
		public readonly Node Right = right is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? right : right;
		public readonly Opcode? Operator = op;

		public override int Precedence => IsIncrementDecrement ? 1 : 14;

		public bool IsIncrementDecrement => Right is ConstantDoubleNode constant && constant.Value == 1.0f
			&& (Operator?.Tag == OpcodeTag.OP_ADD || Operator?.Tag == OpcodeTag.OP_SUB);

		public override bool Equals(object? obj) => base.Equals(obj) && obj is AssignmentNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && Equals(node.Operator, Operator);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ (Operator?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, isExpression: true);

			if (Operator == null)
			{
				stream.Write(" ", "=", " ");
				stream.Write(Right, isExpression: true);
			}
			else
			{
				var tag = Operator.Tag;
				var incDec = IsIncrementDecrement;

				if (incDec)
				{
					if (tag == OpcodeTag.OP_ADD)
					{
						stream.Write("++");
					}
					else if (tag == OpcodeTag.OP_SUB)
					{
						stream.Write("--");
					}
				}

				if (!incDec)
				{
					stream.Write(" ", $"{tag switch
					{
						OpcodeTag.OP_ADD => "+",
						OpcodeTag.OP_SUB => "-",
						OpcodeTag.OP_MUL => "*",
						OpcodeTag.OP_DIV => "/",
						OpcodeTag.OP_MOD => "%",
						OpcodeTag.OP_BITOR => "|",
						OpcodeTag.OP_BITAND => "&",
						OpcodeTag.OP_XOR => "^",
						OpcodeTag.OP_SHL => "<<",
						OpcodeTag.OP_SHR => ">>",
					}}=", " ");

					stream.Write(Right, isExpression: true);
				}
			}

			if (!isExpression)
			{
				stream.Write(";", "\n");
			}
		}
	}
}
