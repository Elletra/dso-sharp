/**
 * FunctionDeclarationNode.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.CodeGenerator;
using DSO.Disassembler;

namespace DSO.AST.Nodes
{
	public class FunctionDeclarationNode(FunctionInstruction instruction) : Node(NodeType.Statement)
	{
		public readonly string Name = instruction.Name;
		public readonly string? Namespace = instruction.Namespace;
		public readonly List<string> Arguments = [..instruction.Arguments];
		public List<Node> Body { get; set; } = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is FunctionDeclarationNode node
			&& node.Name.Equals(Name) && Equals(node.Namespace, Namespace)
			&& node.Arguments.SequenceEqual(Arguments) && node.Body.SequenceEqual(Body);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Namespace?.GetHashCode() ?? 0)
			^ Arguments.GetHashCode() ^ Body.GetHashCode();

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write("function", " ");

			if (Namespace != null)
			{
				writer.Write(Namespace, "::");
			}

			writer.Write(Name, "(");

			for (var i = 0; i < Arguments.Count; i++)
			{
				var arg = Arguments[i];

				// TODO: There's an edge case where someone actually names their variable or argument `%__unused`, but I don't
				// feel like addressing it right now.
				writer.Write(arg == null || arg == "" ? "%__unused" : arg);

				if (i < Arguments.Count - 1)
				{
					writer.Write(",", " ");
				}
			}

			writer.Write(")", "\n", "{", "\n");
			Body.ForEach(node => writer.Write(node, isExpression: false));
			writer.Write("}", "\n", "\n");
		}
	}

	public class PackageNode(string name) : Node(NodeType.Statement)
	{
		public readonly string Name = name;
		public readonly List<FunctionDeclarationNode> Functions = [];

		public override bool Equals(object? obj) => base.Equals(obj) && obj is PackageNode node
			&& node.Name.Equals(Name) && node.Functions.SequenceEqual(Functions);

		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ Functions.GetHashCode();

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write("package", " ", Name, "\n", "{", "\n");
			Functions.ForEach(function => writer.Write(function, isExpression: false));
			writer.Write("}", ";", "\n");
		}
	}
}
