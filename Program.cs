using DSO.AST;
using DSO.CodeGenerator;
using DSO.ControlFlow;
using DSO.Loader;
using DSO.Disassembler;

var loader = new FileLoader();
var disassembler = new Disassembler();
var disassembly = disassembler.Disassemble(loader.LoadFile("./test.cs.dso", 210));
var analyzer = new ControlFlowAnalyzer();
var data = analyzer.Analyze(disassembly);

var elses = 0;
var continues = 0;
var breaks = 0;

foreach (var branch in data.Branches.Values)
{
	switch (branch.Type)
	{
		case ControlFlowBranchType.Else:
			elses++;
			break;

		case ControlFlowBranchType.Continue:
			continues++;
			break;

		case ControlFlowBranchType.Break:
			breaks++;
			break;
	}
}

Console.WriteLine($"Elses: {elses}");
Console.WriteLine($"Continues: {continues}");
Console.WriteLine($"Breaks: {breaks}");

var nodes = new Builder().Build(data, disassembly);
var generator = new CodeGenerator();
var stream = generator.Generate(nodes);

File.WriteAllText("./out.cs", string.Join("", stream));

{ }
