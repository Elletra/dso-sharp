using System;
using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow.Structure.Regions
{
	public abstract class VirtualRegion
	{
		public List<Instruction> Instructions { get; } = new();

		public Instruction FirstInstruction => Instructions.Count > 0 ? Instructions[0] : null;

		public void CopyInstructions (Region region) => region.Instructions.ForEach(Instructions.Add);
		public void CopyInstructions (VirtualRegion vr) => vr.Instructions.ForEach(Instructions.Add);
		public void CopyInstructions (List<Instruction> instructions) => instructions.ForEach(Instructions.Add);
	}

	public class RegionContainer : List<VirtualRegion>
	{
		public new void Add (VirtualRegion virtualRegion)
		{
			/// We extract the instructions and body of <see cref="SequenceRegion"/> to reduce nesting,
			/// as well as to make sure that loop end blocks stay within the main loop body.
			if (virtualRegion is SequenceRegion sequence)
			{
				if (virtualRegion.Instructions.Count > 0)
				{
					base.Add(new InstructionRegion(virtualRegion));
				}

				sequence.Body.ForEach(base.Add);
			}
			else
			{
				base.Add(virtualRegion);
			}
		}
	}

	/// <summary>
	/// A simple virtual region that only contains instructions (meant to differentiate from VirtualRegion base class).
	/// </summary>
	public class InstructionRegion : VirtualRegion
	{
		public InstructionRegion () {}
		public InstructionRegion (Region region) => CopyInstructions(region);
		public InstructionRegion (VirtualRegion vr) => CopyInstructions(vr);
	}

	/// <summary>
	/// A simple virtual region to differentiate the endings of loops.
	/// </summary>
	public class LoopFooterRegion : VirtualRegion
	{
		public LoopFooterRegion () {}
		public LoopFooterRegion (Region region) => CopyInstructions(region);
		public LoopFooterRegion (VirtualRegion vr) => CopyInstructions(vr);
	}

	/// <summary>
	/// A virtual region that can contain other virtual regions.
	/// </summary>
	public class SequenceRegion : VirtualRegion
	{
		public static SequenceRegion Get (VirtualRegion vr)
		{
			if (vr == null || vr is SequenceRegion)
			{
				return vr as SequenceRegion;
			}

			return new SequenceRegion() { Body = { vr } };
		}

		public RegionContainer Body { get; } = new();

		public SequenceRegion () {}
		public SequenceRegion (Region region) => CopyInstructions(region);
		public SequenceRegion (params VirtualRegion[] regions) => Add(regions);

		public void Add (params VirtualRegion[] regions)
		{
			for (var i = 0; i < regions.Length; i++)
			{
				Add(regions[i]);
			}
		}

		public void Add (VirtualRegion region) => Body.Add(region);
	}

	public class FunctionRegion : SequenceRegion
	{
		public FunctionInstruction Header => FirstInstruction as FunctionInstruction;

		public FunctionRegion (FunctionInstruction function) => Instructions.Add(function);
	}

	/// <summary>
	/// A virtual region meant to represent a conditional (if-then or if-then-else).
	/// </summary>
	public class ConditionalRegion : VirtualRegion
	{
		public RegionContainer Then { get; } = new();
		public RegionContainer Else { get; } = new();

		public ConditionalRegion (VirtualRegion then, VirtualRegion @else = null)
		{
			Then.Add(then);

			if (@else != null)
			{
				Else.Add(@else);
			}
		}

		public ConditionalRegion (Region region, VirtualRegion then, VirtualRegion @else = null) : this(then, @else)
		{
			CopyInstructions(region);
		}
	}

	/// <summary>
	/// A virtual region meant to represent a loop.
	/// </summary>
	public class LoopRegion : VirtualRegion
	{
		public bool Infinite { get; set; }
		public RegionContainer Body { get; } = new();

		public LoopRegion () {}

		public LoopRegion (VirtualRegion body, bool infinite)
		{
			Body.Add(body);

			Infinite = infinite;
		}
	}

	public class GotoRegion : VirtualRegion
	{
		public uint TargetAddr { get; }

		/// <summary>
		/// There may be some cases where a branch instruction uses the OP_JMPIF(F)/OP_JMPIF(F)NOT
		/// instructions but does not match any of the schemas (i.e. it is used basically as a goto
		/// and does not match the loop/if-else schemas).<br/><br/>
		///
		/// This is purely hypothetical and I have no idea if this is even possible.
		/// </summary>
		public bool IsConditional => Instructions.Count > 0
			&& Instructions[^1] is BranchInstruction branch
			&& branch.IsConditional;

		public GotoRegion (List<Instruction> instructions)
		{
			if (instructions.Count <= 0)
			{
				throw new Exception($"Attempted to create goto region with no instructions");
			}

			if (instructions[^1] is not BranchInstruction branch)
			{
				throw new Exception("$Attempted to create goto region with non-branch as last instruction");
			}

			TargetAddr = branch.Addr;

			CopyInstructions(instructions);
		}
	}

	public class BreakRegion : VirtualRegion {}
	public class ContinueRegion : VirtualRegion {}
}
