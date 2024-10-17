/**
 * BreakContinueNode.cs
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
	public class BreakNode() : Node(NodeType.Statement)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is BreakNode;
		public override int GetHashCode() => base.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression) => stream.Write("break", ";", "\n");
	}

	public class ContinueNode() : Node(NodeType.Statement)
	{
		public override bool Equals(object? obj) => base.Equals(obj) && obj is ContinueNode;
		public override int GetHashCode() => base.GetHashCode();

		public override void Visit(TokenStream stream, bool isExpression) => stream.Write("continue", ";", "\n");
	}
}
