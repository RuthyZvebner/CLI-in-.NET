using System.CommandLine;

static string GetLanguageFromExtension(string fileExtension)
{
    switch (fileExtension)
    {
        case ".cs":
            return "csharp";
        case ".fs":
            return "fsharp";
        case ".vb":
            return "vb";
        case ".ps1":
            return "pwsh";
        case ".sql":
            return "sql";
        case ".html":
            return "html";
        case ".js":
            return "javascript";
        case ".py":
            return "python";
        case ".java":
            return "java";
        case ".cpp":
            return "cpp";
        case ".rb":
            return "ruby";
        case ".ts":
            return "typescript";
        case ".swift":
            return "swift";
        case ".go":
            return "go";
        case ".asm":
            return "assembly";
        case ".c":
            return "c";
        case ".jsx":
            return "react";
        default:
            return "";
    }
}
static bool IsCodeFile(string filePath, string selectedLanguage)
{
    if (selectedLanguage.Contains("all"))
        return GetLanguageFromExtension(Path.GetExtension(filePath)) != "";
    return GetLanguageFromExtension(Path.GetExtension(filePath)) == selectedLanguage;
}

var bundleCommand = new Command("bundle", "bundle code files to a single file");

var outputOption = new Option<FileInfo>("--output", "file path and name");
var languageOption = new Option<string>(name: "--language", description: "languases in the bundle (use 'all' to include all code files)") { IsRequired = true }.FromAmong(new[] {
    "all",
    "csharp",
    "fsharp",
    "vb",
    "pwsh",
    "sql",
    "html" ,
    "javascript",
    "python",
    "java",
    "cpp",
    "ruby",
    "typescript",
    "swift",
    "go",
    "c",
    "assembly",
    "react"}); 
var noteOption = new Option<bool>(name: "--note", description: "whether to include source code as a comment in the bundle file");
var sortOption = new Option<string[]>("--sort", description: "how to sort").FromAmong(new[] { "alphabetical", "language" });
var removeOption = new Option<bool>(name: "--remove-empty-lines", description: "remove empty lines from the code");
var authorOption = new Option<string>(name: "--author", description: "name of the author");

var CreateRspOption = new Option<bool>("--create-rsp", "Generate a response file with the current command and options.");
var createRspCommand = new Command("create-rsp", "Create response file");

bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeOption);
bundleCommand.AddOption(authorOption);

languageOption.AddAlias("-l");
outputOption.AddAlias("-o");
noteOption.AddAlias("-n");
sortOption.AddAlias("-s");
removeOption.AddAlias("-r");
authorOption.AddAlias("-a");

bundleCommand.SetHandler((output, language, note, sort, remove, author) =>
{
    // Get the current directory
    var currentDirectory = Directory.GetCurrentDirectory();

    // Create a list of all files in the current directory
    var files = Directory.GetFiles(currentDirectory, "*", SearchOption.AllDirectories);

    // Filter the files to only include those that match the specified languages
    var filteredFiles = files
        .Where(x => !Path.GetDirectoryName(x).ToLower().Contains("bin") && !Path.GetDirectoryName(x).ToLower().Contains("obj") && IsCodeFile(x, language));
    // Sort the files in the specified order
    try
    {
        using (var outputFile = File.AppendText(output.FullName))
        {
            //Write a comment to the output file
            if (note)
                outputFile.WriteLine("// Bundled from directory: {0}", currentDirectory);

            // Write the author to the output file
            if (!string.IsNullOrEmpty(author))
                outputFile.WriteLine("// Author: {0}", author);
            if (sort.Contains("language"))
            {
                // Group files by language
                var filesByLanguage = filteredFiles
                    .GroupBy(x => GetLanguageFromExtension(Path.GetExtension(x)))
                    .OrderBy(x => x.Key);

                // Write each group to the output file, with a comment header
                foreach (var group in filesByLanguage)
                {
                    outputFile.WriteLine("{0} files", group.Key);
                    foreach (var file in group)
                    {
                        outputFile.WriteLine(File.ReadAllText(file));
                    }
                }
            }
            else
            {
                // Sort alphabetically
                filteredFiles = filteredFiles.OrderBy(x => x).ToArray();

                // Write the files to the output file
                foreach (var file in filteredFiles)
                    outputFile.WriteLine(File.ReadAllText(file));
            }
        }
        Console.WriteLine("Files were bundled successfully");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("file path invalid");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred");
    }


    // ... (rest of the code remains the same)
}, outputOption, languageOption, noteOption, sortOption, removeOption, authorOption);


//create-rsp
createRspCommand.SetHandler(() =>
{
    Console.Write("Enter value for output: ");
    var outputValue = Console.ReadLine();

    Console.Write("Enter value for language: ");
    var languageValue = Console.ReadLine();

    Console.Write("Enter value for note (true/false): ");
    var noteValue = Console.ReadLine();
    while (!(noteValue == "true" || noteValue == "false"))
    {
        Console.Write("Enter again value for note (true/false): ");
        noteValue = Console.ReadLine();
    }

    bool.TryParse(noteValue, out bool note);
    Console.Write("Enter value for sort:(language/alphabetical) ");
    var sortValue = Console.ReadLine();

    Console.Write("Enter value for remove-empty-lines (true/false): ");
    var removeEmptyLinesValue = Console.ReadLine();
    while (!(noteValue == "true" || noteValue == "false"))
    {
        Console.Write("Enter again value for note (true/false): ");
        noteValue = Console.ReadLine();
    }

    bool.TryParse(removeEmptyLinesValue, out bool removeEmptyLines);

    Console.Write("Enter value for author: ");
    var authorValue = Console.ReadLine();
    string rspContent = $"--language {languageValue}" +
                                $" --output {outputValue}" +
                                $" --note {note}" +
                                $" --sort {sortValue}" +
                                $" --remove-empty-lines {removeEmptyLines}" +
                                $" --author {authorValue}";
    File.WriteAllText("response.rsp", rspContent);
    Console.WriteLine("Response file 'response.rsp' created successfully.");
});


string GetSourceCode()
{
    var sourceFilePath = typeof(Program).Assembly.Location;
    return sourceFilePath;
}

string RemoveEmptyLines(string content)
{
    return string.Join(Environment.NewLine, content.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToArray());
}
var rootCommand = new RootCommand("root command for file bundler");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
await rootCommand.InvokeAsync(args);
