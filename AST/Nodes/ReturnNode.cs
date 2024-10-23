/**
 * ReturnNode.cs
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
	public class ReturnNode(Node? value = null) : Node(NodeType.Statement)
	{
		public readonly Node? Value = value is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? value : value;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ReturnNode node && Equals(node.Value, Value);
		public override int GetHashCode() => base.GetHashCode() ^ (Value?.GetHashCode() ?? 0);

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write("return");

			if (Value != null)
			{
				writer.Write(" ");
				writer.Write(Value, isExpression: true);
			}

			writer.Write(";", "\n");
		}
	}
}
