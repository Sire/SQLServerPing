# SQLServerPing

## USAGE:
    SQLPing <Server> [OPTIONS]

## ARGUMENTS:
    <Server>    Server host or ip number

## OPTIONS:
                             DEFAULT
    -h, --help                          Prints help information
    -d, --database           master     Database name
    -u, --username                      SQL Username, leave empty for Windows integrated security
    -p, --password                      Password
    -t, --timeout            3          Connection timeout in seconds
    -c, --command                       SQL Command
    -f, --failover                      This is a failover cluster / availability group, use MultiSubnetFailover=true
    -a, --failoverpartner               Use a custom failover partner
    -n, --nonstop                       Set this to true to continously ping the server. Default is ping once
    -w, --wait <SECONDS>     10         How long to wait, in seconds, between non-stop pings


# Todo

- [ ] Add terse output option, just returning the output of the command
- [ ] Add custom connection string option
