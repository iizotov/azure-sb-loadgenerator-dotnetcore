using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace LoadGeneratorDotnetCore
{
    public abstract class LoadGenerateeClass
    {
        protected string connectionString;
        public LoadGenerateeClass(string connectionString)
        {
            this.connectionString = connectionString;
        }
        public byte[] GeneratePayload(bool generateJsonPayload, int payloadSize)
        {
            string payload = "";

            if (generateJsonPayload)
            {
                string utcTimeStamp = ((long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds).ToString();
                string randomString = new Bogus.Randomizer().ClampString("", payloadSize, payloadSize);
                payload = $"{{'dt':{utcTimeStamp},'payload':'{randomString}'}}";
            }
            else
            {
                payload = new Bogus.Randomizer().ClampString("", payloadSize, payloadSize);
            }
            return Encoding.UTF8.GetBytes(payload);
        }
        public abstract Task GenerateBatchAndSend(int batchSize, bool dryRun, CancellationToken cancellationToken, Func<byte[]> loadGenerator);
    }
}