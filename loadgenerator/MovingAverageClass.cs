using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace LoadGeneratorDotnetCore
{
    public class MovingAverageClass
    {
        private ConcurrentQueue<Tuple<Int64, Int64>> circularQueue = new ConcurrentQueue<Tuple<Int64, Int64>>();
        private int windowSize = 10;
        private Int64 sampleAccumulator;
        private double average = 0.0;

        public MovingAverageClass(int windowSize)
        {
            this.windowSize = windowSize;
        }
        public void Reset()
        {
            this.circularQueue.Clear();
        }
        public double AddCumulativeSample(Int64 value)
        {
            Int64 epochMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Tuple<Int64, Int64> sampleTuple = new Tuple<Int64, Int64>(value, epochMS);
            Interlocked.Add(ref this.sampleAccumulator, value - Interlocked.Read(ref this.sampleAccumulator));

            circularQueue.Enqueue(sampleTuple);

            if (circularQueue.Count > this.windowSize)
            {
                Tuple<Int64, Int64> sampleToDequeue;
                while (!circularQueue.TryDequeue(out sampleToDequeue)) { }

                Interlocked.Add(ref this.sampleAccumulator, -sampleToDequeue.Item1);
                Interlocked.Exchange(ref this.average, (double)this.sampleAccumulator / (epochMS - sampleToDequeue.Item2) * 1000);
            }
            else
            {
                Tuple<Int64, Int64> sampleToPeek;
                while (!circularQueue.TryPeek(out sampleToPeek)) { }

                Interlocked.Add(ref this.sampleAccumulator, -sampleToPeek.Item1);
                Interlocked.Exchange(ref this.average, (double)this.sampleAccumulator / (epochMS - sampleToPeek.Item2) * 1000);
            }
            return this.average;
        }
        public double AddSample(Int64 value)
        {
            Int64 epochMS = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            Tuple<Int64, Int64> sampleTuple = new Tuple<Int64, Int64>(value, epochMS);
            Interlocked.Add(ref this.sampleAccumulator, value);

            circularQueue.Enqueue(sampleTuple);

            if (circularQueue.Count > this.windowSize)
            {
                Tuple<Int64, Int64> sampleToDequeue;
                while (!circularQueue.TryDequeue(out sampleToDequeue)) { }

                Interlocked.Add(ref this.sampleAccumulator, -sampleToDequeue.Item1);
                Interlocked.Exchange(ref this.average, (double)this.sampleAccumulator / (epochMS - sampleToDequeue.Item2) * 1000);
            }
            else
            {
                Tuple<Int64, Int64> sampleToPeek;
                while (!circularQueue.TryPeek(out sampleToPeek)) { }

                Interlocked.Add(ref this.sampleAccumulator, -sampleToPeek.Item1);
                Interlocked.Exchange(ref this.average, (double)this.sampleAccumulator / (epochMS - sampleToPeek.Item2) * 1000);
            }
            return this.average;
        }
        private double GetAverage()
        {
            return this.average;
        }
    }
}