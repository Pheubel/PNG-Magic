using PngMagic.Console;
using PngMagic.Core;

const int PackArgumentCount = 5;
const string PackArgumentTemplate = "pack <target image path> <output image path> [pack image paths]";

const int ExtractArgumentCount = 3;
const string ExtractArgumentTemplate = "extract <target image path>";

const int MinArgCount = 3;
const string GenericArgumentErrorMessage = $"Invalid arguments, expected one of the following paterns:\n\n{PackArgumentTemplate}\n{ExtractArgumentTemplate}";



var commandArgs = Environment.GetCommandLineArgs();

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
        if (commandArgs.Length != PackArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{PackArgumentTemplate}");
            Environment.Exit(1);
        }

        using (var outputStream = File.OpenWrite(commandArgs[4]))
        using (var payloadStream = File.OpenRead(commandArgs[3]))
        {
            PackOperation.Start(containerPng, outputStream, payloadStream);
        }

        break;

    case OperationMode.Extract:
        if (commandArgs.Length != ExtractArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{ExtractArgumentTemplate}");
            Environment.Exit(1);
        }

        int i = 1;
        foreach(var payload in ExtractOperation.GetInjectedPayloads(containerPng))
        {
            string destination = $"unpack_{i}_" + containerPng;
            File.WriteAllBytes(destination, payload);

            Console.WriteLine($"Unpacked a payload to {destination}");
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