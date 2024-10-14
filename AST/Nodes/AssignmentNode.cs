using DSO.CodeGenerator;
using DSO.Opcodes;

namespace DSO.AST.Nodes
{
	public class AssignmentNode(Node left, Node right, Opcode? op = null) : Node(NodeType.ExpressionStatement)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public readonly Opcode? Operator = op;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is AssignmentNode node
			&& node.Left.Equals(Left) && node.Right.Equals(Right) && Equals(node.Operator, Operator);

		public override int GetHashCode() => base.GetHashCode() ^ Left.GetHashCode() ^ Right.GetHashCode() ^ (Operator?.GetHashCode() ?? 0);

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Left, this);

			if (Operator == null)
			{
				stream.Write(" ", "=", " ");

				Node? right = null;

				if (Right is ConstantStringNode constant)
				{
					right ??= constant.ConvertToDoubleNode();
					right ??= constant.ConvertToUIntNode();
				}

				right ??= Right;

				stream.Write(right, this);
			}
			else
			{
				var op = Operator.Value;
				var incDec = false;

				if (Right is ConstantDoubleNode constant && constant.Value == 1.0f)
				{
					if (op == Ops.OP_ADD)
					{
						incDec = true;
						stream.Write("++");
					}
					else if (op == Ops.OP_SUB)
					{
						incDec = true;
						stream.Write("--");
					}
				}

				if (!incDec)
				{
					stream.Write(" ", $"{op switch
					{
						Ops.OP_ADD => "+",
						Ops.OP_SUB => "-",
						Ops.OP_MUL => "*",
						Ops.OP_DIV => "/",
						Ops.OP_MOD => "%",
						Ops.OP_BITOR => "|",
						Ops.OP_BITAND => "&",
						Ops.OP_XOR => "^",
						Ops.OP_SHL => "<<",
						Ops.OP_SHR => ">>",
					}}=", " ");

					stream.Write(Right, this);
				}
			}

			if (!isExpression)
			{
				stream.Write(";", "\n");
			}
		}

		public override bool ShouldAddParentheses(Node parent) => parent.IsStatement
			&& parent is not LoopNode
			&& parent is not IfNode
			&& parent is not AssignmentNode;
	}
}
