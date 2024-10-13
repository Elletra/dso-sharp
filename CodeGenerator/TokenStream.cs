namespace DSO.CodeGenerator
{
	public class TokenStream
	{
		public readonly List<string> Stream = [];

		public void Write(params string[] tokens) => Stream.AddRange(tokens);
	}
}
