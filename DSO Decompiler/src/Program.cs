using DsoDecompiler.Loader;

namespace DsoDecompiler
{
	class Program
	{
		static void Main (string[] args)
		{
			var loader = new FileLoader ();
			var data = loader.LoadFile ("main.cs.dso", 210);
			var size = data.StringTableSize (true);

			System.Console.WriteLine ("\n### Global String Table:");

			for (uint i = 0; i < size; i++)
			{
				if (data.HasString (i, true))
				{
					System.Console.WriteLine (data.StringTableValue (i, true));
				}
			}

			size = data.StringTableSize (false);

			System.Console.WriteLine ("\n### Function String Table:");

			for (uint i = 0; i < size; i++)
			{
				if (data.HasString (i, false))
				{
					System.Console.WriteLine (data.StringTableValue (i, false));
				}
			}

			size = data.FloatTableSize (true);

			System.Console.WriteLine ("\n### Global Float Table:");

			for (uint i = 0; i < size; i++)
			{
				System.Console.WriteLine (data.FloatTableValue (i, true));
			}

			size = data.FloatTableSize (false);

			System.Console.WriteLine ("\n### Function Float Table:");

			for (uint i = 0; i < size; i++)
			{
				System.Console.WriteLine (data.FloatTableValue (i, false));
			}

			System.Console.WriteLine ($"\nCode size: {data.CodeSize ()}");
		}
	}
}
