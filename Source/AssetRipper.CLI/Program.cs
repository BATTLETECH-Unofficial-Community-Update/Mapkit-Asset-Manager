using AssetRipper.Export.Configuration;
using AssetRipper.Export.UnityProjects;
using AssetRipper.IO.Files;
using AssetRipper.Import.Logging;

string inputPath;
string outputPath;

if (args.Length >= 2)
{
	inputPath = args[0];
	outputPath = args[1];
}
else
{
	Console.Error.WriteLine("Usage: AssetRipper.CLI <input path> <output path>");
	return 1;
}

Logger.Add(new ConsoleLogger());
Logger.LogSystemInformation("AssetRipper.CLI");

FullConfiguration settings = new();
ExportHandler handler = new(settings);

Console.WriteLine($"Loading: {inputPath}");
Console.WriteLine($"Export to: {outputPath}");

var gameData = handler.LoadAndProcess([inputPath], LocalFileSystem.Instance);

if (gameData.GameBundle.HasAnyAssetCollections())
{
	Directory.CreateDirectory(outputPath);
	handler.Export(gameData, outputPath, LocalFileSystem.Instance);
	Console.WriteLine("Export complete.");
	return 0;
}
else
{
	Console.Error.WriteLine("No assets found in input path.");
	return 1;
}
