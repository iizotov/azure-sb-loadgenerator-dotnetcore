using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace LoadGeneratorDotnetCore
{
    class EHLoadGeneratorClass : LoadGenerateeClass
    {
        private EventHubsConnectionStringBuilder ehConnectionString;
        private string entityPath;
        public EHLoadGeneratorClass(
            string connectionString, string entityPath,
            int payloadSize, bool generateJsonPayload,
            int batchSize) : base(connectionString, batchSize, payloadSize, generateJsonPayload)
        {
            this.entityPath = entityPath;

            try
            {
                ehConnectionString = new EventHubsConnectionStringBuilder(connectionString);
            }
            catch (Exception e)
            {
                throw e;
            }
            // Successfully parsed the supplied connection string but need to ensure that for Event Hubs either
            // ...;EntityPath=... exists either in the conn string or in executionOptions
            if (String.IsNullOrWhiteSpace(ehConnectionString.EntityPath))
            {
                if (String.IsNullOrWhiteSpace(entityPath))
                {
                    throw new Exception("Please specify event hub name");
                }
                ehConnectionString.EntityPath = entityPath;
            }
        }
        public override void GenerateWorkload(Guid threadId, CancellationToken cancellationToken)
        {
            Int64 queuedMessageCount = 0;
            List<EventData> batchOfMessages = new List<EventData>();
            DateTime timerStart;
            EventHubClient sendClient = EventHubClient.CreateFromConnectionString(ehConnectionString.ToString());
            try
            {
                timerStart = DateTime.Now;
                while (!cancellationToken.IsCancellationRequested)
                {
                    EventData randomPayload = new EventData(GeneratePayload());
                    batchOfMessages.Add(randomPayload);
                    // Check if batch is ready to send, also checking if it's the final batch
                    if (queuedMessageCount % batchSize == 0 && queuedMessageCount > 0)
                    {
                        
                        sendClient.SendAsync(batchOfMessages)
                        .ContinueWith((t) =>
                        {
                            messagesSentByThread[threadId] += batchSize;
                            bytesSentByThread[threadId] = 0;
                            // just a stub for now, next version of the Event Hub SDK will include AMQP size

                        }, TaskContinuationOptions.OnlyOnRanToCompletion);
                        // messagesSentByThread[threadId] = queuedMessageCount;
                        // bytesSentByThread[threadId] = 0;
                        batchOfMessages.Clear();
                    }
                    queuedMessageCount++;
                    // just a stub for now, next version of the Event Hub SDK will include AMQP size
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                sendClient.CloseAsync();
            }
        }
    }
}