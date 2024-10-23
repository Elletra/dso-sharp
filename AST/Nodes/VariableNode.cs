/**
 * VariableNode.cs
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
	public class VariableNode(string name, Node? index = null) : Node(NodeType.Expression)
	{
		public readonly string Name = name;
		public readonly Node? Index = index is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? index : index;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is VariableNode node && node.Name.Equals(Name) && Equals(node.Index, Index);
		public override int GetHashCode() => base.GetHashCode() ^ Name.GetHashCode() ^ (Index?.GetHashCode() ?? 0);

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write(Name);

			if (Index != null)
			{
				writer.Write("[");
				writer.Write(Index, isExpression: true);
				writer.Write("]");
			}
		}
	}
}
