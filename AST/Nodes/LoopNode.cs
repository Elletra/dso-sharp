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

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write("do", "\n", "{", "\n");

			Body.ForEach(node => writer.Write(node, isExpression: false));

			writer.Write("}", "\n", "while", " ", "(");
			writer.Write(Test, isExpression: true);
			writer.Write(")", "\n");
		}
	}

	public class WhileLoopNode(Node test) : LoopNode(test)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is WhileLoopNode;
		public override int GetHashCode() => base.GetHashCode() ^ 41;

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write("while", " ", "(");
			writer.Write(Test, isExpression: true);
			writer.Write(")", "\n", "{", "\n");

			Body.ForEach(node => writer.Write(node, isExpression: false));

			writer.Write("}", "\n");
		}
	}

	public class ForLoopNode(Node init, Node test, Node end) : LoopNode(test)
	{
		public readonly Node Init = init;
		public readonly Node End = end;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ForLoopNode node
			&& node.Init.Equals(Init) && node.End.Equals(End);

		public override int GetHashCode() => base.GetHashCode() ^ Init.GetHashCode() ^ End.GetHashCode();

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write("for", " ", "(");
			writer.Write(Init, isExpression: true);
			writer.Write(";", " ");
			writer.Write(Test, isExpression: true);
			writer.Write(";", " ");
			writer.Write(End, isExpression: true);
			writer.Write(")", "\n", "{", "\n");

			Body.ForEach(node => writer.Write(node, isExpression: false));

			writer.Write("}", "\n");
		}
	}
}
