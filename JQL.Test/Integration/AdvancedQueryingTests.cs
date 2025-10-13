using System;
using System.Collections;
using System.Collections.Generic;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class AdvancedQueryingTests
    {
        private readonly RealDbFixture _fx;
        public AdvancedQueryingTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void RelationsContainment_On_ReadByKey_And_ReadList()
        {
            int mid = DbTestHelper.SeedMaster(_fx.ConnectionName, "REL-FULL", true);
            DbTestHelper.SeedDetail(_fx.ConnectionName, mid, "D1");

            // ReadByKey exclude all relations
            var rbk = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadByKey");
            rbk.Params = [ new JqlParamRaw("Id", mid) ];
            rbk.RelationsContainment = Containment.ExcludeAll;
            var res1 = rbk.Exec();
            Assert.NotNull(res1);

            // ReadList include no relations by default except MTM; force include none
            var rl = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            rl.RelationsContainment = Containment.ExcludeAll;
            rl.Where = new JqlWhere { CompareClauses = [ new CompareClause("Id", mid) { CompareOperator = CompareOperator.Equal } ] };
            var res2 = rl.Exec();
            Assert.NotNull(res2);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void ClientIndicatedRelations_On_ReadByKey()
        {
            int mid = DbTestHelper.SeedMaster(_fx.ConnectionName, "REL-CI", true);
            DbTestHelper.SeedDetail(_fx.ConnectionName, mid, "DX");

            var rbk = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadByKey");
            rbk.Params = [ new JqlParamRaw("Id", mid) ];
            rbk.RelationsContainment = Containment.IncludeIndicatedItems;
            rbk.ClientIndicatedRelations = new List<string>(); // none
            var res = rbk.Exec();
            Assert.NotNull(res);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void CompareOperators_Like_In_Null()
        {
            // Seed
            int a = DbTestHelper.SeedMaster(_fx.ConnectionName, "Alpha", true);
            int b = DbTestHelper.SeedMaster(_fx.ConnectionName, "Beta", false);
            // Null IsActive
            var reqNull = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.Create");
            reqNull.Params = [ new JqlParamRaw("Name", "Gamma") ];
            int c = Convert.ToInt32(reqNull.Exec());

            var rl = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            rl.Pagination = new JqlPagination{ PageNumber = 1, PageSize = 100 };
            rl.Where = new JqlWhere
            {
                ConjunctiveOperator = ConjunctiveOperator.AND,
                CompareClauses =
                [
                    new CompareClause("Name", "Al") { CompareOperator = CompareOperator.StartsWith },
                    new CompareClause("Name", "ta") { CompareOperator = CompareOperator.EndsWith },
                    new CompareClause("Name", "mm") { CompareOperator = CompareOperator.Contains },
                    new CompareClause("Id", "[" + a + "," + b + "]") { CompareOperator = CompareOperator.In },
                    new CompareClause("IsActive", null) { CompareOperator = CompareOperator.IsNull },
                    new CompareClause("IsActive", true) { CompareOperator = CompareOperator.Equal },
                ]
            };
            var res = rl.Exec();
            Assert.NotNull(res);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void AggregationsContainment_And_AddToMainSelect()
        {
            var rl = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            rl.AddAggregationsToMainSelect = true;
            rl.AggregationsContainment = Containment.IncludeIndicatedItems;
            rl.ClientIndicatedAggregations = new List<string>{ "Count" };
            var res = rl.Exec();
            Assert.NotNull(res);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void ColumnsContainment_Include_Exclude_All()
        {
            var rl = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            rl.ColumnsContainment = Containment.IncludeIndicatedItems;
            rl.ClientIndicatedColumns = new List<string>{ "Id", "Name" };
            var res1 = rl.Exec();
            Assert.NotNull(res1);

            rl.ColumnsContainment = Containment.ExcludeIndicatedItems;
            rl.ClientIndicatedColumns = new List<string>{ "Name" };
            var res2 = rl.Exec();
            Assert.NotNull(res2);

            rl.ColumnsContainment = Containment.IncludeAll;
            rl.ClientIndicatedColumns = null;
            var res3 = rl.Exec();
            Assert.NotNull(res3);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void UpdateByKey_With_History_Table()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);
            factory.CreateNewUpdateByKey(
                objectName: DbTestHelper.Master,
                readByKeyApiName: "ReadByKey_Partial",
                columnsToUpdate: new() { "Name", "IsActive" },
                partialUpdateApiName: "UpdateByKey_Partial",
                byColumnName: JqlUtils.UpdatedBy,
                onColumnName: JqlUtils.UpdatedOn,
                historyTableName: DbTestHelper.History);

            int mid = DbTestHelper.SeedMaster(_fx.ConnectionName, "HIST-1", true);
            var upd = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.UpdateByKey_Partial");
            upd.Params = [ new JqlParamRaw("Id", mid), new JqlParamRaw("Name", "HIST-2") ];
            upd.Exec();
        }
    }
}
