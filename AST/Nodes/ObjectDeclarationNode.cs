/**
 * ObjectDeclarationNode.cs
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
	public class ObjectDeclarationNode(CreateObjectInstruction instruction, Node className, Node? objectName, int depth) : Node(NodeType.ExpressionStatement)
	{
		private readonly List<Node> _arguments = [];

		public readonly bool IsDataBlock = instruction.IsDataBlock;
		public readonly bool IsInternal = instruction.IsInternal ?? false;
		public readonly Node Class = className;
		public readonly Node? Name = objectName;
		public readonly string? Parent = instruction.Parent;
		public readonly int Depth = depth;
		public readonly List<AssignmentNode> Fields = [];
		public readonly List<ObjectDeclarationNode> Children = [];

		public void AddArgument(Node arg) => _arguments.Add(arg is ConstantStringNode node ? node.ConvertToUIntNode() ?? node.ConvertToDoubleNode() ?? arg : arg);

		public override bool Equals(object? obj) => base.Equals(obj) && obj is ObjectDeclarationNode node
			&& node.IsDataBlock.Equals(IsDataBlock) && node.Class.Equals(Class) && Equals(node.Name, Name) && Equals(node.Parent, Parent)
			&& node.Depth.Equals(Depth) && node._arguments.SequenceEqual(_arguments) && node.Fields.SequenceEqual(Fields) && node.Children.SequenceEqual(Children);

		public override int GetHashCode() => base.GetHashCode() ^ IsDataBlock.GetHashCode()
			^ Class.GetHashCode() ^ (Name?.GetHashCode() ?? 0) ^ (Parent?.GetHashCode() ?? 0)
			^ Depth.GetHashCode() ^ _arguments.GetHashCode() ^ Fields.GetHashCode() ^ Children.GetHashCode();

		public override void Visit(CodeWriter writer, bool isExpression)
		{
			writer.Write(IsDataBlock ? "datablock" : "new", " ");
			writer.Write(Class, node => node is not ConstantStringNode str || str.StringType != StringType.Identifier);
			writer.Write("(");

			if (IsInternal)
			{
				writer.Write("[");
			}

			if (Name != null && (Name is not ConstantStringNode || (Name is ConstantStringNode constant && constant.Value != "")))
			{
				writer.Write(Name, isExpression: true);
			}

			if (IsInternal)
			{
				writer.Write("]");
			}

			if (Parent != null && Parent != "")
			{
				if (Name != null)
				{
					writer.Write(" ");
				}

				writer.Write(":", " ", Parent);
			}

			if (_arguments.Count > 0)
			{
				writer.Write(",", " ");
			}

			foreach (var arg in _arguments)
			{
				writer.Write(arg, isExpression: true);

				if (arg != _arguments.Last())
				{
					writer.Write(",", " ");
				}
			}

			writer.Write(")");

			if (Fields.Count > 0 || Children.Count > 0)
			{
				writer.Write("\n", "{", "\n");

				Fields.ForEach(field => writer.Write(field, isExpression: false));

				if (Fields.Count > 0 && Children.Count > 0)
				{
					writer.Write("\n");
				}

				Children.ForEach(child => writer.Write(child, isExpression: false));

				writer.Write("}");
			}

			if (!isExpression)
			{
				writer.Write(";", "\n");
			}
		}
	}
}
