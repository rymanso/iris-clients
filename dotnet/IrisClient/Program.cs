using Microsoft.Extensions.Configuration;
using IrisClient;
using IrisClient.helpers;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var settings = config.Get<Settings>();
settings.Validate();

var client = ServiceBusClientHelpers.GetAuthenticatedServiceBusClient(settings);

var processor = client.CreateProcessor(settings.ServiceBusQueue);
Console.WriteLine("Connection created with processor");


try
{
    var relativeDownloadDirectory = string.IsNullOrWhiteSpace(settings.RelativeFileDownloadDirectory)
        ? "data"
        : settings.RelativeFileDownloadDirectory;
    var handlers = new MessageProcessors(relativeDownloadDirectory);
    processor.ProcessMessageAsync += handlers.DownloadHandler;
    processor.ProcessErrorAsync += MessageProcessors.ErrorHandler;
    await processor.StartProcessingAsync();

    Console.WriteLine("Wait for a minute and then press any key to end the processing");
    Console.ReadKey();

    Console.WriteLine("\nStopping the receiver...");
    await processor.StopProcessingAsync();
    Console.WriteLine("Stopped receiving messages");
}
finally
{
    await processor.DisposeAsync();
    await client.DisposeAsync();
}