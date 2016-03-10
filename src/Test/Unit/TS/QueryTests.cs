namespace Test.Unit.TS
{
    using NUnit.Framework;
    using RiakClient;
    using RiakClient.Commands.TS;
    using RiakClient.Messages;

    public class QueryTests : TimeseriesTest
    {
        private const string Query = "SELECT * From GeoCheckin";
        private static readonly RiakString QueryRS = new RiakString(Query);

        [Test]
        public void Should_Build_Req()
        {
            var cmd = new Query.Builder()
                .WithTable(Table)
                .WithQuery(Query)
                .Build();

            Assert.AreEqual(MessageCode.TsQueryResp, cmd.ExpectedCode);

            TsQueryReq pb = (TsQueryReq)cmd.ConstructPbRequest();
            Assert.AreEqual(QueryRS, RiakString.FromBytes(pb.query.@base));
            Assert.IsTrue(pb.streamSpecified);
            Assert.IsFalse(pb.stream);
        }
    }
}
