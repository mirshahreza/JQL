using System.Collections;
using System.Linq;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class RefToJoinTests
    {
        private readonly RealDbFixture _fx;
        public RefToJoinTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void RefTo_Adds_Left_Joins_And_Columns()
        {
            // Ensure a FK exists MasterId -> ITEST_Master.Id in ITEST_Detail
            // ReadList on Detail should include RefTo to Master and bring its human-id (Name)
            var factory = new JqlModelFactory(_fx.ConnectionName);
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Detail, DbObjectType.Table));

            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Detail}.ReadList");
            req.ColumnsContainment = Containment.IncludeAll;
            var r = req.Exec();
            Assert.NotNull(r);
        }
    }
}
