using System;
using Microsoft.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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

            // Apply secure credential handling
            ApplySecureCredentials(settings);

            var connString = GetConnectionString(settings);

            AnsiConsole.MarkupLine($"ConnectionString: [teal]{RedactConnectionString(connString).EscapeMarkup()}[/]");
            AnsiConsole.MarkupLine($"SQL Query       : [teal]{settings.SQLCommand.EscapeMarkup()}[/]");

            // Helpful warning if using IP
            if (LooksLikeIp(settings.Server)
                && (settings.TrustServerCertificate != true)
                && string.IsNullOrWhiteSpace(settings.HostNameInCertificate))
            {
                AnsiConsole.MarkupLine("[yellow]Hint:[/] You're connecting by IP. TLS certificate name validation usually fails with IPs unless the cert has the IP in SAN. Use a DNS name, set [teal]--hostname-in-certificate[/], or [teal]--trust-server-certificate true[/] for dev.");
            }

            bool running = true;

            while (running)
            {
                int sec = settings.Wait;
                await AnsiConsole.Status()
                    .AutoRefresh(true)
                    .Spinner(Spinner.Known.Dots)
                    .SpinnerStyle(Style.Parse("green bold"))
                    .StartAsync("Please wait...", async ctx =>
                    {
                        ctx.Status($"Trying to connect to server [teal]{settings.Server.EscapeMarkup()}[/]...");
                        await CallDatabaseAsync(connString, settings);

                        ctx.Status($"Waiting [teal]{sec}[/] seconds...");

                        if (settings.NonStop)
                            await Task.Delay(TimeSpan.FromSeconds(sec));
                        else
                            running = false;
                    });
            }

            return await Task.FromResult(0);
        }

        // Validate as part of the command. This is a good way of validating options if you require any injected services.
        public override ValidationResult Validate(CommandContext context, ConsoleSettings settings)
        {
            return ValidationResult.Success();
        }

        private static void ApplySecureCredentials(ConsoleSettings settings)
        {
            // Check for credentials from environment variables first
            var envUsername = Environment.GetEnvironmentVariable("SQLPING_USERNAME");
            var envPassword = Environment.GetEnvironmentVariable("SQLPING_PASSWORD");

            bool passwordProvidedViaCommandLine = !string.IsNullOrEmpty(settings.Password);

            // Use environment variables if command line values are not provided
            if (string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(envUsername))
            {
                settings.Username = envUsername;
                AnsiConsole.MarkupLine("[yellow]Using username from SQLPING_USERNAME environment variable[/]");
            }

            if (string.IsNullOrEmpty(settings.Password) && !string.IsNullOrEmpty(envPassword))
            {
                settings.Password = envPassword;
                AnsiConsole.MarkupLine("[yellow]Using password from SQLPING_PASSWORD environment variable[/]");
            }

            // If username is provided but password is not, prompt for password
            if (!string.IsNullOrEmpty(settings.Username) && string.IsNullOrEmpty(settings.Password))
            {
                settings.Password = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Password:[/]")
                        .PromptStyle("red")
                        .Secret());
            }

            // Warn if password was provided via command line (security risk)
            if (passwordProvidedViaCommandLine)
            {
                AnsiConsole.MarkupLine("[yellow]WARNING: Password provided via command line is visible in process list and shell history.[/]");
                AnsiConsole.MarkupLine("[yellow]Consider using SQLPING_PASSWORD environment variable or interactive prompt instead.[/]");
            }
        }

        private static string GetConnectionString(ConsoleSettings settings) {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
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

            // TLS related options
            if (settings.TrustServerCertificate.HasValue)
                builder.TrustServerCertificate = settings.TrustServerCertificate.Value;

            if (!string.IsNullOrWhiteSpace(settings.Encrypt))
            {
                var encValue = settings.Encrypt.Trim().ToLowerInvariant();
                if (encValue is "true" or "false" or "strict")
                {
                    builder["Encrypt"] = settings.Encrypt;
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Invalid --encrypt value. Use true|false|strict. Ignoring.[/]");
                }
            }

            if (!string.IsNullOrWhiteSpace(settings.HostNameInCertificate))
            {
                builder["HostNameInCertificate"] = settings.HostNameInCertificate;
            }

            if (settings.NoTransparentNetworkIPResolution)
            {
                builder["TransparentNetworkIPResolution"] = "false";
            }

            string connString = builder.ConnectionString;
            return connString;
        }

        private static string RedactConnectionString(string connectionString)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);

                // Redact password if present
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    builder.Password = "***REDACTED***";
                }

                return builder.ConnectionString;
            }
            catch
            {
                // If parsing fails, return a generic redacted message
                return "***CONNECTION STRING REDACTED***";
            }
        }

        private async Task CallDatabaseAsync(string connString, ConsoleSettings settings)
        {
            AnsiConsole.MarkupLine($"[teal]{DateTime.Now.ToLocalTime()}[/] Connecting to {settings.Server.EscapeMarkup()}... ");

            try
            {

                await using (SqlConnection connection = new SqlConnection(connString))
                {
                    await connection.OpenAsync();

                    // Ensure we are in the requested database even if Initial Catalog was not applied for any reason
                    if (!string.IsNullOrWhiteSpace(settings.Database))
                    {
                        try
                        {
                            connection.ChangeDatabase(settings.Database);
                        }
                        catch (Exception dbEx)
                        {
                            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to change database to '[teal]{settings.Database.EscapeMarkup()}[/]': {dbEx.Message.EscapeMarkup()}");
                        }
                    }

                    var sb = new StringBuilder();
                    await using (SqlCommand command = new SqlCommand(settings.SQLCommand!, connection))
                    {
                        // Add parameter if the query contains @DatabaseName placeholder
                        if (settings.SQLCommand != null && settings.SQLCommand.Contains("@DatabaseName"))
                        {
                            command.Parameters.AddWithValue("@DatabaseName", settings.Database);
                        }

                        await using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    if (reader.GetValue(i) != DBNull.Value)
                                        sb.Append($"{reader.GetName(i)}: {Convert.ToString(reader.GetValue(i))}  ");
                            }
                        }
                    }

                    // Try to show connection encryption info, but ignore permission errors
                    string info;
                    string currentDbName = connection.Database;
                    try
                    {
                        await using var infoCmd = new SqlCommand(
                            "SELECT encrypt_option, net_transport FROM sys.dm_exec_connections WHERE session_id = @@SPID;", connection);
                        await using var infoReader = await infoCmd.ExecuteReaderAsync();
                        if (await infoReader.ReadAsync())
                        {
                            var encryptOption = Convert.ToString(infoReader["encrypt_option"]);
                            var transport = Convert.ToString(infoReader["net_transport"]);
                            info = $" (db={currentDbName}, encrypt_option={encryptOption}, net_transport={transport})";
                        }
                        else
                        {
                            info = $" (db={currentDbName})";
                        }
                    }
                    catch (SqlException)
                    {
                        info = $" (db={currentDbName}, connection details unavailable: requires VIEW SERVER STATE)";
                    }

                    AnsiConsole.MarkupLine($"  [green]SUCCESS[/] {sb.ToString().EscapeMarkup()}{info}");
                }
            }
            catch (SqlException ex)
            {
                AnsiConsole.MarkupLine($"  [red]ERROR: {ex.Message.EscapeMarkup()}[/] ");

                if (ex.Message.IndexOf("certificate chain was issued by an authority that is not trusted", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    AnsiConsole.MarkupLine("[yellow]Troubleshooting tips:[/]");
                    AnsiConsole.MarkupLine("- Ensure SQL Server uses a certificate trusted by this machine's Trusted Root store.");
                    AnsiConsole.MarkupLine("- Connect using a DNS name that matches the certificate's CN/SAN.");
                    AnsiConsole.MarkupLine("- Or set [teal]--hostname-in-certificate[/] to the certificate subject.");
                    AnsiConsole.MarkupLine("- For dev only, use [teal]--trust-server-certificate true[/] to bypass validation.");
                    AnsiConsole.MarkupLine("- If name keeps flipping to an IP, try [teal]--no-tnir[/].");
                }
            }

        }

        private static bool LooksLikeIp(string server)
        {
            // Accept formats: "x.x.x.x" or "x.x.x.x,port"
            var parts = server.Split('\\')[0]; // ignore instance suffix
            var ipAndPort = parts.Split(',');
            var ip = ipAndPort[0].Trim();
            return Regex.IsMatch(ip, @"^\d{1,3}(\.\d{1,3}){3}$");
        }

        public PingCommand(ILogger<PingCommand> logger)
        {
            Logger = logger;
        }
    }
}