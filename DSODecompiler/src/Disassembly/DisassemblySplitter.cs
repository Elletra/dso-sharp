using System.Collections.Generic;

namespace DSODecompiler.Disassembly
{
	public class InstructionBlock : List<Instruction>
	{
		public Instruction First => this?[0];
		public Instruction Last => this?[^1];

		public bool IsFunction => First is FunctionInstruction;
	}

	/// <summary>
	/// Splits the disassembly into blocks, separated by functions.
	/// </summary>
	public class DisassemblySplitter
	{
		protected List<InstructionBlock> blocks;
		protected InstructionBlock currBlock;

		public List<InstructionBlock> Split (Disassembly disassembly)
		{
			blocks = new();
			currBlock = null;

			Split(disassembly.GetInstructions());

			return blocks;
		}

		protected void Split (List<Instruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (ShouldCreateBlock(instruction))
				{
					blocks.Add(currBlock = new());
				}

				currBlock.Add(instruction);
			}
		}

		/// <summary>
		/// Determines whether we should create a new instruction block based on three rules:<br/><br/>
		///
		/// <list type="number">
		/// <item>We're at the start of the code.</item>
		/// <item>We're at the beginning of a function.</item>
		/// <item>We're at the end of a function.</item>
		/// </list>
		/// </summary>
		/// <param name="instruction"></param>
		/// <returns></returns>
		protected bool ShouldCreateBlock (Instruction instruction)
		{
			return currBlock == null
				|| instruction is FunctionInstruction
				|| (currBlock.IsFunction && instruction.Addr >= (currBlock.First as FunctionInstruction).EndAddr);
		}
	}
}
