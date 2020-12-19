using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

/// <summary>
/// Add the following to deploy a specific file: -t r -f MACrosser
/// </summary>
internal class AttributeProgram
{
    public static int Main(string[] args) => CommandLineApplication.Execute<AttributeProgram>(args);

    [Required]
    [Option("-t <type>", Description = "The type must be either i (indicator) or r (robot)")]
    [AllowedValues("i", "r", IgnoreCase = true)]
    public string Type { get; } = "normal";

    [Required]
    [Option("-f <filename>", Description = "The file name")]
    public string FileName { get; }

    private void OnExecute()
    {
        var currentFolder = Environment.CurrentDirectory;
        currentFolder = Path.Combine(currentFolder, @"..\..\..\..\..\");
        currentFolder = Path.Combine(currentFolder, "cAlgo.Library");
        currentFolder = Path.GetFullPath(currentFolder);

        currentFolder = Path.Combine(currentFolder, GetSubFolder());

        var file = GetFileNameAtFolder(currentFolder);
        if (file == null)
        {
            return;
        }

        ProcessFile(file);
    }

    private void ProcessFile(string fileName)
    {
        const string BaseFolder = @"C:\Users\veryb\Documents\cAlgo\Sources\";

        UpdateFileVersion(fileName);

        var subFolder = Path.GetFileNameWithoutExtension(fileName);
        var destFileName = Path.Combine(BaseFolder, GetSubFolder());

        // Solution level
        destFileName = Path.Combine(destFileName, subFolder);

        // Go to code file level
        destFileName = Path.Combine(destFileName, subFolder);
        destFileName = Path.Combine(destFileName, subFolder + ".cs");

        File.Copy(fileName, destFileName, true);
    }

    private void UpdateFileVersion(string fileName)
    {
        const string FileContentsPrefix = "// Version";

        var fileContents = File.ReadAllText(fileName);
        if (fileContents.StartsWith(FileContentsPrefix))
        {
            var index = fileContents.IndexOf(Environment.NewLine);
            fileContents = fileContents.Remove(0, index + Environment.NewLine.Length);
        }

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        using (var writer = new StreamWriter(fileName))
        {
            writer.WriteLine($"{FileContentsPrefix} {timestamp}");
            writer.Write(fileContents);
        }
    }

    private string GetSubFolder()
    {
        switch (Type.ToLowerInvariant())
        {
            case "i":
                return "Indicators";

            case "r":
                return "Robots";
        }

        return null;
    }

    private string GetFileNameAtFolder(string folder)
    {
        var attempt = 0;
        var name = FileName;

        do
        {
            if (attempt > 0)
            {
                name += ".cs";
            }

            var file = Path.Combine(folder, name);
            if (File.Exists(file))
            {
                return file;
            }

            attempt++;
        } while (attempt <= 1);

        Console.WriteLine($"Error: Could not find file {FileName}");
        return null;
    }
}