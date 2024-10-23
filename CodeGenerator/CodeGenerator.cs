/**
 * CodeGenerator.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

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
		private CodeWriter _writer = new();

		public List<string> Generate(List<Node> nodes)
		{
			_writer = new();

			foreach (var node in nodes)
			{
				node.Visit(_writer, isExpression: false);
			}

			return _writer.Stream;
		}
	}
}
