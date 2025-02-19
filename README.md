# DSO Sharp

This is a DSO decompiler for the Torque Game Engine. It takes `.dso` files and decompiles them back into TorqueScript!

This project supports the following games and engines:

* Torque Game Engine 1.0-1.3 (e.g. Marble Blast Gold, Blockland v0002, [Blockland Retail Beta](https://bl.kenko.dev/Versions/Retail%20Beta), Age of Time)
* Torque Game Engine 1.4
* Tribes 2
* The Forgettable Dungeon
* Blockland v1
* Blockland v20
* Blockland v21


## Background

Years ago, I made [dso.js](https://github.com/Elletra/dso.js), a DSO decompiler for Blockland v21. The code was absolutely atrocious and used no computer science concepts whatsoever. However, it (mostly) worked, and was the only (mostly) working, publicly-available DSO decompiler for Blockland, so it was okay for the time.

I tried off and on for a few years to write a better DSO decompiler using actual [computer science](https://www.cs.tufts.edu/comp/150FP/archive/keith-cooper/dom14.pdf) [concepts](https://www.usenix.org/system/files/conference/usenixsecurity13/sec13-paper_schwartz.pdf). Unfortunately, I struggled to do so and burned out multiple times, so I eventually gave up.

But I still really wanted decompiled scripts for other games, so I decided to just rewrite the program using similar techniques in _dso.js_, but better. After all, a shoddily-coded decompiler is better than no decompiler at all!

And so, here it finally is.


## Contributing

**All opcodes must be verified by me.** If I do not have the game, I cannot verify that the opcodes are correct and ***will not approve the pull request.***

The base classes are based on Torque Game Engine 1.0-1.3. Any additional functionality is implemented by creating subclasses in the `Versions/` folder. During game detection, these subclasses are composed by `GameVersion.cs`.

If you're modifying a base class to support more engines or games, make sure it is ***absolutely necessary*** first.


## Usage

There are two ways to use this program: either as a typical console program, or as a command-line interface.

To use it normally, just drag a `.dso` file or a directory full of `.dso` files onto the program. It will try to automatically detect and decompile the file(s) that were passed in.

You can also use it as a command-line interface: `usage: dso-sharp path1[, path2[, ...]] [-h] [-q] [-g game] [-d | -D] [-X]`


| Flag                   |   Description  |
|:-----------------------|:---------------|
| `-h` | Displays help. |
| `-q` | Disables all messages (except command-line argument errors). |
| `-g` | Specifies which game's scripts we are decompiling (default: `auto`). |
| `-d` | Writes a `.disasm` file containing the disassembly. |
| `-D` | Writes only the disassembly file and nothing else. |
| `-X` | Makes the program operate as a command-line interface that takes no keyboard input and closes immediately upon completion or failure. |


### Supported Games

| Value    | Game |
|:---------|:-----|
| `auto`   | Automatically determines the game from script file (defaults to this if `--game` flag is not set). |
| `tge10`  | Torque Game Engine 1.0-1.3 |
| `tge14`  | Torque Game Engine 1.4 |
| `t2`     | Tribes 2 |
| `tfd`    | The Forgettable Dungeon |
| `blv1`   | Blockland v1 |
| `blv20`  | Blockland v20 |
| `blv21`  | Blockland v21 |

## Building

### Windows

To build for Windows:

1. Open in Visual Studio 2022 (or later)
2. Right-click on the `DSO` project and click "Publish"
3. Create a new profile with the "Folder" target
4. Set the "Target Runtime" to `win-x64`
5. Click "Show all settings" and set "Deployment mode" to `Self-contained`
6. Click "Save" and then click the large "Publish" button in the top right corner

### Linux

To build for Linux:

1. Install the .NET 8.0 SDK with `sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0`
2. Navigate to the repo folder
3. Build the project: `dotnet publish -a x64 --os linux -c Release --sc`
