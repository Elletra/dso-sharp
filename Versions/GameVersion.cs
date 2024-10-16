using DSO.Loader;
using DSO.Opcodes;

using static DSO.Constants.Decompiler;

namespace DSO.Versions
{
	public enum GameIdentifier : uint
	{
		ForgettableDungeon,
		TorqueGameEngine14,
		BlocklandV20,
		BlocklandV21,
		Unknown,
	};

	public class GameVersion
	{
		static public string GetDisplayName(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.ForgettableDungeon => "The Forgettable Dungeon",
			GameIdentifier.TorqueGameEngine14 => "Torque Game Engine 1.4",
			GameIdentifier.BlocklandV20 => "Blockland v20",
			GameIdentifier.BlocklandV21 => "Blockland v21",
			_ => "<ERROR>",
		};

		static public uint GetVersionFromIdentifier(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.ForgettableDungeon => GameVersions.TFD,
			GameIdentifier.TorqueGameEngine14 => GameVersions.TGE14,
			GameIdentifier.BlocklandV20 => GameVersions.BLV20,
			GameIdentifier.BlocklandV21 => GameVersions.BLV21,
			_ => 0,
		};

		static public GameIdentifier GetIdentifierFromVersion(uint version) => version switch
		{
			GameVersions.TFD => GameIdentifier.ForgettableDungeon,
			GameVersions.TGE14 => GameIdentifier.TorqueGameEngine14,
			GameVersions.BLV20 => GameIdentifier.BlocklandV20,
			GameVersions.BLV21 => GameIdentifier.BlocklandV21,
			_ => GameIdentifier.Unknown,
		};

		static public Ops? CreateOps(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.ForgettableDungeon => new TFD.Ops(),
			GameIdentifier.TorqueGameEngine14 => new Ops(),
			// TODO: GameIdentifier.BlocklandV20 =>,
			GameIdentifier.BlocklandV21 => new Blockland.V21.Ops(),
			_ => null,
		};

		static public FileLoader? CreateFileLoader(GameIdentifier identifier) => identifier switch
		{
			GameIdentifier.ForgettableDungeon => new TFD.FileLoader(),
			GameIdentifier.TorqueGameEngine14 => new FileLoader(),
			GameIdentifier.BlocklandV20 => new Blockland.FileLoader(),
			GameIdentifier.BlocklandV21 => new Blockland.FileLoader(),
			_ => null,
		};

		static public GameVersion? Create(GameIdentifier identifier) => identifier >= GameIdentifier.Unknown ? null : new()
		{
			Identifier = identifier,
			DisplayName = GetDisplayName(identifier),
			Version = GetVersionFromIdentifier(identifier),
			Ops = CreateOps(identifier),
			FileLoader = CreateFileLoader(identifier),
		};

		public GameIdentifier Identifier { get; set; } = GameIdentifier.Unknown;
		public string DisplayName { get; set; } = "<ERROR>";
		public uint Version { get; set; } = 0;
		public Ops Ops { get; set; } = null;
		public FileLoader FileLoader { get; set; } = null;
	}
}
