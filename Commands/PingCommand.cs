using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using SQLServerPing.Commands.Settings;
using Console = System.Console;

namespace SQLServerPing.Commands
{
    public class PingCommand : AsyncCommand<ConsoleSettings>
    {
        private ILogger Logger { get; }

        public override async Task<int> ExecuteAsync(CommandContext context, ConsoleSettings settings)
        {
            if (settings.SQLCommand == null)
                settings.SQLCommand = @"SELECT @@SERVERNAME AS ""Server"", name as ""Database"", state_desc AS ""State"", replica_id AS ""Replica"" FROM sys.databases WHERE name = @DatabaseName";

            //Logger.LogInformation("Connection string: {Mandatory}", connectionString);
            //Logger.LogInformation("SQL Command: {Optional}", settings.SQLCommand);
            //Logger.LogInformation("CommandOptionFlag: {CommandOptionFlag}", settings.CommandOptionFlag);
            //Logger.LogInformation("CommandOptionValue: {CommandOptionValue}", settings.CommandOptionValue);

            var connString = GetConnectionString(settings);

            //Logger.LogInformation("");
            AnsiConsole.MarkupLine($"ConnectionString: [teal]{connString.EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"SQL Query       : [teal]{settings.SQLCommand.EscapeMarkup()}[/]");

            bool running = true;

            while (running)
            {

                int sec = settings.Wait;
                AnsiConsole.Status()
                    .AutoRefresh(true)
                    .Spinner(Spinner.Known.Dots) // https://jsfiddle.net/sindresorhus/2eLtsbey/embedded/result/
                    .SpinnerStyle(Style.Parse("green bold"))
                    .Start("Please wait...", ctx =>
                    {
                        // Simulate some work
                        ctx.Status($"Trying to connect to server [teal]{settings.Server.EscapeMarkup()}[/]...");
                        CallDatabase(connString, settings);

                        // Update the status and spinner
                        ctx.Status($"Waiting [teal]{sec}[/] seconds...");

                        if (settings.NonStop)
                            Thread.Sleep(TimeSpan.FromSeconds(sec));
                        else
                            running = false;
                    });
            }

            //Console.WriteLine("\nDone. Press enter.");
            //Console.ReadLine();

            return await Task.FromResult(0);
        }

        // Validate as part of the command. This is a good way of validating options if you require any injected services.
        public override ValidationResult Validate(CommandContext context, ConsoleSettings settings)
        {
            //if (settings.Wait < 1)
            //    return ValidationResult.Error("...");
            return ValidationResult.Success();
        }

        private static string GetConnectionString(ConsoleSettings settings) {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            //builder.ConnectionString = settings.ConnectionString;
            builder.DataSource = settings.Server;
            if (!string.IsNullOrEmpty(settings.Username)) {
                builder.UserID = settings.Username;
                builder.Password = settings.Password;
                builder.IntegratedSecurity = false;
            }
            else
                builder.IntegratedSecurity = true;

            builder.InitialCatalog = settings.Database;
            builder.ConnectTimeout = settings.Timeout;

            if (settings.FailoverPartner != null)
                builder.FailoverPartner = settings.FailoverPartner;

            builder.MultiSubnetFailover = settings.Failover;
            builder.WorkstationID = Environment.MachineName;
            builder.ApplicationName = Assembly.GetExecutingAssembly().FullName;

            string connString = builder.ConnectionString;
            return connString;
        }

        private void CallDatabase(string connString, ConsoleSettings settings)
        {
            AnsiConsole.MarkupLine($"[teal]{DateTime.Now.ToLocalTime()}[/] Connecting to {settings.Server.EscapeMarkup()}... ");

            try
            {

                using (SqlConnection connection = new SqlConnection(connString))
                {
                    connection.Open();
                    var sb = new StringBuilder();
                    using (SqlCommand command = new SqlCommand(settings.SQLCommand, connection))
                    {
                        // Add parameter if the query contains @DatabaseName placeholder
                        if (settings.SQLCommand.Contains("@DatabaseName"))
                        {
                            command.Parameters.AddWithValue("@DatabaseName", settings.Database);
                        }

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    if (reader.GetValue(i) != DBNull.Value)
                                        sb.Append($"{reader.GetName(i)}: {Convert.ToString(reader.GetValue(i))}  ");
                                //sb.AppendLine();
                            }
                        }
                    }
                    AnsiConsole.MarkupLine($"  [green]SUCCESS[/] {sb.ToString().EscapeMarkup()}");
                }
            }
            catch (SqlException ex)
            {
                AnsiConsole.MarkupLine($"  [red]ERROR: {ex.Message.EscapeMarkup()}[/] ");
            }

        }

        public PingCommand(ILogger<PingCommand> logger)
        {
            Logger = logger;
        }
    }
}