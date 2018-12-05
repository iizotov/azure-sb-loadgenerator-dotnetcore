# Multi-threaded load Generator for Azure Service Bus and Event Hubs
> Note: the old unmaintained version is still available [here](https://github.com/iizotov/azure-sb-loadgenerator)

[![Build status](https://dev.azure.com/igizotov/azure-sb-loadgenerator-dotnetcore/_apis/build/status/Build%20Docker%20Image)](https://dev.azure.com/igizotov/azure-sb-loadgenerator-dotnetcore/_build/latest?definitionId=9)

This .NET Core app generates a random payload stream and pushes it into an Azure Service Bus Queue / Topic or an Azure Event Hub. In addition to controlling the app via command line arguments, the tool supports adjusting some parameters interactively:

* Press Q or A to increase/decrease the throughput target in messages per second. Setting it to 0 turns off all rate limiting 
* Press W or S to increase/decrease batch size (+/- 10 messages)
* Press Escape to cancel all threads and gracefully exit

Payload options: 
* JSON consistsing of a UTC timestamp and a random string payload of an arbitrary length
    ```json
    {"dt":1542128101310,"payload":"FPBNFAGYABFPWWOAQALJKCIBJIFVAMLXHZO"}
    ```
* random string of an arbitrary length
    ```
    FPBNFAGYABFPWWOAQALJKCIBJIFVAMLXHZO
    ```
> Note: it is recommended to append `;TransportType=Amqp` to your connection string to enforce [AMQP](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-dotnet) for better performance

**Software prerequisites:**
1. [.NET Core Runtime 2.2+](https://www.microsoft.com/net/download)
2. (Optional) [Visual Studio Code](https://code.visualstudio.com/) and [.NET Core SDK 2.2+](https://www.microsoft.com/net/download) if you wish to build from source
3. (Optional) [Docker](https://docs.docker.com/install/)

## Running this sample
1. Download and extrack the [latest release](https://github.com/iizotov/azure-sb-loadgenerator-dotnetcore/releases/latest) or grab a docker image from [Docker Hub](https://hub.docker.com/r/iizotov/azure-sb-loadgenerator-dotnetcore/)
2. Run `dotnet loadgenerator.dll <parameters>`

### Command Line Parameters
```
  --service                 (Default: eh) Valid options: eh or sb for Azure
                            Event Hub or Service Bus respectively.

  -s, --size                (Default: 35) How many characters of random payload
                            to generate

  -j, --json                (Default: true) Generate json payload with a random 
                            string or just a random string itself

  -d, --dry-run             (Default: false) Execute a dry run (messages will be
                            generated but none will be sent)

  --terminate-after         (Default: 0) Terminates execution after N seconds

  -m, --messagestosend      (Default: 100) Messages to send in each thread 
                            before termination, 0 for infinity

  -c, --connectionstring    Required. Event Hub or Service Bus Namespace 
                            connection string

  -n, --name                Event Hub or Queue or Topic Name. Alternative to ...;
                            EntityPath=... in the connection string

  --checkpoint              (Default: 1000) Log to console every N milliseconds

  -t, --throughput          (Default: 100) Target throughput, messages/sec, 0 for 
                            unlimited

  --batchsize               (Default: 1) Determines the size of the batch if 
                            using batch mode

  --help                    Display this help screen.

  --version                 Display version information.
```

### Example
The following command will generate uncapped load in batches of 100 json messages, each message with a payload of 35 random characters. The code will run infitinely until a SIGTERM / SIGKILL is received or Escape is hit. Some parameters can be interactively adjusted in-flight

```bash
dotnet loadgenerator.dll --service "eh" \
    --connectionstring "Endpoint=sb://eh-af-perftest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=****;TransportType=Amqp;" \
    --name "eh-ah-perftest-32"

-----INITIAL PARAMETERS-----
Execution started: 21/11/2018 5:38:16 AM
Service: eh
MessageSize: 35
GenerateJson: True
DryRun: False
TerminateAfter: 0
MessagesToSend: 0
ConnectionString: Endpoint=sb://eh-af-perftest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=***;TransportType=Amqp;
EntityPath: eh-ah-perftest-32
Checkpoint: 1000
TargetThroughput: 100
BatchSize: 1

Sample payload: {'dt':1542818296213,'payload':'AEPTGRZSZRITTKSYERKCBKQQLGNWEWVOWFP'}
-----INTERACTIVITY-----
Press Q/A to increase/decrease the target throughput
Press W/S to increase/decrease batch size
Press Escape to cancel all threads and gracefully exit
-----EXECUTION LOG FOLLOWS-----

[2018-11-21T05:38:17.284] Target: throughput 100 msg/sec, volume: 0, batch size: 1, msg size: 68
[2018-11-21T05:38:17.284] Stats: 100 msg sent, long avg 97 msg/sec, short avg 0 msg/sec

[2018-11-21T05:38:18.262] Target: throughput 100 msg/sec, volume: 0, batch size: 1, msg size: 68
[2018-11-21T05:38:18.262] Stats: 200 msg sent, long avg 99.5 msg/sec, short avg 53.4 msg/sec

[2018-11-21T05:38:19.257] Target: throughput 100 msg/sec, volume: 0, batch size: 1, msg size: 68
[2018-11-21T05:38:19.257] Stats: 300 msg sent, long avg 100.3 msg/sec, short avg 70 msg/sec

[2018-11-21T05:38:20.263] Target: throughput 100 msg/sec, volume: 0, batch size: 1, msg size: 68
[2018-11-21T05:38:20.263] Stats: 400 msg sent, long avg 100.5 msg/sec, short avg 78 msg/sec

[2018-11-21T05:38:21.253] Target: throughput 100 msg/sec, volume: 0, batch size: 1, msg size: 68
[2018-11-21T05:38:21.253] Stats: 500 msg sent, long avg 100.2 msg/sec, short avg 82.4 msg/sec
``` 
### Notes re capping throughput
The target throughput setting uses the call rate limiting approach and is particularly accurate with small batch size (1..5). It's great for 'warming up' the system for testing before removing all rate limiters. Smaller batch size is likely going to saturate your CPU first. Overall, the algorithm is pretty efficient and can push out 20k msg/sec in 100-message batches on a single-CPU machine.

A simple script that runs a 5-minute warm-up cycle at 100 msg/sec before removing the throughput cap and sending through 100-message batches:

```bash
#!/bin/bash

CONNECTION_STRING="Endpoint=sb://.....;TransportType=Amqp;"
ENTITY_NAME="...."

dotnet loadgenerator.dll -c $CONNECTION_STRING -t 100 --terminate-after 300 -b 1 -n $ENTITY_NAME
dotnet loadgenerator.dll -c $CONNECTION_STRING -t 0 -b 100 -n $ENTITY_NAME

```

## Disclaimers
The code included in this sample is not intended to be a set of best practices on how to build scalable enterprise grade applications. This is beyond the scope of this quick start sample.

## Related Links
For more information, see these articles:
- [Best Practices for performance improvements using Service Bus Messaging](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements)

## TODO
* Add a PID control loop and throttling to enable auto-tuning of parameters to achieve desired throughput more accurately