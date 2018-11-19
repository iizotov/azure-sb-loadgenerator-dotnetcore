# Multi-threaded load Generator for Azure Service Bus and Event Hubs
> Note: the old unmaintained version is still available [here](https://github.com/iizotov/azure-sb-loadgenerator)

This .NET Core console app generates random payloads and inserts it into an Azure Service Bus Queue or a Topic or an Azure Event Hub. In addition to command line arguments, the tool now supports adjusting load parameters interactively:

* Press Q or A to increase/decrease the number of threads
* Press W or S to increase/decrease payload size in (+/- 10 characters)
* Press E or D to increase/decrease batch size (+/- 10 messages)
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
> Note: it is recommended to add `;TransportType=Amqp` to your connection string to enforce [AMQP](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-amqp-dotnet) for better performance

**Software prerequisites:**
1. [.NET Core 2.1+](https://www.microsoft.com/net/download)
2. (Optional) [Visual Studio Code](https://code.visualstudio.com/)


## Running this sample
1. Clone this repository or download the zip file.
2. Open the folder and run `dotnet run -c Release` to build and run or grab one of the prebuilt releases
   
### Command Line Arguments
```
  --service                 (Default: eh) Valid options: eh or sb for Azure
                            Event Hub or Service Bus respectively.

  -t, --threads             (Default: 5) Threads to spawn.

  -s, --size                (Default: 35) How many characters of random payload
                            to generate

  -j, --json                (Default: true) Generate json payload with a random 
                            string or just a random string itself

  -m, --messagestosend      (Default: 100) Messages to send in each thread 
                            before termination, 0 for infinity

  -c, --connectionstring    Required. Event Hub or Service Bus Namespace 
                            connection string

  -n, --name                Event Hub or Queue or Topic Name. Alternative to ...;
                            EntityPath=... in the connection string

  --checkpoint              (Default: 300) Log to console every N milliseconds

  --batchsize               (Default: 100) Determines the size of the batch if 
                            using batch mode

  --help                    Display this help screen.

  --version                 Display version information.
```

### Example
The following command will generate load in batches of 100 json messages using 5 parallel threads, each message includes a payload of 35 random characters. The code will run infitinely until a SIGTERM / SIGKILL is received or the escape key is hit. Parameters can be interactively adjusted in-flight

```
dotnet run -- --service "eh" --connectionstring "Endpoint=sb://eh-af-perftest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=****;TransportType=Amqp;" --name "eh-ah-perftest-32"

-----INITIAL PARAMETERS-----
Execution started: 13/11/2018 6:58:30 AM
Service: eh
Threads: 5
MessageSize: 35
GenerateJson: True
MessagesToSend: 0
ConnectionString: Endpoint=sb://eh-af-perftest.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=****;TransportType=Amqp;
EntityPath: eh-ah-perftest-32
Checkpoint: 300
BatchSize: 100

Sample payload: {'dt':1542131910742,'payload':'FYYWVMSLFKATZDOKHQOYHTAJTQURPFYVVCC'}
-----INTERACTIVITY-----
Press Q/A to increase/decrease the number of threads
Press W/S to increase/decrease payload size
Press E/D to increase/decrease batch size
Press Escape to cancel all threads and gracefully exit
-----EXECUTION LOG FOLLOWS-----

Target threads: 5 Active threads: 5
Thread 15b3f4e0-72e8-435c-bb29-08b6d074799c is Running, sent 2200 msgs, avg speed: 1220.3 msg/sec
Thread 44d0df50-1f5a-4165-b92e-419425a5e650 is Running, sent 0 msgs, avg speed: 0 msg/sec
Thread 113d7799-f0c0-4c2e-b149-a8c53a06a555 is Running, sent 3100 msgs, avg speed: 1292 msg/secec
Thread 276d9c64-1081-4738-a6fd-40b8121d58d7 is Running, sent 3200 msgs, avg speed: 1187 msg/secec
Thread 2b03a503-8abf-494b-8dac-dcdf26f84e30 is Running, sent 3700 msgs, avg speed: 1241.6 msg/sec
Sent: 12200 msgs, avg speed: 4074.9 msg/sec
``` 

## Disclaimers
The code included in this sample is not intended to be a set of best practices on how to build scalable enterprise grade applications. This is beyond the scope of this quick start sample.

## Related Links
For more information, see these articles:
- [Best Practices for performance improvements using Service Bus Messaging](https://docs.microsoft.com/en-us/azure/service-bus-messaging/service-bus-performance-improvements)

## TODO
* Tidy up the code, add comments throughout
* Tidy up output
* Add a PID control loop and throttling to enable auto-tuning of parameters to achieve desired throughput
* Add message size accumulator (dependent on .net core AMQP implemetation)