using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using SQLServerPing.Commands;

try
{

var serviceCollection = new ServiceCollection()
    .AddLogging(configure =>
            configure
                .AddSimpleConsole(opts => {
                    opts.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                })
    );

using var registrar = new DependencyInjectionRegistrar(serviceCollection);
var app = new CommandApp(registrar);

app.Configure(
    config =>
    {
        config.ValidateExamples();

        config.Settings.ApplicationName = "SQLPing";

        //config.AddCommand<PingCommand>("ping")
        //        .WithDescription("Ping SQL Server until stopped")
        //        .WithExample(new[] { "ping" });

    });

app.SetDefaultCommand<PingCommand>();

return await app.RunAsync(args);
}
catch (Exception ex)
{
        Console.WriteLine("ERROR: " + ex.Message);
        return 1;
}