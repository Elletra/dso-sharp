using System.IO;

namespace DSODecompiler.Disassembly
{
	public class DisassemblyWriter
	{
		public void WriteToFile(Disassembly disassembly, string filePath)
		{
			using (var writer = new StreamWriter(filePath))
			{
				foreach (var instruction in disassembly.GetInstructions())
				{
					writer.WriteLine(string.Format("{0, -8}  {1}", instruction.Addr, instruction));
					writer.Flush();
				}
			}
		}
	}
}
