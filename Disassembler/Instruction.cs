/**
 * Instruction.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Loader;
using DSO.Opcodes;

namespace DSO.Disassembler
{
	/**
	 * There's a bunch of empty subclasses of Instruction in order to let AST.Builder use classes
	 * instead of checking opcodes, which is much better.
	 *
	 * I know it's not the most elegant solution, but oh well!
	 */

	/// <summary>
	/// Base instruction class.
	/// </summary>
	public abstract class Instruction(Opcode opcode, uint addr)
	{
		public Opcode Opcode { get; } = opcode;
		public uint Address { get; } = addr;

		public Instruction? Prev { get; set; } = null;
		public Instruction? Next { get; set; } = null;

		public virtual void Visit(DisassemblyWriter writer)
		{
			if (Address >= writer.Function?.EndAddress)
			{
				writer.Function = null;
			}

			writer.WriteValue(Opcode.Tag);
		}
	}

	/// <summary>
	/// Function declaration instruction.
	/// </summary>
	public class FunctionInstruction(Opcode opcode, uint addr, StringTableEntry name, StringTableEntry ns, StringTableEntry package,
		bool hasBody, uint endAddr) : Instruction(opcode, addr)
	{
		public StringTableEntry Name { get; } = name;
		public StringTableEntry? Namespace { get; } = ns;
		public StringTableEntry? Package { get; } = package;
		public bool HasBody { get; } = hasBody;

		/// <summary>
		/// Ignore if <see cref="HasBody"/> is <see langword="false"/>.
		/// </summary>
		public uint EndAddress { get; } = endAddr;

		public List<StringTableEntry> Arguments { get; } = [];

		public override void Visit(DisassemblyWriter writer)
		{
			writer.Write("\n");
			writer.WriteCommentLine("======================== F U N C T I O N ============================================", writeAddress: true);
			writer.WriteLine(Address, "", "", indent: false);
			writer.WriteCommentLine($"Start of `{(Namespace == null ? "" : $"{Namespace}::")}{Name}()`", writeAddress: true, indent: true);
			writer.WriteLine(Address, "");

			base.Visit(writer);

			writer.WriteValue(Name, "name");
			writer.WriteValue(Namespace, "namespace");
			writer.WriteValue(Package, "package");
			writer.WriteValue(HasBody, "has body");
			writer.WriteAddressValue(EndAddress, "end address");
			writer.WriteValue(Arguments.Count, "argument count");

			var count = 1;

			Arguments.ForEach(arg => writer.WriteValue(arg, $"arg {count++}"));

			writer.Function = this;
		}
	}

	/// <summary>
	/// Instruction for the first part of object creation.
	/// </summary>
	public class CreateObjectInstruction(Opcode opcode, uint addr, StringTableEntry parent, bool isDataBlock, uint failJumpAddress) : Instruction(opcode, addr)
	{
		public StringTableEntry? Parent { get; } = parent;
		public bool IsDataBlock { get; } = isDataBlock;
		public uint FailJumpAddress { get; } = failJumpAddress;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Parent, "parent");
			writer.WriteValue(IsDataBlock, "is datablock");
			writer.WriteAddressValue(FailJumpAddress, "fail jump address");
		}
	}

	/// <summary>
	/// Instruction for the second part of object creation.
	/// </summary>
	public class AddObjectInstruction(Opcode opcode, uint addr, bool placeAtRoot) : Instruction(opcode, addr)
	{
		public bool PlaceAtRoot { get; } = placeAtRoot;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(PlaceAtRoot, "place at root");
		}
	}

	/// <summary>
	/// Instruction for the third and final part of object creation.
	/// </summary>
	public class EndObjectInstruction(Opcode opcode, uint addr, bool value) : Instruction(opcode, addr)
	{
		/// <summary>
		/// Can either be for `isDataBlock` or `placeAtRoot`.
		/// </summary>
		public bool Value { get; } = value;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Value, "is datablock or place at root");
		}
	}

	/// <summary>
	/// Branch instruction, for loops, conditionals, breaks, continues, and logical AND/OR.
	/// </summary>
	public class BranchInstruction(Opcode opcode, uint addr, uint targetAddress) : Instruction(opcode, addr)
	{
		public uint TargetAddress { get; } = targetAddress;

		public bool IsLoopEnd => TargetAddress < Address;

		public bool IsUnconditional => Opcode.Tag == OpcodeTag.OP_JMP;
		public bool IsConditional => !IsUnconditional;

		/// <summary>
		/// Checks whether this is a branch for the logical operators || or &&.
		/// </summary>
		public bool IsLogicalOperator => Opcode.Tag == OpcodeTag.OP_JMPIF_NP || Opcode.Tag == OpcodeTag.OP_JMPIFNOT_NP;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteBranchTarget(TargetAddress, "branch target");
		}
	}

	/// <summary>
	/// Return instruction.
	/// </summary>
	public class ReturnInstruction(Opcode opcode, uint addr, bool returnsValue) : Instruction(opcode, addr)
	{
		public bool ReturnsValue { get; } = returnsValue;
	}

	/// <summary>
	/// Opcodes for binary instructions like OP_ADD, OP_XOR, OP_CMPEQ, etc.
	/// </summary>
	public class BinaryInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// A separate instruction for OP_COMPARE_STR because the operands are inverse of the other
	/// binary operations.
	/// </summary>
	public class BinaryStringInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	public class UnaryInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr)
	{
		public bool IsNot => Opcode.Tag == OpcodeTag.OP_NOT || Opcode.Tag == OpcodeTag.OP_NOTF;
	}

	/// <summary>
	/// Variable instruction for regular variables only.<br/><br/>
	///
	/// For "array"-indexing a variable, see <see cref="VariableArrayInstruction"/>.
	/// </summary>
	public class VariableInstruction(Opcode opcode, uint addr, StringTableEntry name) : Instruction(opcode, addr)
	{
		public StringTableEntry Name { get; } = name;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Name, "variable name");
		}
	}

	/// <summary>
	/// "Array"-indexing a variable (e.g. OP_SETCURVAR_ARRAY, OP_SETCURVAR_ARRAY_CREATE, etc.).
	/// </summary>
	public class VariableArrayInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_LOADVAR_*
	/// </summary>
	public class LoadVariableInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_SAVEVAR_*
	/// </summary>
	public class SaveVariableInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_SETCUROBJECT
	/// </summary>
	public class ObjectInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_SETCUROBJECT_NEW
	/// </summary>
	public class ObjectNewInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// Field instruction.
	/// </summary>
	public class FieldInstruction(Opcode opcode, uint addr, StringTableEntry name) : Instruction(opcode, addr)
	{
		public StringTableEntry Name { get; } = name;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Name, "field name");
		}
	}

	/// <summary>
	/// "Array"-indexing a field (e.g. OP_SETCURFIELD_ARRAY).
	/// </summary>
	public class FieldArrayInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_LOADFIELD_*
	/// </summary>
	public class LoadFieldInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_SAVEFIELD_*
	/// </summary>
	public class SaveFieldInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// Type conversion instruction.<br/><br/>
	///
	/// Does not have any operands, but it still has extra information we want to know.
	/// </summary>
	public class ConvertToTypeInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr)
	{
		public TypeReq Type => Opcode.TypeReq;
	}

	/// <summary>
	/// Immediate instruction for integers, floats, strings, tagged strings, and identifiers.<br/><br/>
	///
	/// "Identifier" is a term that Torque uses to refer to strings that aren't surrounded by quotes...
	/// TorqueScript is a very odd language.
	/// </summary>
	public class ImmediateInstruction<T>(Opcode opcode, uint addr, T value) : Instruction(opcode, addr)
	{
		public T Value { get; } = value;

		public bool IsTaggedString => Opcode.Tag == OpcodeTag.OP_TAG_TO_STR;
		public bool IsIdentifier => Opcode.Tag == OpcodeTag.OP_LOADIMMED_IDENT;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Value, "value");
		}
	}

	/// <summary>
	/// Function call instruction.
	/// </summary>
	public class CallInstruction(Opcode opcode, uint addr, StringTableEntry name, StringTableEntry ns, uint callType) : Instruction(opcode, addr)
	{
		public StringTableEntry Name { get; } = name;
		public StringTableEntry? Namespace { get; } = ns;
		public uint CallType { get; } = callType;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Name, "name");
			writer.WriteValue(Namespace, "namespace");
			writer.WriteValue(CallType, "call type");
		}
	}

	/// <summary>
	/// OP_ADVANCE_STR
	/// </summary>
	public class AdvanceStringInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// Advance-string instruction with appended character (used for SPC, TAB, and NL keywords).<br/><br/>
	/// </summary>
	public class AdvanceAppendInstruction(Opcode opcode, uint addr, char ch) : Instruction(opcode, addr)
	{
		public char Char { get; } = ch;

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Char, "char to append");
		}
	}

	/// <summary>
	/// OP_ADVANCE_STR_COMMA
	/// </summary>
	public class AdvanceCommaInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_ADVANCE_STR_NUL
	/// </summary>
	public class AdvanceNullInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_REWIND_STR
	/// </summary>
	public class RewindStringInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_TERMINATE_REWIND_STR
	/// </summary>
	public class TerminateRewindInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_PUSH
	/// </summary>
	public class PushInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_PUSH_FRAME
	/// </summary>
	public class PushFrameInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_BREAK
	/// </summary>
	public class DebugBreakInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }

	/// <summary>
	/// OP_UNUSED#
	/// </summary>
	public class UnusedInstruction(Opcode opcode, uint addr) : Instruction(opcode, addr) { }
}
