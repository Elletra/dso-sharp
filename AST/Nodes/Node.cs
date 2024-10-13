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

		public bool IsExpression => Type == NodeType.Expression || Type == NodeType.ExpressionStatement;
		public bool IsExpressionOnly => Type == NodeType.Expression;
		public bool IsStatement => Type == NodeType.Statement;

		public override bool Equals(object? obj) => obj is Node node && node.Type.Equals(Type);
		public override int GetHashCode() => Type.GetHashCode();

		public virtual void Visit(TokenStream stream, bool isExpression) { }
		public virtual bool ShouldAddParentheses(Node parent) => false;
	}
}
