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
		BlocklandV20,
		BlocklandV21,
	};

	public class GameVersion
	{
		static public string GetDisplayName(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => "Automatically determines the game version from script file.",
			GameIdentifier.TorqueGameEngine14 => "Torque Game Engine 1.4",
			GameIdentifier.ForgettableDungeon => "The Forgettable Dungeon",
			GameIdentifier.BlocklandV20 => "Blockland v20",
			GameIdentifier.BlocklandV21 => "Blockland v21",
			_ => "<ERROR>",
		};

		static public uint GetVersionFromIdentifier(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => 0,
			GameIdentifier.TorqueGameEngine14 => GameVersions.TGE14,
			GameIdentifier.ForgettableDungeon => GameVersions.TFD,
			GameIdentifier.BlocklandV20 => GameVersions.BLV20,
			GameIdentifier.BlocklandV21 => GameVersions.BLV21,
			_ => 0,
		};

		static public GameIdentifier GetIdentifierFromVersion(uint version) => version switch
		{
			GameVersions.TGE14 => GameIdentifier.TorqueGameEngine14,
			GameVersions.TFD => GameIdentifier.ForgettableDungeon,
			GameVersions.BLV20 => GameIdentifier.BlocklandV20,
			GameVersions.BLV21 => GameIdentifier.BlocklandV21,
			_ => GameIdentifier.Auto,
		};

		static public Ops? CreateOps(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => null,
			GameIdentifier.TorqueGameEngine14 => new Ops(),
			GameIdentifier.ForgettableDungeon => new TFD.Ops(),
			GameIdentifier.BlocklandV20 => new Blockland.V20.Ops(),
			GameIdentifier.BlocklandV21 => new Blockland.V21.Ops(),
			_ => null,
		};

		static public FileLoader? CreateFileLoader(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.Auto => null,
			GameIdentifier.TorqueGameEngine14 => new FileLoader(),
			GameIdentifier.ForgettableDungeon => new TFD.FileLoader(),
			GameIdentifier.BlocklandV20 => new Blockland.FileLoader(),
			GameIdentifier.BlocklandV21 => new Blockland.FileLoader(),
			_ => null,
		};

		static public GameVersion? Create(GameIdentifier identifier) => identifier == GameIdentifier.Auto ? null : new()
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
		public Ops Ops { get; set; } = null;
		public FileLoader FileLoader { get; set; } = null;
	}
}
