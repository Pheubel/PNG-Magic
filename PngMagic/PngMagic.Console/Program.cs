using PngMagic.Console;
using PngMagic.Core;

const int PackArgumentCount = 5;
const string PackArgumentTemplate = "pack <target image path> <output image path> [pack image paths]";

const int ExtractArgumentCount = 4;
const string ExtractArgumentTemplate = "extract <target image path> <extension type>";

const int MinArgCount = 4;
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
            Stream[] payloadStreams = new Stream[payloadArgs.Length];

            for (int i = 0; i < payloadArgs.Length; i++)
            {
                payloadStreams[i] = File.OpenRead(payloadArgs[i]);
            }

            PackOperation.Start(containerPng, outputStream, payloadStreams);

            for (int i = 0; i < payloadStreams.Length; i++)
            {
                payloadStreams[i].Dispose();
            }
        }

        break;

    case OperationMode.Extract:
        if (commandArgs.Length != ExtractArgumentCount)
        {
            Console.WriteLine($"Invalid argument patern, expected:\n{ExtractArgumentTemplate}");
            Environment.Exit(1);
        }

        string containerName = Path.GetFileNameWithoutExtension(containerPng);

        int count = 1;
        foreach(var payload in ExtractOperation.GetInjectedPayloads(containerPng))
        {
            string destination = $"unpack_{count}_" + containerName + $".{commandArgs[3]}";
            File.WriteAllBytes(destination, payload);

            Console.WriteLine($"Unpacked a payload to {destination}");
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