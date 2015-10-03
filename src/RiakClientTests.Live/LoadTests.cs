namespace RiakClientTests.Live.LoadTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using RiakClient;
    using RiakClient.Extensions;
    using RiakClient.Models;
    using RiakClient.Models.MapReduce;
    using RiakClient.Models.MapReduce.Inputs;

    [TestFixture, IntegrationTest]
    public class WhenUnderLoad : LiveRiakConnectionTestBase
    {
        // thread count is currently set to the same
        // value as the number of connections available
        // across the cluster. This is deliberate until
        // connection recovery/retry across nodes is
        // functioning and the load-balancing strategies
        // are in place.
        private const int ThreadCount = 30;
        private const int ActionCount = 10;
        //private const int ThreadCount = 1;
        //private const int ActionCount = 1;

        [Test]
        public void LotsOfConcurrentMapRedRequestsShouldWork()
        {
            var keys = new List<string>();

            for (var i = 1; i < 11; i++)
            {
                var key = "key" + i;
                var doc = new RiakObject(MapReduceBucket, key, new { value = i });
                keys.Add(key);

                var result = Client.Put(doc, new RiakPutOptions { ReturnBody = true });
                result.IsSuccess.ShouldBeTrue();
            }

            var input = new RiakBucketKeyInput();
            keys.ForEach(k => input.Add(new RiakObjectId(MapReduceBucket, k)));

            var query = new RiakMapReduceQuery()
                .Inputs(input)
                .MapJs(m => m.Source(@"function(o){return[1];}"))
                .ReduceJs(r => r.Name(@"Riak.reduceSum").Keep(true));
            query.Compile();

            var results = new List<RiakResult<RiakMapReduceResult>>[ThreadCount];
            Parallel.For(0, ThreadCount, i =>
                {
                    results[i] = DoMapRed(query);
                });

            var failures = 0;
            foreach (var r in results.SelectMany(l => l))
            {
                if (r.IsSuccess)
                {
                    var resultValue = JsonConvert.DeserializeObject<int[]>(r.Value.PhaseResults.ElementAt(1).Values.First().FromRiakString())[0];
                    resultValue.ShouldEqual(10);
                    //r.Value.PhaseResults.ElementAt(1).GetObject<int[]>()[0].ShouldEqual(10);
                }
                else
                {
                    // the only acceptable result is that it ran out of retries when
                    // talking to the cluster (trying to get a connection)
                    r.ResultCode.ShouldEqual(ResultCode.NoRetries);
                    ++failures;
                }
            }

            Console.WriteLine("Total of {0} out of {1} failed to execute due to connection contention", failures, ThreadCount * ActionCount);
        }

        private List<RiakResult<RiakMapReduceResult>> DoMapRed(RiakMapReduceQuery query)
        {
            var client = Cluster.CreateClient();

            var results = ActionCount.Times(() => client.MapReduce(query));
            return results.ToList();
        }

        [Test]
        public void LotsOfConcurrentStreamingMapRedRequestsShouldWork()
        {
            var keys = new List<string>();

            for (var i = 1; i < 11; i++)
            {
                var key = "key" + i;
                var doc = new RiakObject(MapReduceBucket, key, new { value = i });
                keys.Add(key);

                var result = Client.Put(doc, new RiakPutOptions { ReturnBody = true });
                result.IsSuccess.ShouldBeTrue();
            }

            var input = new RiakBucketKeyInput();
            keys.ForEach(k => input.Add(new RiakObjectId(MapReduceBucket, k)));

            var query = new RiakMapReduceQuery()
                .Inputs(input)
                .MapJs(m => m.Source(@"function(o){return[1];}"))
                .ReduceJs(r => r.Name(@"Riak.reduceSum").Keep(true));
            query.Compile();

            var results = new List<RiakMapReduceResultPhase>[ThreadCount];
            Parallel.For(0, ThreadCount, i =>
                {
                    results[i] = DoStreamingMapRed(query);
                });

            var failures = 0;
            foreach (var result in results)
            {
                if (result.Count > 0)
                {
                    var lastResult = result.OrderByDescending(r => r.Phase).First();
                    var resultValue = JsonConvert.DeserializeObject<int[]>(lastResult.Values.First().FromRiakString());
                    //var resultValue = JsonConvert.DeserializeObject<int[]>(r.Value.PhaseResults.ElementAt(1).Values.First().FromRiakString())[0];
                    // due to the speed which things happen at, we can't gaurantee all 10 will be in the result set
                    resultValue[0].IsAtLeast(5);
                    //lastResult.GetObject<int[]>()[0].ShouldEqual(10);
                }
                else
                {
                    ++failures;
                }
            }
            Console.WriteLine("Total of {0} out of {1} failed to execute due to connection contention", failures, ThreadCount * ActionCount);
        }

        private List<RiakMapReduceResultPhase> DoStreamingMapRed(RiakMapReduceQuery query)
        {
            var client = Cluster.CreateClient();

            var results = ActionCount.Times(() =>
                {
                    var streamedResults = client.StreamMapReduce(query);
                    if (streamedResults.IsSuccess)
                    {
                        return streamedResults.Value.PhaseResults;
                    }
                    return null;
                }).Where(r => r != null).SelectMany(r => r);

            return results.ToList();
        }
    }
}
