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

namespace DSO.Versions.TGE14
{
	public class FileLoader : Loader.FileLoader
	{
		protected override void ReadTables(FileData data)
		{
			ReadStringTable(data, global: true);
			ReadStringTable(data, global: false);
			ReadFloatTable(data, global: true);
			ReadFloatTable(data, global: false);
		}
	}
}
