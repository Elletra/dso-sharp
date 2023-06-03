using System;
using System.Collections.Generic;

using DSODecompiler.Loader;

namespace DSODecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader();
			var fileData = loader.LoadFile("init.cs.dso", 210);
		}
	}
}
