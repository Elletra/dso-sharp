/**
 * Instruction.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Opcodes;

namespace DSO.Versions.Constructor
{
	public class CreateObjectInstruction(Opcode opcode, uint address, BytecodeReader reader) : Disassembler.CreateObjectInstruction(opcode, address, reader)
	{
		protected override void Read(Disassembler.BytecodeReader reader)
		{
			Parent = reader.ReadIdentifier();
			IsDataBlock = reader.ReadBool();
			IsInternal = reader.ReadBool();
			FailJumpAddress = reader.ReadUInt();
		}
	}
}
