/**
 * GameVersion.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Loader;
using DSO.Opcodes;

using static DSO.Constants.Decompiler;

namespace DSO.Versions
{
	public enum GameIdentifier : uint
	{
		Auto,
		ForgettableDungeon,
		TorqueGameEngine14,
		BlocklandBeta,
		BlocklandV1,
		BlocklandV20,
		BlocklandV21,
	};

	public class GameVersion
	{
		static public string GetDisplayName(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => "Automatically determines the game from script file.",
			GameIdentifier.TorqueGameEngine14 => "Torque Game Engine 1.4",
			GameIdentifier.ForgettableDungeon => "The Forgettable Dungeon",
			GameIdentifier.BlocklandBeta => "Blockland Retail Beta",
			GameIdentifier.BlocklandV1 => "Blockland v1",
			GameIdentifier.BlocklandV20 => "Blockland v20",
			GameIdentifier.BlocklandV21 => "Blockland v21",
			_ => "<ERROR>",
		};

		static public uint GetVersionFromIdentifier(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => 0,
			GameIdentifier.TorqueGameEngine14 => GameVersions.TGE14,
			GameIdentifier.ForgettableDungeon => GameVersions.TFD,
			GameIdentifier.BlocklandBeta => GameVersions.BLBETA,
			GameIdentifier.BlocklandV1 => GameVersions.BLV1,
			GameIdentifier.BlocklandV20 => GameVersions.BLV20,
			GameIdentifier.BlocklandV21 => GameVersions.BLV21,
			_ => 0,
		};

		static public GameIdentifier[] GetIdentifiersFromVersion(uint version) => version switch
		{
			GameVersions.TGE14 => [GameIdentifier.TorqueGameEngine14],
			GameVersions.TFD or GameVersions.BLBETA => [GameIdentifier.ForgettableDungeon, GameIdentifier.BlocklandBeta],
			GameVersions.BLV1 => [GameIdentifier.BlocklandV1],
			GameVersions.BLV20 => [GameIdentifier.BlocklandV20],
			GameVersions.BLV21 => [GameIdentifier.BlocklandV21],
			_ => [GameIdentifier.Auto],
		};

		static public Ops? CreateOps(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => null,
			GameIdentifier.TorqueGameEngine14 => new Ops(),
			GameIdentifier.ForgettableDungeon => new TFD.Ops(),
			GameIdentifier.BlocklandBeta => new Blockland.Beta.Ops(),
			GameIdentifier.BlocklandV1 => new Blockland.V1.Ops(),
			GameIdentifier.BlocklandV20 => new Blockland.V20.Ops(),
			GameIdentifier.BlocklandV21 => new Blockland.V21.Ops(),
			_ => null,
		};

		static public FileLoader? CreateFileLoader(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => null,
			GameIdentifier.TorqueGameEngine14 => new FileLoader(),
			GameIdentifier.ForgettableDungeon => new TFD.FileLoader(),
			GameIdentifier.BlocklandBeta => new Blockland.Beta.FileLoader(),
			GameIdentifier.BlocklandV1 => new Blockland.V1.FileLoader(),
			GameIdentifier.BlocklandV20 => new Blockland.FileLoader(),
			GameIdentifier.BlocklandV21 => new Blockland.FileLoader(),
			_ => null,
		};

		static public GameVersion Create(GameIdentifier identifier) => new()
		{
			Identifier = identifier,
			DisplayName = GetDisplayName(identifier),
			Version = GetVersionFromIdentifier(identifier),
			Ops = CreateOps(identifier),
			FileLoader = CreateFileLoader(identifier),
		};

		public GameIdentifier Identifier { get; set; } = GameIdentifier.Auto;
		public string DisplayName { get; set; } = "<ERROR>";
		public uint Version { get; set; } = 0;
		public Ops? Ops { get; set; } = null;
		public FileLoader? FileLoader { get; set; } = null;
	}
}
