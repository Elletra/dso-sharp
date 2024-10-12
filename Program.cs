using DSO.AST;
using DSO.ControlFlow;
using DSO.Decompiler.Loader;
using DSO.Disassembler;

var loader = new FileLoader();
var data = loader.LoadFile("./test.cs.dso", 210);
var disassembler = new Disassembler();
var disassembly = disassembler.Disassemble(data);
var analyzer = new ControlFlowAnalyzer();
var root = analyzer.Analyze(disassembly);
var builder = new Builder();

var elses = 0;
var continues = 0;
var breaks = 0;

void CountBranches(ControlFlowBlock block)
{
	foreach (var branch in block.Branches.Values)
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

	block.Children.ForEach(CountBranches);
}

CountBranches(root);

Console.WriteLine($"Elses: {elses}");
Console.WriteLine($"Continues: {continues}");
Console.WriteLine($"Breaks: {breaks}");

var nodes = builder.Build(root, disassembly);

{ }
