using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow.Structure.Regions
{
	public abstract class VirtualRegion
	{
		public List<Instruction> Instructions { get; } = new();

		public void CopyInstructions (Region region) => region.Instructions.ForEach(Instructions.Add);
		public void CopyInstructions (VirtualRegion vr) => vr.Instructions.ForEach(Instructions.Add);
	}

	public class RegionContainer : List<VirtualRegion>
	{
		public new void Add (VirtualRegion virtualRegion)
		{
			/* We extract the instructions and body of `SequenceRegion`s to reduce nesting, as well
			   as to make sure that loop end blocks stay within the main loop body. */
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
		public uint Addr { get; }
		public bool Infinite { get; set; }
		public RegionContainer Body { get; } = new();

		public LoopRegion (uint addr) => Addr = addr;

		public LoopRegion (uint addr, VirtualRegion body, bool infinite) : this(addr)
		{
			Body.Add(body);

			Infinite = infinite;
		}
	}

	public class GotoRegion : VirtualRegion
	{
		public uint TargetAddr { get; }

		public GotoRegion (uint target) => TargetAddr = target;
	}

	public class BreakRegion : VirtualRegion {}
	public class ContinueRegion : VirtualRegion {}
}
