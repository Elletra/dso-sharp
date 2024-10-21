/**
 * Constants.cs
 *
 * Copyright (C) 2024 Elletra
 *
 * This file is part of the DSO Sharp source code. It may be used under the BSD 3-Clause License.
 *
 * For full terms, see the LICENSE file or visit https://spdx.org/licenses/BSD-3-Clause.html
 */

namespace DSO.Constants
{
    static public class Decompiler
    {
        public const string AUTHOR = "Elletra";
        public const string VERSION = "1.1.1";
        public const string EXTENSION = ".dso";
        public const string DISASM_EXTENSION = ".disasm";

        static public class GameVersions
        {
            public const uint TGE14 = 36;
            public const uint TFD = 33;
            public const uint BLBETA = 33;
            public const uint BLV1 = 90;
            public const uint BLV20 = 190;
            public const uint BLV21 = 210;
        }
    }

    static public class Disassembly
    {
        public const int VALUE_COLUMN_LENGTH = 32;
        public const int VALUE_TRUNCATE_LENGTH = VALUE_COLUMN_LENGTH - 8;
    }
}
