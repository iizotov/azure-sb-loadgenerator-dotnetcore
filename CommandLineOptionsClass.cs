using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private int threads;
        [Option('t', "threads", Required = false,
            HelpText = "Threads to spawn.", Default = 5)]
        public int Threads
        {
            get
            {
                return threads;
            }
            set
            {
                threads = value >= 0 ? value : 0;
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

        private Int64 messagesToSend { get; set; }
        [Option('m', "messagestosend", Required = false,
            HelpText = "Messages to send in each thread before termination, 0 for infinity", Default = 0)]
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
            HelpText = "Log to console every N milliseconds", Default = 300)]
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

        private int batchSize;
        [Option("batchsize", Required = false,
            HelpText = "Determines the size of the batch if using batch mode", Default = 100)]
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
