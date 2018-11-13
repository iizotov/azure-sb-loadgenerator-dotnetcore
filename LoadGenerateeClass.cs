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
        private Int64 _throttleMS;
        protected int throttleMS
        {
            get
            {
                // looks like a dodgy cast but not really - see the setter
                return (int)Interlocked.Read(ref _throttleMS);
            }
            set
            {
                int val = Math.Max(0, value);
                val = Math.Min(val, int.MaxValue);
                Interlocked.Exchange(ref _throttleMS, val);
            }
        }
        private Int64 _batchSize;
        protected int batchSize
        {
            get
            {
                // looks like a dodgy cast but not really - see the setter
                return (int)Interlocked.Read(ref _batchSize);
            }
            set
            {
                int val = Math.Max(1, value);
                val = Math.Min(val, int.MaxValue);
                Interlocked.Exchange(ref _batchSize, val);
            }
        }

        private Int64 _payloadSize;
        protected int payloadSize
        {
            get
            {
                // looks like a dodgy cast but not really - see the setter
                return (int)Interlocked.Read(ref _payloadSize);
            }
            set
            {
                int val = Math.Max(1, value);
                val = Math.Min(val, int.MaxValue);
                Interlocked.Exchange(ref _payloadSize, val);
            }
        }

        protected bool timerStarted = false;
        protected bool generateJsonPayload = false;
        protected int highThreadId = 0;
        protected readonly object sharedLock = new object();

        protected string connectionString;
        protected DateTime timerStart = DateTime.Now;
        protected ConcurrentDictionary<Guid, Int64> bytesSentByThread = new ConcurrentDictionary<Guid, Int64>(100000, 100);
        protected ConcurrentDictionary<Guid, Int64> messagesSentByThread = new ConcurrentDictionary<Guid, Int64>(100000, 100);
        protected ConcurrentDictionary<Guid, DateTime> threadStartTime = new ConcurrentDictionary<Guid, DateTime>(100000, 100);
        public LoadGenerateeClass(string connectionString, int batchSize, int payloadSize, bool generateJsonPayload)
        {
            this.connectionString = connectionString;
            this.batchSize = batchSize;
            this.payloadSize = payloadSize;
            this.generateJsonPayload = generateJsonPayload;
        }
        public void SetBatchSize(int batchSize)
        {
            this.batchSize = batchSize;
        }

        public int GetBatchSize()
        {
            return this.batchSize;
        }

        public void SetPayloadSize(int payloadSize)
        {
            this.payloadSize = payloadSize;
        }

        public int GetPayloadSize()
        {
            return this.payloadSize;
        }
        public void StartThread(Guid threadId, CancellationToken cancellationToken)
        {
            lock (sharedLock)
            {
                // very first execution
                if (!timerStarted)
                {
                    timerStarted = true;
                    timerStart = DateTime.Now;
                }
                highThreadId++;
            }
            try
            {
                messagesSentByThread[threadId] = 0;
                bytesSentByThread[threadId] = 0;
                threadStartTime[threadId] = DateTime.Now;
                GenerateWorkload(threadId, cancellationToken);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        public byte[] GeneratePayload()
        {
            return GeneratePayload(this.generateJsonPayload, this.payloadSize);
        }
        private byte[] GeneratePayload(bool generateJsonPayload, int payloadSize)
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
        public abstract void GenerateWorkload(Guid threadId, CancellationToken cancellationToken);
        public Int64 GetTotalMessageCount()
        {
            return messagesSentByThread.Count > 0
                ? messagesSentByThread.Values.Sum()
                : 0;
        }
        public Int64 GetTotalBytesSent()
        {
            return bytesSentByThread.Count > 0
                ? bytesSentByThread.Values.Sum()
                : 0;
        }
        public Int64 GetThreadBytesSent(Guid threadId)
        {
            return bytesSentByThread.ContainsKey(threadId)
                ? bytesSentByThread[threadId]
                : 0;
        }
        public Int64 GetThreadMessageCount(Guid threadId)
        {
            return messagesSentByThread.ContainsKey(threadId)
                ? messagesSentByThread[threadId]
                : 0;
        }
        public DateTime GetThreadStartTime(Guid threadId)
        {
            return threadStartTime.ContainsKey(threadId)
                ? threadStartTime[threadId]
                : DateTime.Now.AddDays(-1);
        }
        public double GetAverageThreadThroughputMPS(Guid threadId)
        {
            return GetThreadMessageCount(threadId) / (DateTime.Now - GetThreadStartTime(threadId)).TotalSeconds;
        }
        public double GetAverageThroughputMPS()
        {
            return GetTotalMessageCount() / (DateTime.Now - timerStart).TotalSeconds;
        }
        public double GetAverageThreadThroughputBPS(Guid threadId)
        {
            return GetThreadBytesSent(threadId) / (DateTime.Now - GetThreadStartTime(threadId)).TotalSeconds;
        }
        public double GetAverageThreadThroughputKBPS(Guid threadId)
        {
            return GetAverageThreadThroughputBPS(threadId) / 1024;
        }
        public double GetAverageThroughputKBPS()
        {
            return GetAverageThroughputBPS() / 1024;
        }
        public double GetAverageThroughputBPS()
        {
            return GetTotalBytesSent() / (DateTime.Now - timerStart).TotalSeconds;
        }
    }
}