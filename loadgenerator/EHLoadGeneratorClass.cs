using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace LoadGeneratorDotnetCore
{
    class EHLoadGeneratorClass : LoadGenerateeClass
    {
        private EventHubsConnectionStringBuilder ehConnectionString;
        private string entityPath;
        private EventHubClient sendClient;
        public EHLoadGeneratorClass(
            string connectionString, string entityPath) : base(connectionString)
        {
            this.entityPath = entityPath;

            try
            {
                this.ehConnectionString = new EventHubsConnectionStringBuilder(base.connectionString);
            }
            catch (Exception e)
            {
                throw e;
            }
            // Successfully parsed the supplied connection string but need to ensure that for Event Hubs either
            // ...;EntityPath=... exists either in the conn string or in executionOptions
            if (String.IsNullOrWhiteSpace(this.ehConnectionString.EntityPath))
            {
                if (String.IsNullOrWhiteSpace(entityPath))
                {
                    throw new Exception("Please specify event hub name");
                }
                this.ehConnectionString.EntityPath = entityPath;
            }
            this.sendClient = EventHubClient.CreateFromConnectionString(ehConnectionString.ToString());
            this.sendClient.RetryPolicy = RetryPolicy.NoRetry;
        }
        public override Task GenerateBatchAndSend(int batchSize, bool dryRun, CancellationToken cancellationToken, Func<byte[]> loadGenerator)
        {
            List<EventData> batchOfMessages = new List<EventData>();
            for (int i = 0; i < batchSize && !cancellationToken.IsCancellationRequested; i++)
            {
                batchOfMessages.Add(new EventData(loadGenerator()));
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