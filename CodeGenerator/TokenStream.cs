using DSO.AST.Nodes;

namespace DSO.CodeGenerator
{
	public class TokenStream
	{
		public readonly List<string> Stream = [];

		public void Write(params string[] tokens) => Stream.AddRange(tokens);
		public void Write(Node node, bool isExpression) => node.Visit(this, isExpression);

		public void Write(Node node, Node parent)
		{
			var addParentheses = node.ShouldAddParentheses(parent);

			if (addParentheses)
			{
				Write("(");
			}

			node.Visit(this, isExpression: true);

			if (addParentheses)
			{
				Write(")");
			}
		}
	}
}
