using PngMagic.Console;
using PngMagic.Core;

const int PackArgumentCount = 5;
const string PackArgumentTemplate = "pack <target image path> <output image path> [pack image paths]";

const int ExtractMinArgumentCount = 3;
const int ExtractMaxArgumentCount = 4;
const string ExtractArgumentTemplate = "extract <target image path> (output directory)";

const int MinArgCount = 3;
const string GenericArgumentErrorMessage = $"Invalid arguments, expected one of the following paterns:\n\n{PackArgumentTemplate}\n{ExtractArgumentTemplate}";



Span<string> commandArgs = Environment.GetCommandLineArgs();

if (commandArgs.Length < MinArgCount)
{
    Console.WriteLine(GenericArgumentErrorMessage);
    Environment.Exit(1);
}

OperationMode operationsMode = GetOperation(commandArgs[1]);

Console.WriteLine($"Operation mode: {operationsMode}");

string containerPng = commandArgs[2];

switch (operationsMode)
{
    case OperationMode.Pack:
        if (commandArgs.Length < PackArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{PackArgumentTemplate}");
            Environment.Exit(1);
        }

        using (var outputStream = File.OpenWrite(commandArgs[3]))
        {
            Span<string> payloadArgs = commandArgs[4..];

            PackOperation.Start(containerPng, outputStream, payloadArgs);
        }

        break;

    case OperationMode.Extract:
        if (commandArgs.Length < ExtractMinArgumentCount || commandArgs.Length > ExtractMaxArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{ExtractArgumentTemplate}");
            Environment.Exit(1);
        }


        string outDirectory = commandArgs.Length == ExtractMinArgumentCount ? Path.GetDirectoryName(containerPng)! : commandArgs[3];

        Directory.CreateDirectory(outDirectory);

        int count = 1;
        foreach(var payload in ExtractOperation.GetInjectedPayloads(containerPng))
        {
            if(payload is not FilePayload filePayload)
            {
                Console.WriteLine("Raw byte payload detected, skipping.");
                continue;
            }

            var outFilePath = Path.Combine(outDirectory, filePayload.FileName);

            File.WriteAllBytes(outFilePath, payload.PayloadData);

            Console.WriteLine($"Unpacked a payload to {outFilePath}");
            count++;
        }

        break;

    default:
        Console.WriteLine(GenericArgumentErrorMessage);
        Environment.Exit(1);
        break;
}


static OperationMode GetOperation(string argument)
{
    return argument.ToLower() switch
    {
        "pack" => OperationMode.Pack,
        "extract" => OperationMode.Extract,
        "project" => OperationMode.Project,
        _ => OperationMode.Unspecified
    };
}