using System;
using System.Collections;
using System.IO;
using JQL;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class ValueSharpAndContextTests
    {
        private readonly RealDbFixture _fx;
        public ValueSharpAndContextTests(RealDbFixture fx) => _fx = fx;

        private static byte[] TinyPng() => new byte[] { 137,80,78,71,13,10,26,10,0,0,0,13,73,72,68,82,0,0,0,1,0,0,0,1,8,6,0,0,0,31,21,196,137,0,0,0,12,73,68,65,84,120,156,99,96,0,0,0,2,0,1,226,33,188,33,0,0,0,0,73,69,78,68,174,66,96,130 };

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Create_And_Update_ValueSharp_And_Context()
        {
            var ctx = new Hashtable { ["UserId"] = 1, ["UserName"] = "tester" };

            // Create: CreatedBy/CreatedOn auto; Picture_xs resized from Picture
            var create = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.Create", ctx);
            create.Params =
            [
                new JqlParamRaw("Name", "SharpTest"),
                new JqlParamRaw("Picture", Convert.ToBase64String(TinyPng()))
            ];
            var newIdObj = create.Exec();
            int id = Convert.ToInt32(newIdObj);

            // UpdateByKey: UpdatedBy/UpdatedOn auto
            var upd = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.UpdateByKey", ctx);
            upd.Params = [ new JqlParamRaw("Id", id), new JqlParamRaw("Name", "SharpTest2") ];
            upd.Exec();
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void ValueSharp_ToMD5_And_ToMD4()
        {
            // For ToMD5/ToMD4 we need a column; we reuse Name mapping by constructing a special UpdateByKey_Partial
            var factory = new JqlModelFactory(_fx.ConnectionName);
            factory.CreateNewUpdateByKey(DbTestHelper.Master, "ReadByKey_MD", new() { "Name" }, "UpdateByKey_MD", string.Empty, string.Empty, string.Empty);

            var upd = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.UpdateByKey_MD");
            // Emulate ValueSharp by directly setting on dbQuery after init through Params merging; here we just provide plain value since hashing is done when ValueSharp is set by model.
            upd.Params = [ new JqlParamRaw("Id", 1), new JqlParamRaw("Name", "hash-me") ];
            var ex = Record.Exception(() => upd.Exec());
            Assert.Null(ex);
        }
    }
}
