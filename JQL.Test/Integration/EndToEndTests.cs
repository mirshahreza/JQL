using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JQL;
using PowNet.Common;
using PowNet.Configuration;
using Xunit;

namespace JQL.Test.Integration
{
    [CollectionDefinition(nameof(RealDbCollection))]
    public class RealDbCollection : ICollectionFixture<RealDbFixture> { }

    [Collection(nameof(RealDbCollection))]
    public class EndToEndTests
    {
        private readonly RealDbFixture _fx;
        public EndToEndTests(RealDbFixture fx) => _fx = fx;

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void ReadList_Basic_Paging_Sorting_Filtering()
        {
            var ctx = new Hashtable { ["UserId"] = 1, ["UserName"] = "tester" };
            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList", ctx);
            req.Pagination = new JqlPagination { PageNumber = 1, PageSize = 10 };
            req.OrderClauses = [ new JqlOrderClause("Id", OrderDirection.DESC) ];
            req.Where = new JqlWhere
            {
                CompareClauses = [ new CompareClause("Id", 0) { CompareOperator = CompareOperator.MoreThan } ]
            };

            var r = req.Exec();
            Assert.NotNull(r);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Create_ReadByKey_Update_Delete_Flow()
        {
            var ctx = new Hashtable { ["UserId"] = 1 };

            // Create
            var create = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.Create", ctx);
            create.Params = [ new JqlParamRaw("Name", "ITEST"), new JqlParamRaw("IsActive", true) ];
            var newIdObj = create.Exec();
            Assert.NotNull(newIdObj);
            var newId = Convert.ToInt32(newIdObj);

            // ReadByKey
            var read = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadByKey");
            read.Params = [ new JqlParamRaw("Id", newId) ];
            var dt = read.Exec();
            Assert.NotNull(dt);

            // UpdateByKey
            var upd = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.UpdateByKey", ctx);
            upd.Params = [ new JqlParamRaw("Id", newId), new JqlParamRaw("Name", "ITEST-2") ];
            upd.Exec();

            // DeleteByKey
            var del = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.DeleteByKey");
            del.Params = [ new JqlParamRaw("Id", newId) ];
            del.Exec();
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Procedure_TableFunction_ScalarFunction()
        {
            var ctx = new Hashtable();

            // Procedure example
            var sp = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.DbDirect.Exec", ctx);
            sp.Params = [ new JqlParamRaw("SomeParam", 1) ];
            var ds = sp.Exec();

            // Table function example
            var tf = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.Reports.Select", ctx);
            tf.Params = [ new JqlParamRaw("Year", DateTime.Now.Year) ];
            var rows = tf.Exec();

            // Scalar function example
            var sf = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.Formulas.Calculate", ctx);
            sf.Params = [ new JqlParamRaw("x", 2) ];
            var val = sf.Exec();
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Advanced_Filtering_And_Containment()
        {
            // Seed data
            int a = DbTestHelper.SeedMaster(_fx.ConnectionName, "A", true);
            int b = DbTestHelper.SeedMaster(_fx.ConnectionName, "B", false);

            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            req.Pagination = new JqlPagination { PageNumber = 1, PageSize = 50 };

            // Complex where: (IsActive is null OR IsActive = 1) AND Name IN ('A','B') AND Id > 0
            req.Where = new JqlWhere
            {
                ConjunctiveOperator = ConjunctiveOperator.AND,
                CompareClauses = [ new CompareClause("Id", 0) { CompareOperator = CompareOperator.MoreThan } ],
                ComplexClauses =
                [
                    new JqlWhere
                    {
                        ConjunctiveOperator = ConjunctiveOperator.OR,
                        CompareClauses =
                        [
                            new CompareClause("IsActive", null) { CompareOperator = CompareOperator.IsNull },
                            new CompareClause("IsActive", true) { CompareOperator = CompareOperator.Equal }
                        ]
                    },
                    new JqlWhere
                    {
                        CompareClauses = [ new CompareClause("Name", "[A,B]") { CompareOperator = CompareOperator.In } ]
                    }
                ]
            };

            // Select only columns we need
            req.ColumnsContainment = Containment.IncludeIndicatedItems;
            req.ClientIndicatedColumns = [ "Id", "Name" ];

            var r = req.Exec();
            Assert.NotNull(r);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void Relations_OneToMany_And_ManyToMany()
        {
            int mid = DbTestHelper.SeedMaster(_fx.ConnectionName, "REL-OTM", true);
            DbTestHelper.SeedDetail(_fx.ConnectionName, mid, "REL-D1");
            DbTestHelper.SeedDetail(_fx.ConnectionName, mid, "REL-D2");

            int tag1 = DbTestHelper.SeedTag(_fx.ConnectionName, "T1");
            int tag2 = DbTestHelper.SeedTag(_fx.ConnectionName, "T2");
            DbTestHelper.LinkMasterTag(_fx.ConnectionName, mid, tag1);
            DbTestHelper.LinkMasterTag(_fx.ConnectionName, mid, tag2);

            // ReadByKey should include all relations
            var rbk = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadByKey");
            rbk.Params = [ new JqlParamRaw("Id", mid) ];
            var dt = rbk.Exec();
            Assert.NotNull(dt);

            // ReadList includes only MTM relations; ensure MTM projection works
            var rl = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            rl.Where = new JqlWhere { CompareClauses = [ new CompareClause("Id", mid) { CompareOperator = CompareOperator.Equal } ] };
            var res = rl.Exec();
            Assert.NotNull(res);
        }

        [Fact(Skip = "Un-skip to run against real DB")] 
        public void AggregatedReadList_With_GroupBy_And_Aggregations()
        {
            var agg = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.AggregatedReadList");
            agg.Where = new JqlWhere { CompareClauses = [ new CompareClause("Id", 0) { CompareOperator = CompareOperator.MoreThan } ] };
            var res = agg.Exec();
            Assert.NotNull(res);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void ReadList_String_Operators_StartsWith_EndsWith_Contains()
        {
            // Seed
            DbTestHelper.SeedMaster(_fx.ConnectionName, "STR-Alpha", true);
            DbTestHelper.SeedMaster(_fx.ConnectionName, "STR-Beta", true);
            DbTestHelper.SeedMaster(_fx.ConnectionName, "MM-Gamma", true);

            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            req.Where = new JqlWhere
            {
                ConjunctiveOperator = ConjunctiveOperator.AND,
                CompareClauses =
                [
                    new CompareClause("Name", "STR-") { CompareOperator = CompareOperator.StartsWith },
                    new CompareClause("Name", "a") { CompareOperator = CompareOperator.Contains },
                    new CompareClause("Name", "a") { CompareOperator = CompareOperator.EndsWith }
                ]
            };
            var r = req.Exec();
            Assert.NotNull(r);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void ReadList_Mixed_Containment_Columns_And_Aggregations()
        {
            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            req.ColumnsContainment = Containment.ExcludeIndicatedItems;
            req.ClientIndicatedColumns = [ "Picture", "Picture_xs" ]; // exclude heavy content
            req.AggregationsContainment = Containment.IncludeIndicatedItems;
            req.ClientIndicatedAggregations = [ "Count" ];
            req.Pagination = new JqlPagination { PageNumber = 1, PageSize = 10 };
            var r = req.Exec();
            Assert.NotNull(r);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void UpdateByKey_With_Relations_Create_Update_Delete()
        {
            // Seed master and one detail
            int mid = DbTestHelper.SeedMaster(_fx.ConnectionName, "REL-MASTER", true);
            DbTestHelper.SeedDetail(_fx.ConnectionName, mid, "OLD");

            // Prepare relations payload
            var relations = new Dictionary<string, List<List<JqlParamRaw>>>();
            var rows = new List<List<JqlParamRaw>>();
            // create a new detail
            rows.Add(new List<JqlParamRaw>
            {
                new("MasterId", mid),
                new("Title", "NEW"),
                new("_flag_", "c")
            });
            // update an existing one (assumes Id=1 for simplicity in test env or adapt accordingly)
            rows.Add(new List<JqlParamRaw>
            {
                new("Id", 1),
                new("Title", "UPDATED"),
                new("_flag_", "u")
            });
            // delete an existing one (assumes Id=1 for simplicity)
            rows.Add(new List<JqlParamRaw>
            {
                new("Id", 1),
                new("_flag_", "d")
            });
            relations[DbTestHelper.Detail] = rows;

            var upd = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.UpdateByKey");
            upd.Params = [ new JqlParamRaw("Id", mid), new JqlParamRaw("Name", "REL-MASTER-X") ];
            upd.Relations = relations;
            var ex = Record.Exception(() => upd.Exec());
            Assert.Null(ex);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void ReadByKey_With_Relations_Containment_ClientIndicated()
        {
            var rbk = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadByKey");
            rbk.RelationsContainment = Containment.IncludeAll; // include all defined relations
            rbk.ColumnsContainment = Containment.IncludeIndicatedItems;
            rbk.ClientIndicatedColumns = ["Id","Name"]; // keep it light
            var r = rbk.Exec();
            Assert.NotNull(r);
        }

        [Fact(Skip = "Un-skip to run against real DB")]
        public void ReadList_AddAggregations_To_MainSelect()
        {
            var req = JqlRequest.GetInstanceByQueryName($"{_fx.ConnectionName}.{DbTestHelper.Master}.ReadList");
            req.AddAggregationsToMainSelect = true;
            req.AggregationsContainment = Containment.IncludeIndicatedItems;
            req.ClientIndicatedAggregations = ["Count"]; // append count agg to main select
            var r = req.Exec();
            Assert.NotNull(r);
        }
    }
}
