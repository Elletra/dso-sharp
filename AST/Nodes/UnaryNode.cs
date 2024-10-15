using DSO.CodeGenerator;
using DSO.Opcodes;

namespace DSO.AST.Nodes
{
	public class UnaryNode(Node node, Opcode op) : Node(NodeType.Expression)
	{
		public readonly Node Node = node;
		public readonly Opcode Op = op;

		public override int Precedence => 1;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is UnaryNode node && node.Node.Equals(Node) && node.Op.Equals(Op);
		public override int GetHashCode() => base.GetHashCode() ^ Node.GetHashCode() ^ Op.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write(Op.Tag switch
			{
				OpcodeTag.OP_NEG => "-",
				OpcodeTag.OP_ONESCOMPLEMENT => "~",
				OpcodeTag.OP_NOT or OpcodeTag.OP_NOTF => "!",
			});

			stream.Write(Node, node => node.Precedence > Precedence);
		}
	}
}
