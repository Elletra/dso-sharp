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
	public abstract class Instruction
	{
		public Opcode Opcode { get; }
		public uint Address { get; }

		public Instruction? Prev { get; set; } = null;
		public Instruction? Next { get; set; } = null;

		public Instruction(Opcode opcode, uint address, BytecodeReader reader)
		{
			Opcode = opcode;
			Address = address;

			if (Opcode.ReturnValue != ReturnValue.NoChange)
			{
				reader.ReturnableValue = Opcode.ReturnValue == ReturnValue.ToTrue;
			}

			if (reader.InFunction && address >= reader.Function?.EndAddress)
			{
				reader.Function = null;
			}
		}

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
	public class FunctionInstruction : Instruction
	{
		public StringTableEntry Name { get; }
		public StringTableEntry? Namespace { get; }
		public StringTableEntry? Package { get; }
		public bool HasBody { get; }

		/// <summary>
		/// Ignore if <see cref="HasBody"/> is <see langword="false"/>.
		/// </summary>
		public uint EndAddress { get; }

		public List<StringTableEntry> Arguments { get; } = [];

		public FunctionInstruction(Opcode opcode, uint address, BytecodeReader reader) : base(opcode, address, reader)
		{
			Name = reader.ReadIdentifier();
			Namespace = reader.ReadIdentifier();
			Package = reader.ReadIdentifier();
			HasBody = reader.ReadBool();
			EndAddress = reader.ReadUInt();

			var args = reader.ReadUInt();

			for (uint i = 0; i < args; i++)
			{
				Arguments.Add(reader.ReadIdentifier());
			}

			reader.Function = this;
		}

		public override void Visit(DisassemblyWriter writer)
		{
			writer.Write("\n");
			writer.WriteCommentLine("======================== F U N C T I O N ============================================", writeAddress: true);
			writer.WriteLine(Address, "", "", indent: false);

			var startComment = "Start of `";

			if (Namespace != null)
			{
				startComment += $"{Namespace}::";
			}

			startComment += $"{Name}()`";

			if (Package != null)
			{
				startComment += $" (package: `{Package}`)";
			}

			writer.WriteCommentLine(startComment, writeAddress: true, indent: true);
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
	public class CreateObjectInstruction : Instruction
	{
		public StringTableEntry? Parent { get; protected set; } = null;
		public bool IsDataBlock { get; protected set; } = false;
		public bool? IsInternal { get; protected set; } = null;
		public uint FailJumpAddress { get; protected set; } = 0;

		public CreateObjectInstruction(Opcode opcode, uint address, BytecodeReader reader) : base(opcode, address, reader) => Read(reader);

		protected virtual void Read(BytecodeReader reader)
		{
			Parent = reader.ReadIdentifier();
			IsDataBlock = reader.ReadBool();
			FailJumpAddress = reader.ReadUInt();
		}

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Parent, "parent");
			writer.WriteValue(IsDataBlock, "is datablock");

			if (IsInternal != null)
			{
				writer.WriteValue(IsInternal, "is internal");
			}

			writer.WriteAddressValue(FailJumpAddress, "fail jump address");
		}
	}

	/// <summary>
	/// Instruction for the second part of object creation.
	/// </summary>
	public class AddObjectInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public bool PlaceAtRoot { get; } = reader.ReadBool();

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(PlaceAtRoot, "place at root");
		}
	}

	/// <summary>
	/// Instruction for the third and final part of object creation.
	/// </summary>
	public class EndObjectInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		/// <summary>
		/// Can either be for `isDataBlock` or `placeAtRoot`.
		/// </summary>
		public bool Value { get; } = reader.ReadBool();

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Value, "is datablock or place at root");
		}
	}

	/// <summary>
	/// Branch instruction, for loops, conditionals, breaks, continues, and logical AND/OR.
	/// </summary>
	public class BranchInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public uint TargetAddress { get; } = reader.ReadUInt();

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
	public class ReturnInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public bool ReturnsValue { get; } = reader.ReturnableValue;
	}

	/// <summary>
	/// Opcodes for binary instructions like OP_ADD, OP_XOR, OP_CMPEQ, etc.
	/// </summary>
	public class BinaryInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// A separate instruction for OP_COMPARE_STR because the operands are inverse of the other
	/// binary operations.
	/// </summary>
	public class BinaryStringInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	public class UnaryInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public bool IsNot => Opcode.Tag == OpcodeTag.OP_NOT || Opcode.Tag == OpcodeTag.OP_NOTF;
	}

	/// <summary>
	/// Variable instruction for regular variables only.<br/><br/>
	///
	/// For "array"-indexing a variable, see <see cref="VariableArrayInstruction"/>.
	/// </summary>
	public class VariableInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public StringTableEntry Name { get; } = reader.ReadIdentifier();

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Name, "variable name");
		}
	}

	/// <summary>
	/// "Array"-indexing a variable (e.g. OP_SETCURVAR_ARRAY, OP_SETCURVAR_ARRAY_CREATE, etc.).
	/// </summary>
	public class VariableArrayInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_LOADVAR_*
	/// </summary>
	public class LoadVariableInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_SAVEVAR_*
	/// </summary>
	public class SaveVariableInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_SETCUROBJECT
	/// </summary>
	public class ObjectInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_SETCUROBJECT_NEW
	/// </summary>
	public class ObjectNewInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_SETCUROBJECT_INTERNAL
	/// </summary>
	public class ObjectInternalInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// Field instruction.
	/// </summary>
	public class FieldInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public StringTableEntry Name { get; } = reader.ReadIdentifier();

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Name, "field name");
		}
	}

	/// <summary>
	/// "Array"-indexing a field (e.g. OP_SETCURFIELD_ARRAY).
	/// </summary>
	public class FieldArrayInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_LOADFIELD_*
	/// </summary>
	public class LoadFieldInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_SAVEFIELD_*
	/// </summary>
	public class SaveFieldInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// Type conversion instruction.<br/><br/>
	///
	/// Does not have any operands, but it still has extra information we want to know.
	/// </summary>
	public class ConvertToTypeInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public TypeReq Type => Opcode.TypeReq;
	}

	/// <summary>
	/// Immediate instruction for integers, floats, strings, tagged strings, and identifiers.<br/><br/>
	///
	/// "Identifier" is a term that Torque uses to refer to strings that aren't surrounded by quotes...
	/// TorqueScript is a very odd language.
	/// </summary>
	public abstract class ImmediateInstruction<T>(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public T Value { get; protected set; }

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Value, "value");
		}
	}

	public class ImmediateStringInstruction : ImmediateInstruction<StringTableEntry>
	{
		public bool IsTaggedString => Opcode.Tag == OpcodeTag.OP_TAG_TO_STR;
		public bool IsIdentifier => Opcode.Tag == OpcodeTag.OP_LOADIMMED_IDENT;

		public ImmediateStringInstruction(Opcode opcode, uint address, BytecodeReader reader) : base(opcode, address, reader)
		{
			Value = opcode.Tag == OpcodeTag.OP_LOADIMMED_IDENT ? reader.ReadIdentifier() : reader.ReadString();
		}
	}

	public class ImmediateUIntInstruction : ImmediateInstruction<uint>
	{
		public ImmediateUIntInstruction(Opcode opcode, uint address, BytecodeReader reader) : base(opcode, address, reader)
		{
			Value = reader.ReadUInt();
		}
	}

	public class ImmediateDoubleInstruction : ImmediateInstruction<double>
	{
		public ImmediateDoubleInstruction(Opcode opcode, uint address, BytecodeReader reader) : base(opcode, address, reader)
		{
			Value = reader.ReadDouble();
		}
	}

	/// <summary>
	/// Function call instruction.
	/// </summary>
	public class CallInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public StringTableEntry Name { get; } = reader.ReadIdentifier();
		public StringTableEntry? Namespace { get; } = reader.ReadIdentifier();
		public uint CallType { get; } = reader.ReadUInt();

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
	public class AdvanceStringInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// Advance-string instruction with appended character (used for SPC, TAB, and NL keywords).<br/><br/>
	/// </summary>
	public class AdvanceAppendInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader)
	{
		public char Char { get; } = reader.ReadChar();

		public override void Visit(DisassemblyWriter writer)
		{
			base.Visit(writer);

			writer.WriteValue(Char, "char to append");
		}
	}

	/// <summary>
	/// OP_ADVANCE_STR_COMMA
	/// </summary>
	public class AdvanceCommaInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_ADVANCE_STR_NUL
	/// </summary>
	public class AdvanceNullInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_REWIND_STR
	/// </summary>
	public class RewindStringInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_TERMINATE_REWIND_STR
	/// </summary>
	public class TerminateRewindInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_PUSH
	/// </summary>
	public class PushInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_PUSH_FRAME
	/// </summary>
	public class PushFrameInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_BREAK
	/// </summary>
	public class DebugBreakInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_UNIT_CONVERSION
	/// </summary>
	public class UnitConversionInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }

	/// <summary>
	/// OP_UNUSED#
	/// </summary>
	public class UnusedInstruction(Opcode opcode, uint address, BytecodeReader reader) : Instruction(opcode, address, reader) { }
}
