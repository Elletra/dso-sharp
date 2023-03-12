using System;

using DSODecompiler.Disassembler;
using DSODecompiler.Loader;

namespace DSODecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader();
			var fileData = loader.LoadFile("test.cs.dso", 210);
		}
	}
}
