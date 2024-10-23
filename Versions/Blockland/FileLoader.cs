/**
 * FileLoader.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

using DSO.Loader;

namespace DSO.Versions.Blockland
{
    public class FileLoader : Loader.FileLoader
    {
		static private string UnencryptString(string str)
		{
			var key = "cl3buotro";
			var unencrypted = "";

			for (var i = 0; i < str.Length; i++)
			{
				unencrypted += (char) (str[i] ^ key[i % 9]);
			}

			return unencrypted;
		}

		protected override void ReadTables(FileData data)
		{
			ReadStringTable(data, global: true);
			ReadFloatTable(data, global: true);
			ReadStringTable(data, global: false);
			ReadFloatTable(data, global: false);
		}

		protected override void ReadStringTable(FileData data, bool global)
		{
			var table = new StringTable(UnencryptString(_reader.ReadString()), global);

			if (global)
			{
				data.GlobalStringTable = table;
			}
			else
			{
				data.FunctionStringTable = table;
			}
		}
	}
}
