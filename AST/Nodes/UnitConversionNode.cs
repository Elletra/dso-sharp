/**
 * UnitConversionNode.cs
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
	public class UnitConversionNode(Node unit, Node value) : Node(NodeType.Expression)
	{
		public readonly Node Unit = unit;
		public readonly Node Value = value;

		public override bool Equals(object? obj) => base.Equals(obj) && obj is UnitConversionNode node
			&& node.Unit.Equals(Unit) && node.Value.Equals(Value);

		public override int GetHashCode() => base.GetHashCode() ^ Unit.GetHashCode() ^ Value.GetHashCode();

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write(Value, isExpression: true);
			writer.Write(Unit, isExpression: true);
		}
	}
}
