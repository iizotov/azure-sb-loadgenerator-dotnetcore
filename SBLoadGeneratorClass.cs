using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;

namespace LoadGeneratorDotnetCore
{
    class SBLoadGeneratorClass : LoadGenerateeClass
    {
        private ServiceBusConnectionStringBuilder sbConnectionString;
        private string entityPath;
        public SBLoadGeneratorClass(
            string connectionString, string entityPath,
            int payloadSize, bool generateJsonPayload,
            int batchSize) : base(connectionString, batchSize, payloadSize, generateJsonPayload)
        {
            this.entityPath = entityPath;
            try
            {
                sbConnectionString = new ServiceBusConnectionStringBuilder(connectionString);
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
                sbConnectionString.EntityPath = entityPath;
            }
        }
        public override void GenerateWorkload(Guid threadId, CancellationToken cancellationToken)
        {
            Int64 queuedMessageCount = 0;
            List<Message> batchOfMessages = new List<Message>();
            DateTime timerStart;
            QueueClient sendClient = new QueueClient(sbConnectionString);
            try
            {
                timerStart = DateTime.Now;
                while (!cancellationToken.IsCancellationRequested)
                {
                    Message randomPayload = new Message(GeneratePayload());
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
                        batchOfMessages.Clear();
                    }
                    queuedMessageCount++;
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