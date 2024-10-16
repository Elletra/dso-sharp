using DSO.Loader;

namespace DSO.Versions.TFD
{
    public class FileLoader : Loader.FileLoader
    {
		protected override void ReadTables(FileData data)
		{
			ReadStringTable(data, global: true);
			ReadFloatTable(data, global: true);
			ReadStringTable(data, global: false);
			ReadFloatTable(data, global: false);
		}
	}
}
