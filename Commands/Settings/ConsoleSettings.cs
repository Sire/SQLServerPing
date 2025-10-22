using System.ComponentModel;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SQLServerPing.Commands.Settings
{
    public class ConsoleSettings : CommandSettings
    {
        public override ValidationResult Validate()
        {
            if (Wait < 1)
                return ValidationResult.Error($"You need to wait at least 1 seconds between pings");
            //if (Timeout < 1)
            //    return ValidationResult.Error($"Timeout ");

            return ValidationResult.Success();
        }

        [CommandArgument(0, "<Server>")]
        [Description("Server host or ip number. For non-standard port use the format \"server,port\"")]
        public string Server { get; set; } = "";

        [CommandOption("--database|-d")]
        [Description("Database name")]
        [DefaultValue("master")]
        public string Database { get; set; } = "master";

        [CommandOption("--username|-u")]
        [Description("SQL Username, leave empty for Windows integrated security")]
        public string Username { get; set; } = string.Empty;

        [CommandOption( "--password|-p")]
        [Description("Password")]
        public string Password { get; set; } = string.Empty;

        [CommandOption( "--timeout|-t")]
        [Description("Connection timeout in seconds")]
        [DefaultValue(3)]
        public int Timeout { get; set; }
        
        [CommandOption( "--command|-c")]
        [Description("Custom SQL Command to run (default will return database information)")]
        public string? SQLCommand { get; set; } = null;

        [CommandOption("--failover|-f")]
        [Description("This is a failover cluster / availability group, use MultiSubnetFailover=true")]
        [DefaultValue(false)]
        public bool Failover { get; set; } = false;

        [CommandOption("--failoverpartner|-a")]
        [Description("Use a custom failover partner (works with Database Mirroring, not Availability Groups)")]
        public string? FailoverPartner { get; set; }

        [CommandOption("--nonstop|-n")]
        [Description("Set this to true to continously ping the server. Default is ping once")]
        [DefaultValue(false)]
        public bool NonStop { get; set; } = false;

        [CommandOption("--wait|-w <seconds>")]
        [Description("How long to wait, in seconds, between non-stop pings")]
        [DefaultValue(10)]
        public int Wait { get; set; }

        // TLS-related options
        [CommandOption("--encrypt <true|false|strict>")]
        [Description("Encrypt mode: true|false|strict. Default follows Microsoft.Data.SqlClient (true).")]
        public string? Encrypt { get; set; }

        [CommandOption("--trust-server-certificate|-T <true|false>")]
        [Description("Set TrustServerCertificate=true (DEV ONLY). Bypasses cert validation but keeps encryption.")]
        public bool? TrustServerCertificate { get; set; }

        [CommandOption("--hostname-in-certificate|-H <name>")]
        [Description("Override HostNameInCertificate for TLS validation (use the subject/SAN on the server certificate).")]
        public string? HostNameInCertificate { get; set; }

        [CommandOption("--no-tnir")]
        [Description("Disable TransparentNetworkIPResolution to avoid host->IP substitution that breaks TLS name matching.")]
        [DefaultValue(false)]
        public bool NoTransparentNetworkIPResolution { get; set; } = false;



        //[CommandOption("--command-option-value <value>")]
        //[Description("Command option value.")]
        //[ValidateString]
        //public string? CommandOptionValue { get; set; }
    }
}