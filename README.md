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
    -p, --password                      Password
    -t, --timeout            3          Connection timeout in seconds
    -c, --command                       SQL Command (default will return database information)
    -f, --failover                      This is a failover cluster / availability group, use MultiSubnetFailover=true
    -a, --failoverpartner               Use a custom failover partner
    -n, --nonstop                       Set this to true to continously ping the server. Default is ping once
    -w, --wait <SECONDS>     10         How long to wait, in seconds, between non-stop pings


# Todo

- [ ] Add terse output option, just returning the output of the command
- [ ] Add custom connection string option
