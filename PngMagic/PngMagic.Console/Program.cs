using PngMagic.Core;

const int PackArgumentCount = 5;
const string PackArgumentTemplate = "pack <target image path> <pack image path> <output image path>";

const int ExtractArgumentCount = 4;
const string ExtractArgumentTemplate = "extract <target image path> <output image> path";

const int ProjectArgumentCount = 4;
const string ProjectArgumentTemplate = "project <target image path> <output image path>";

const int MinArgCount = 4;
const int MaxArgCount = 5;
const string GenericArgumentErrorMessage = $"Invalid arguments, expected one of the following paterns:\n\n{PackArgumentTemplate}\n{ExtractArgumentTemplate}\n{ProjectArgumentTemplate}";



var commandArgs = Environment.GetCommandLineArgs();

if(commandArgs.Length < MinArgCount || commandArgs.Length > MaxArgCount)
{
    Console.WriteLine(GenericArgumentErrorMessage);
    Environment.Exit(1);
}

OperationMode operationsMode = GetOperation(commandArgs[1]);

// See https://aka.ms/new-console-template for more information
Console.WriteLine($"Operation mode: {operationsMode}");

string containerPng = commandArgs[2];
string payloadPng = commandArgs[3];
string outputPath = commandArgs[4];

switch (operationsMode)
{
    case OperationMode.Pack:
        if(commandArgs.Length != PackArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{PackArgumentTemplate}");
            Environment.Exit(1);
        }

        PackOperation.Start(containerPng, payloadPng);

        break;

    case OperationMode.Extract:
        if (commandArgs.Length != ExtractArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{ExtractArgumentTemplate}");
            Environment.Exit(1);
        }


        break;

    case OperationMode.Project:
        if (commandArgs.Length != ProjectArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{ProjectArgumentTemplate}");
            Environment.Exit(1);
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