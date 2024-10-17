/**
 * LoopNode.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.CodeGenerator;

namespace DSO.AST.Nodes
{
	public class LoopNode(Node test) : Node(NodeType.Statement)
	{
		public readonly Node Test = test;
		public List<Node> Body { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is LoopNode node && node.Test.Equals(Test) && node.Body.SequenceEqual(Body);
		public override int GetHashCode() => base.GetHashCode() ^ Test.GetHashCode() ^ Body.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write("do", "\n", "{", "\n");

			Body.ForEach(node => stream.Write(node, isExpression: false));

			stream.Write("}", "\n", "while", " ", "(");
			stream.Write(Test, isExpression: true);
			stream.Write(")", "\n");
		}
	}

	public class WhileLoopNode(Node test) : LoopNode(test)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is WhileLoopNode;
		public override int GetHashCode() => base.GetHashCode() ^ 41;

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write("while", " ", "(");
			stream.Write(Test, isExpression: true);
			stream.Write(")", "\n", "{", "\n");

			Body.ForEach(node => stream.Write(node, isExpression: false));

			stream.Write("}", "\n");
		}
	}

	public class ForLoopNode(Node init, Node test, Node end) : LoopNode(test)
	{
		public readonly Node Init = init;
		public readonly Node End = end;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ForLoopNode node
			&& node.Init.Equals(Init) && node.End.Equals(End);

		public override int GetHashCode() => base.GetHashCode() ^ Init.GetHashCode() ^ End.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression)
		{
			stream.Write("for", " ", "(");
			stream.Write(Init, isExpression: true);
			stream.Write(";", " ");
			stream.Write(Test, isExpression: true);
			stream.Write(";", " ");
			stream.Write(End, isExpression: true);
			stream.Write(")", "\n", "{", "\n");

			Body.ForEach(node => stream.Write(node, isExpression: false));

			stream.Write("}", "\n");
		}
	}
}
