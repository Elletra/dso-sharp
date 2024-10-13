using DSO.AST.Nodes;

namespace DSO.CodeGenerator
{
    public class CodeGeneratorException : Exception
	{
		public CodeGeneratorException() { }
		public CodeGeneratorException(string message) : base(message) { }
		public CodeGeneratorException(string message, Exception inner) : base(message, inner) { }
	}

	public class CodeGenerator
	{
		private TokenStream _stream = new();

		public List<string> Generate(List<Node> nodes)
		{
			_stream = new();

			foreach (var node in nodes)
			{
				node.Visit(_stream, isExpression: false);
			}

			return _stream.Stream;
		}
	}
}
