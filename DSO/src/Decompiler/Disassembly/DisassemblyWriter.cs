using System.Collections.Generic;
using System.IO;

using DSO.Decompiler.ControlFlow;

namespace DSO.Decompiler.Disassembly
{
	public class DisassemblyWriter
	{
		public void WriteToFile(Disassembly disassembly, string filePath, List<ControlFlowGraph> cfgs = null)
		{
			var cfgIndex = 0;

            using var writer = new StreamWriter(filePath);

			foreach (var instruction in disassembly.GetInstructions())
			{
				if (cfgs != null)
				{
					if (cfgIndex + 1 < cfgs.Count && cfgs[cfgIndex + 1].EntryPoint == instruction.Addr)
					{
						writer.WriteLine($"###### Graph {instruction.Addr}:");
						cfgIndex++;
					}

					if (cfgs[cfgIndex].HasNode(instruction.Addr))
					{
						writer.WriteLine($"    ### Node {instruction.Addr}:");
					}
				}

				writer.WriteLine(string.Format("        {0, -8}  {1}", instruction.Addr, instruction));
				writer.Flush();
			}
		}
	}
}
