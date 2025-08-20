using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    [MemoryDiagnoser]
    [RankColumn]
    public class BenchmarkTest
    {
        [Benchmark]
        public void BenchPing()
        {
            PingManager.StartPing();
        }

    }
}
