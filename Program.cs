using DSO;
using DSO.Util;

Logger.LogHeader();

var decompiler = new Decompiler();

decompiler.Decompile("client", "server");
