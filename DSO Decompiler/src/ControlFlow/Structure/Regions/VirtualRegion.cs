using System.Collections.Generic;

using DSODecompiler.Disassembler;

namespace DSODecompiler.ControlFlow.Structure.Regions
{
	public abstract class VirtualRegion
	{
		public List<Instruction> Instructions { get; } = new();

		public void CopyInstructions (Region region) => region.Instructions.ForEach(instruction => Instructions.Add(instruction));
		public void CopyInstructions (VirtualRegion vr) => vr.Instructions.ForEach(instruction => Instructions.Add(instruction));
	}

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

		public List<VirtualRegion> Body { get; } = new();

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

	public class GotoRegion : VirtualRegion
	{
		public uint TargetAddr { get; }

		public GotoRegion (uint target) => TargetAddr = target;
	}

	public class BreakRegion : VirtualRegion {}
	public class ContinueRegion : VirtualRegion {}

	public class ConditionalRegion : VirtualRegion
	{
		public SequenceRegion Then { get; } = null;
		public SequenceRegion Else { get; } = null;

		public ConditionalRegion (VirtualRegion then, VirtualRegion @else = null)
		{
			Then = SequenceRegion.Get(then);
			Else = SequenceRegion.Get(@else);
		}

		public ConditionalRegion (Region region, VirtualRegion then, VirtualRegion @else = null) : this(then, @else)
		{
			CopyInstructions(region);
		}
	}

	public class LoopRegion : VirtualRegion
	{
		public bool Infinite { get; set; }
		public SequenceRegion Body { get; } = new();

		public LoopRegion () {}

		public LoopRegion (VirtualRegion body, bool infinite)
		{
			Body = SequenceRegion.Get(body);
			Infinite = infinite;
		}

		public LoopRegion (Region region, bool infinite)
		{
			Body.CopyInstructions(region);
			Infinite = infinite;
		}
	}
}
