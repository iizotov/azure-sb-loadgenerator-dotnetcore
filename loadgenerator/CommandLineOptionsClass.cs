using System;
using CommandLine;

namespace LoadGeneratorDotnetCore
{
    enum ServiceEnum { eh, sb };
    class ExecutionOptionsClass
    {
        private string service;

        [Option("service", Required = false,
            HelpText = "Valid options: eh or sb for Azure Event Hub or Service Bus respectively.", Default = "eh")]
        public string Service
        {
            get
            {
                return this.service;
            }
            set
            {
                this.service = value.ToLower();
            }
        }

        private int messageSize;
        [Option('s', "size", Required = false,
            HelpText = "How many characters of random payload to generate", Default = 35)]
        public int MessageSize
        {
            get
            {
                return messageSize;
            }
            set
            {
                messageSize = value >= 0 ? value : 0;
            }
        }

        [Option('j', "json", Required = false,
            HelpText = "Generate json payload with a random string or just a random string itself", Default = true)]
        public bool GenerateJson { get; set; }

        [Option('d', "dry-run", Required = false,
            HelpText = "Execute a dry run (messages will be generated but none will be sent)", Default = false)]
        public bool DryRun { get; set; }

        private int terminateAfter;
        [Option("terminate-after", Required = false,
            HelpText = "Terminates execution after N seconds", Default = 0)]
        public int TerminateAfter
        {
            get
            {
                return terminateAfter;
            }
            set
            {
                terminateAfter = value >= 0 ? value : 0;
            }
        }

        private Int64 messagesToSend { get; set; }
        [Option('m', "messagestosend", Required = false,
            HelpText = "Messages to send, 0 for infinity", Default = 0)]
        public Int64 MessagesToSend
        {
            get
            {
                return messagesToSend;
            }
            set
            {
                messagesToSend = value >= 0 ? value : 0;
            }
        }

        [Option('c', "connectionstring", Required = true,
            HelpText = "Event Hub or Service Bus Namespace connection string")]
        public string ConnectionString { get; set; }

        [Option('n', "name", Required = false,
            HelpText = "Event Hub or Queue or Topic Name. Alternative to ...;EntityPath=... in the connection string")]
        public string EntityPath { get; set; }

        private int checkpoint;
        [Option("checkpoint", Required = false,
            HelpText = "Log to console every N milliseconds", Default = 1000)]
        public int Checkpoint
        {
            get
            {
                return checkpoint;
            }
            set
            {
                checkpoint = value >= 0 ? value : 0;
            }
        }

        private int targetThroughput;
        [Option('t', "throughput", Required = false,
            HelpText = "Target throughput, messages/sec, 0 for unlimited", Default = 100)]
        public int TargetThroughput
        {
            get
            {
                return targetThroughput;
            }
            set
            {
                targetThroughput = value >= 0 ? value : 0;
            }
        }

        private int batchSize;
        [Option('b', "batchsize", Required = false,
            HelpText = "Batches messages if required batch if using batch mode", Default = 1)]
        public int BatchSize
        {
            get
            {
                return batchSize;
            }
            set
            {
                batchSize = value >= 0 ? value : 0;
            }
        }
    }
}
