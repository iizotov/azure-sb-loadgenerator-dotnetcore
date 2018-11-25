using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace LoadGeneratorDotnetCore
{
    class SBLoadGeneratorClass : LoadGenerateeClass
    {
        private ServiceBusConnectionStringBuilder sbConnectionString;
        private string entityPath;
        private QueueClient sendClient;
        public SBLoadGeneratorClass(
            string connectionString, string entityPath) : base(connectionString)
        {
            this.entityPath = entityPath;

            try
            {
                this.sbConnectionString = new ServiceBusConnectionStringBuilder(connectionString);
            }
            catch (Exception e)
            {
                throw e;
            }
            // Successfully parsed the supplied connection string but need to ensure that for Event Hubs either
            // ...;EntityPath=... exists either in the conn string or in executionOptions
            if (String.IsNullOrWhiteSpace(sbConnectionString.EntityPath))
            {
                if (String.IsNullOrWhiteSpace(entityPath))
                {
                    throw new Exception("Please specify event hub name");
                }
                this.sbConnectionString.EntityPath = entityPath;
            }
            this.sendClient = new QueueClient(sbConnectionString, retryPolicy: RetryPolicy.NoRetry);
        }

        public override Task GenerateBatchAndSend(int batchSize, bool dryRun, CancellationToken cancellationToken, Func<byte[]> loadGenerator)
        {
            List<Message> batchOfMessages = new List<Message>();
            for (int i = 0; i < batchSize && !cancellationToken.IsCancellationRequested; i++)
            {
                batchOfMessages.Add(new Message(loadGenerator()));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                var tcs = new TaskCompletionSource<int>();
                tcs.TrySetCanceled();
                return tcs.Task;
            }
            if (!dryRun)
            {
                return this.sendClient.SendAsync(batchOfMessages);
            }
            else
            {
                return Task.CompletedTask;
            }
        }
    }
}