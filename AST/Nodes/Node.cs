using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public enum NodeType
	{
		Statement,
		Expression,
		ExpressionStatement,
		CommaConcat, // Special case because the CommaConcatNode is only viable under one circumstance.
	}

	public abstract class Node(NodeType type)
	{
		public NodeType Type { get; protected set; } = type;

		public virtual int Precedence => -1;

		public bool IsExpression => Type == NodeType.Expression || Type == NodeType.ExpressionStatement;
		public bool IsExpressionOnly => Type == NodeType.Expression;
		public bool IsStatement => Type == NodeType.Statement || Type == NodeType.ExpressionStatement;
		public bool IsStatementOnly => Type == NodeType.Statement;

		protected bool CheckPrecedenceAndAssociativity(Node node)
		{
			if (this is BinaryNode binary1 && node is BinaryNode binary2)
			{
				return !binary1.Op.Equals(binary2.Op) || !binary1.IsOpAssociative;
			}

			if (this is ConcatNode && node is ConcatNode)
			{
				return false;
			}

			return node.Precedence >= Precedence || (node is AssignmentNode assign && !assign.IsIncrementDecrement);
		}

		public override bool Equals(object? obj) => obj is Node node && node.Type.Equals(Type);
		public override int GetHashCode() => Type.GetHashCode();
		public virtual void Visit(TokenStream stream, bool isExpression) { }
	}
}
