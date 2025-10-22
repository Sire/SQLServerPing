# SQLServerPing

Ping a SQL Server. For example to test connectivity through a firewall, testing a failover cluster or testing permissions.

This is a .NET console app and should run fine on [all supported operating systems](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md): Windows, Linux and macOS.

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
    -n, --nonstop                       Set this to true to continuously ping the server. Default is ping once
    -w, --wait <SECONDS>     10         How long to wait, in seconds, between non-stop pings
    --encrypt <VALUE>        true       Encryption mode: true|false|strict
    -T, --trust-server-certificate      Trust server certificate without validation (DEV ONLY)
    -H, --hostname-in-certificate       Override hostname for TLS certificate validation
    --no-tnir                           Disable TransparentNetworkIPResolution

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

## TLS/ENCRYPTION OPTIONS:

### Connection Encryption

By default, SQL Server connections use encryption. You can control this behavior with the following options:

**--encrypt <true|false|strict>**
- `true` (default): Encrypted connection with certificate validation
- `false`: No encryption (not recommended for production)
- `strict`: Strictest encryption mode (requires SQL Server 2022+)

**-T, --trust-server-certificate**
- For development/testing environments only
- Bypasses certificate validation but maintains encryption
- WARNING: Vulnerable to man-in-the-middle attacks in production

**-H, --hostname-in-certificate <name>**
- Override the expected hostname in the server's TLS certificate
- Useful when connecting via IP address but certificate contains a specific hostname
- Must match the Subject Alternative Name (SAN) or CN in the server's certificate

**--no-tnir**
- Disables TransparentNetworkIPResolution
- Prevents automatic hostname-to-IP substitution that can break TLS name matching
- Recommended when using `--hostname-in-certificate`

### Examples

```bash
# Connect with strict encryption (SQL Server 2022+)
SQLPing myserver -u myuser --encrypt strict

# Development: connect via IP with self-signed certificate
SQLPing 10.0.0.5 -u myuser -T --hostname-in-certificate sqlserver.local

# Production: connect via IP with valid certificate, specify expected hostname
SQLPing 10.0.0.5 -u myuser -H sqlserver.company.com --no-tnir

# Disable encryption (not recommended)
SQLPing myserver -u myuser --encrypt false
```

# Todo

- [ ] Add terse output option, just returning the output of the command
- [ ] Add custom connection string option
