# SQLServerPing

Ping a SQL Server. For example to test connectivity through a firewall, testing a failover cluster or testing permissions.

This is a .NET console app and should run fine on [all supported operating systems](https://github.com/dotnet/core/blob/main/release-notes/7.0/supported-os.md): Windows, Linux and macOS.

## USAGE:
    SQLPing <Server> [OPTIONS]

## ARGUMENTS:
    <Server>    Server host or ip number. For non-standard port use the format "server,port"

## OPTIONS:
                             DEFAULT
    -h, --help                          Prints help information
    -d, --database           master     Database name
    -u, --username                      SQL Username, leave empty for Windows integrated security
    -p, --password                      Password (NOT RECOMMENDED - see Security Notes below)
    -t, --timeout            3          Connection timeout in seconds
    -c, --command                       SQL Command (default will return database information)
    -f, --failover                      This is a failover cluster / availability group, use MultiSubnetFailover=true
    -a, --failoverpartner               Use a custom failover partner
    -n, --nonstop                       Set this to true to continously ping the server. Default is ping once
    -w, --wait <SECONDS>     10         How long to wait, in seconds, between non-stop pings

## SECURITY NOTES:

### Credential Handling

SQLPing supports multiple ways to provide credentials, listed from most secure to least secure:

**1. Interactive Password Prompt (RECOMMENDED)**
```bash
# Provide username, password will be prompted securely
SQLPing myserver -u myusername
Password: ********
```

**2. Environment Variables (RECOMMENDED)**
```bash
# Set environment variables (more secure than command line)
export SQLPING_USERNAME="myusername"
export SQLPING_PASSWORD="mypassword"
SQLPing myserver
```

**3. Command Line Arguments (NOT RECOMMENDED)**
```bash
# WARNING: Password visible in process list and shell history
SQLPing myserver -u myusername -p mypassword
```

**Important Security Considerations:**
- Passwords provided via command line (`-p`) are visible in:
  - Shell history
  - Process lists (`ps aux`, Task Manager)
  - Logs and monitoring tools
- Always prefer environment variables or interactive prompts for production use
- Connection strings in output have passwords redacted automatically


# Todo

- [ ] Add terse output option, just returning the output of the command
- [ ] Add custom connection string option
