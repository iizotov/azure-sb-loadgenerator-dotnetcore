using System;
using System.Threading.Tasks;
using System.Threading;
using RateLimiter;
using System.Text;

namespace LoadGeneratorDotnetCore
{
    class OrchestratorClass
    {
        private System.Timers.Timer statisticsTimer = new System.Timers.Timer(100);
        private DateTime processStartTS;
        private bool isRunning = false;
        protected readonly object sharedLock = new object();
        private dynamic loadGeneratee;
        private int targetThroughput;
        private Int64 targetMessageCount;
        private Int64 totalMessageCount;
        private double longAverageThroughput = 0.0;
        private double shortAverageThroughput = 0.0;
        private int batchSize;
        private bool dryRun;
        public bool isJobDone = false;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Func<byte[]> DataGenerator;
        private MovingAverageClass movingAverage = new MovingAverageClass(500); // moving window across 500 data points
        private int messageSize;
        private TimeLimiter throttler;
        public OrchestratorClass(dynamic loadGeneratee, int targetMessagesPerSecond, Int64 targetMessageCount, int batchSize, bool dryRun, Func<byte[]> DataGenerator)
        {
            this.loadGeneratee = loadGeneratee;
            this.targetMessageCount = targetMessageCount;
            this.targetThroughput = targetMessagesPerSecond;
            this.batchSize = batchSize;
            this.dryRun = dryRun;
            this.DataGenerator = DataGenerator;
            this.messageSize = DataGenerator().Length;
        }
        private void UpdateStatistics()
        {
            Interlocked.Exchange(ref this.longAverageThroughput, (double)this.totalMessageCount / (DateTime.Now - this.processStartTS).TotalMilliseconds * 1000.0);
            Interlocked.Exchange(ref this.shortAverageThroughput, this.movingAverage.AddCumulativeSample(this.totalMessageCount));
        }
        private void SetThrottler()
        {
            if (this.targetThroughput > 0)
            {
                // 25% extra for overhead - maybe, need a more scientific way to calc, 0% for now 
                double overheadCompensator = this.dryRun ? 1.0 : 1.0;

                double controlInterval = 1.0; // seconds

                // how many invokations per second are allowed
                double invocationsPerControlInterval = (double)(this.targetThroughput * overheadCompensator) / (double)this.batchSize * controlInterval;

                double clampBelowInterval = controlInterval / invocationsPerControlInterval * 1000.0;

                // renormalise to avoid fractional invocations
                if (invocationsPerControlInterval < 100)
                {
                    double normaliser = 100.0 / invocationsPerControlInterval;
                    invocationsPerControlInterval = normaliser * invocationsPerControlInterval;
                    controlInterval = normaliser * controlInterval;
                }

                // does not make sence to clamp from below at < 15ms - timers aren't that precise
                if (clampBelowInterval < 15.0)
                {
                    this.throttler = TimeLimiter.GetFromMaxCountByInterval(Convert.ToInt32(invocationsPerControlInterval), TimeSpan.FromSeconds(controlInterval));
                }
                else
                {
                    var clampAbove = new CountByIntervalAwaitableConstraint(Convert.ToInt32(invocationsPerControlInterval), TimeSpan.FromSeconds(controlInterval));
                    // Clamp from below: e.g. one invocation every 100 ms
                    var clampBelow = new CountByIntervalAwaitableConstraint(1, TimeSpan.FromMilliseconds(clampBelowInterval));
                    //Compose the two constraints
                    this.throttler = TimeLimiter.Compose(clampAbove, clampBelow);
                }
            }
            else // no throttling
            {
                this.throttler = TimeLimiter.GetFromMaxCountByInterval(Int32.MaxValue, TimeSpan.FromSeconds(1));
            }
        }
        public string GetStatusSnapshot()
        {
            String dt = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"[{dt}] Target: throughput {this.targetThroughput} msg/sec, volume: {this.targetMessageCount}, batch size: {this.batchSize}, msg size: {this.messageSize}");
            sb.AppendLine($"[{dt}] Stats: {this.totalMessageCount} msg sent, long avg {this.longAverageThroughput:0.#} msg/sec, short avg {this.shortAverageThroughput:0.#} msg/sec");
            return sb.ToString();
        }
        // Kicks off the main loop
        public void Start()
        {
            CancellationToken cancellationToken = this.cancellationTokenSource.Token;
            // this._Start(cancellationToken);
            Task task = new Task(
            () => this._Start(cancellationToken)
            , cancellationToken, TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning);
            task.Start(TaskScheduler.Default);
        }

        // The main loop
        private async void _Start(CancellationToken cancellationToken)
        {
            // ensure only one instance is running
            if (this.isRunning)
            {
                return;
            }
            lock (this.sharedLock)
            {
                this.isRunning = true;
                this.processStartTS = DateTime.Now;
                this.SetThrottler();
                this.statisticsTimer.Elapsed += (sender, e) =>
                {
                    this.UpdateStatistics();
                };
                this.statisticsTimer.Start();
            }

            // Keep running until:
            // a. shared cancellation token is triggered
            // b. we've emitted the required quantity of messages
            // Keep running forever otherwise           
            while (this.totalMessageCount < this.targetMessageCount ||
                    this.targetMessageCount <= 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (this.targetThroughput <= 0) // unconstrained
                {
                    try
                    {
                        // TODO: understand if exceptions propagate - looks like the counter is incremented regardless!
                        await this.loadGeneratee.GenerateBatchAndSend(this.batchSize, this.dryRun, cancellationToken, this.DataGenerator);
                        Interlocked.Add(ref this.totalMessageCount, this.batchSize);
                    }
                    catch { } // swallow all exceptions
                }
                else // constrained version
                {
                    try
                    {
                        await this.throttler.Perform(async () =>
                        {
                            await this.loadGeneratee.GenerateBatchAndSend(this.batchSize, this.dryRun, cancellationToken, this.DataGenerator);
                            Interlocked.Add(ref this.totalMessageCount, this.batchSize);
                        }, cancellationToken);
                    }
                    catch { } // swallow all exceptions
                }
            }

            // Clean up
            lock (this.sharedLock)
            {
                this.isRunning = false;
                this.isJobDone = true;
                this.statisticsTimer.Stop();
            }
        }
        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
        }
    }

}