/**
 * GameVersion.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Disassembler;
using DSO.Loader;
using DSO.Opcodes;

using static DSO.Constants.Decompiler;

namespace DSO.Versions
{
	public enum GameIdentifier : uint
	{
		Auto,
		TorqueGameEngine10,
		TorqueGameEngine14,
		Tribes2,
		ForgettableDungeon,
		BlocklandV1,
		BlocklandV20,
		BlocklandV21,
	};

	public class GameVersion
	{
		static public string GetDisplayName(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => "Automatically determines the game from script file.",
			GameIdentifier.TorqueGameEngine10 => "Torque Game Engine 1.0-1.3",
			GameIdentifier.TorqueGameEngine14 => "Torque Game Engine 1.4",
			GameIdentifier.Tribes2 => "Tribes 2",
			GameIdentifier.ForgettableDungeon => "The Forgettable Dungeon",
			GameIdentifier.BlocklandV1 => "Blockland v1",
			GameIdentifier.BlocklandV20 => "Blockland v20",
			GameIdentifier.BlocklandV21 => "Blockland v21",
			_ => "<ERROR>",
		};

		static public uint GetVersionFromIdentifier(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => 0,
			GameIdentifier.TorqueGameEngine10 => GameVersions.TGE10,
			GameIdentifier.TorqueGameEngine14 => GameVersions.TGE14,
			GameIdentifier.Tribes2 => GameVersions.T2,
			GameIdentifier.ForgettableDungeon => GameVersions.TFD,
			GameIdentifier.BlocklandV1 => GameVersions.BLV1,
			GameIdentifier.BlocklandV20 => GameVersions.BLV20,
			GameIdentifier.BlocklandV21 => GameVersions.BLV21,
			_ => 0,
		};

		static public GameIdentifier[] GetIdentifiersFromVersion(uint version) => version switch
		{
			GameVersions.TGE10 or GameVersions.TFD => [GameIdentifier.TorqueGameEngine10, GameIdentifier.ForgettableDungeon],
			GameVersions.TGE14 => [GameIdentifier.TorqueGameEngine14],
			GameVersions.T2 => [GameIdentifier.Tribes2],
			GameVersions.BLV1 => [GameIdentifier.BlocklandV1],
			GameVersions.BLV20 => [GameIdentifier.BlocklandV20],
			GameVersions.BLV21 => [GameIdentifier.BlocklandV21],
			_ => [],
		};

		static public Ops? CreateOps(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => null,
			GameIdentifier.TorqueGameEngine10 or GameIdentifier.Tribes2 => new Ops(),
			GameIdentifier.TorqueGameEngine14 => new TorqueGameEngine14.Ops(),
			GameIdentifier.ForgettableDungeon => new TFD.Ops(),
			GameIdentifier.BlocklandV1 => new Blockland.V1.Ops(),
			GameIdentifier.BlocklandV20 => new Blockland.V20.Ops(),
			GameIdentifier.BlocklandV21 => new Blockland.V21.Ops(),
			_ => null,
		};

		static public FileLoader? CreateFileLoader(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => null,
			GameIdentifier.TorqueGameEngine10 or GameIdentifier.Tribes2 => new FileLoader(),
			GameIdentifier.TorqueGameEngine14 => new TorqueGameEngine14.FileLoader(),
			GameIdentifier.ForgettableDungeon => new TFD.FileLoader(),
			GameIdentifier.BlocklandV1 => new Blockland.V1.FileLoader(),
			GameIdentifier.BlocklandV20 => new Blockland.FileLoader(),
			GameIdentifier.BlocklandV21 => new Blockland.FileLoader(),
			_ => null,
		};

		static public GameVersion? Create(GameIdentifier identifier)
		{
			var displayName = GetDisplayName(identifier);
			var version = GetVersionFromIdentifier(identifier);
			var ops = CreateOps(identifier);
			var loader = CreateFileLoader(identifier);

			return displayName == null || ops == null || loader == null ? null : new()
			{
				Identifier = identifier,
				DisplayName = displayName,
				Version = version,
				Ops = ops,
				FileLoader = loader,
			};
		}

		public GameIdentifier Identifier { get; set; } = GameIdentifier.Auto;
		public string DisplayName { get; set; } = "<ERROR>";
		public uint Version { get; set; } = 0;
		public Ops? Ops { get; set; } = null;
		public FileLoader? FileLoader { get; set; } = null;

		public void Visit(DisassemblyWriter writer)
		{
			writer.WriteCommentLine("");
			writer.WriteCommentLine($" Game Settings : {DisplayName}");
			writer.WriteCommentLine($" DSO Version   : {Version}");
			writer.WriteCommentLine("");
		}
	}
}
