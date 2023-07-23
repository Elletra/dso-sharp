using DSODecompiler.AST.Nodes;

using System.Collections.Generic;

namespace DSODecompiler.AST
{
	/// <summary>
	/// A <em>little</em> bit of a hack.<br/><br/>
	///
	/// Since control flow graphs are split into functions and non-functions, the ASTs are built
	/// separately for all of them. This class just bundles them all together into one NodeList.
	/// </summary>
	public class Bundler
	{
		private NodeList bundle;

		public NodeList Bundle (List<NodeList> lists)
		{
			bundle = new();

			foreach (var list in lists)
			{
				Bundle(list);
			}

			return bundle;
		}

		protected void Bundle (NodeList list)
		{
			foreach (var node in list)
			{
				Bundle(node);
			}
		}

		protected void Bundle (Node node)
		{
			if (node is FunctionStatementNode function)
			{
				if (bundle.Peek() is PackageNode package && package.Name == function.Package)
				{
					package.Functions.Push(function);
				}
				else if (function.Package != null)
				{
					var newPackage = new PackageNode(function.Package);

					newPackage.Functions.Push(node);

					bundle.Push(newPackage);
				}
				else
				{
					bundle.Push(function);
				}
			}
			else
			{
				bundle.Push(node);
			}
		}
	}
}
