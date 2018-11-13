using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;



// TODO: modify protection of members
// TODO: introduce LINQ

namespace LoadGeneratorDotnetCore
{
    class OrchestratorClass
    {
        public bool isJobDone { get; set; } = false;
        private const int CONTROL_LOOP_INTERVAL_MS = 300;
        private const int MAX_THREADS = 100;
        private dynamic loadGeneratee;
        private System.Timers.Timer timer;
        private int targetThreadCount = 1;
        private Int64 targetMessageCount = 0;
        protected readonly object sharedLock = new object();
        private ConcurrentBag<Tuple<Task, CancellationTokenSource>> taskCollection = new ConcurrentBag<Tuple<Task, CancellationTokenSource>>();
        private int cursorTop = -1;

        public OrchestratorClass(dynamic loadGeneratee, int targetThreadCount, Int64 targetMessageCount)
        {
            this.loadGeneratee = loadGeneratee;
            this.timer = new System.Timers.Timer { Interval = CONTROL_LOOP_INTERVAL_MS };
            this.targetMessageCount = targetMessageCount;
            SetTargetThreadCount(targetThreadCount);
        }

        public void PrintProgress()
        {
            if(this.cursorTop < 0)
            {
                this.cursorTop = Console.CursorTop;
                // TODO: not thread-safe
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Target threads: {GetTargetThreadCount()} Active threads: {GetActiveThreadCount()}");
            sb.AppendLine($"Batch size: {loadGeneratee.GetBatchSize()} payload size: {loadGeneratee.GetPayloadSize()} bytes");

            foreach (var taskTuple in taskCollection)
            {
                Guid threadId = (Guid)taskTuple.Item1.AsyncState;
                sb.AppendLine($"Thread {threadId} is {taskTuple.Item1.Status}, sent {loadGeneratee.GetThreadMessageCount(threadId)} msgs, avg speed: {loadGeneratee.GetAverageThreadThroughputMPS(threadId):0.#} msg/sec");
            }
            sb.AppendLine($"Sent: {loadGeneratee.GetTotalMessageCount()} msgs, avg speed: {loadGeneratee.GetAverageThroughputMPS():0.#} msg/sec");
            sb.AppendLine("");
            Console.Write(sb.ToString());
            try
            {
                Console.CursorTop = this.cursorTop;
                Console.CursorLeft = 0;
            }
            catch{}
        }

        public void Start()
        {
            timer.Elapsed += (sender, e) =>
            {
                ControlLoop();
            };
            timer.Start();
        }

        public void Stop()
        {
            SetTargetThreadCount(0);
        }

        public int GetActiveThreadCount()
        {
            return taskCollection.Where(t => t.Item1.IsCompleted == false).Count();
        }
        public int GetTargetThreadCount()
        {
            return targetThreadCount;
        }
        public void SetTargetThreadCount(int targetThreadCount)
        {
            this.targetThreadCount = targetThreadCount >= 0 ? Math.Min(targetThreadCount, MAX_THREADS) : 0;
        }
        public int GetBatchSize()
        {
            return loadGeneratee.GetBatchSize();
        }
        public void SetBatchSize(int batchSize)
        {
            loadGeneratee.SetBatchSize(batchSize);
        }
        public int GetPayloadSize()
        {
            return loadGeneratee.GetPayloadSize();
        }
        public void SetPayloadSize(int payloadSize)
        {
            loadGeneratee.SetPayloadSize(payloadSize);
        }
        private void ControlLoop()
        {
            // lock (sharedLock)
            {
                if (GetActiveThreadCount() < GetTargetThreadCount())
                {
                    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                    CancellationToken cancellationToken = cancellationTokenSource.Token;

                    Task task = Task.Factory
                        .StartNew(
                            (Object g) =>
                            {
                                loadGeneratee.StartThread((Guid)g, cancellationToken);
                            },
                            Guid.NewGuid(),
                            cancellationToken,
                            TaskCreationOptions.LongRunning,
                            TaskScheduler.Default);

                    taskCollection.Add(new Tuple<Task, CancellationTokenSource>(task, cancellationTokenSource));
                }

                if (GetActiveThreadCount() > GetTargetThreadCount())
                {
                    CancellationTokenSource cancellationTokenSource = taskCollection.First(t => t.Item1.IsCompleted == false).Item2;
                    cancellationTokenSource.Cancel();
                }

                if (targetMessageCount > 0 && targetMessageCount > loadGeneratee.GetTotalMessageCount())
                {
                    SetTargetThreadCount(0);
                }

                if (GetActiveThreadCount() == 0 && GetTargetThreadCount() == 0)
                {
                    isJobDone = true;
                    timer.Stop();
                }
            }
        }
    }

}