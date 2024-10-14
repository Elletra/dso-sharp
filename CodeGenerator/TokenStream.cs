using DSO.AST.Nodes;

namespace DSO.CodeGenerator
{
	public delegate bool ShouldAddParentheses(Node node);

	public class TokenStream
	{
		private string _prevToken = "";
		public readonly List<string> Stream = [];

		public int Indent { get; private set; } = 0;

		public void Write(params string[] tokens)
		{
			foreach (var token in tokens)
			{
				if (token == "}")
				{
					Indent--;
				}

				if (_prevToken == "\n" && token != "\n")
				{
					for (var i = 0; i < Indent; i++)
					{
						Stream.Add("\t");
					}
				}

				if (token == "{")
				{
					Indent++;
				}

				_prevToken = token;

				Stream.Add(token);
			}
		}

		public void Write(Node node, bool isExpression) => node.Visit(this, isExpression);

		public void Write(Node node, ShouldAddParentheses test)
		{
			var addParentheses = test(node);

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
