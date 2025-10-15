using System;
using System.Collections.Generic;
using System.IO;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test.Integration
{
    [Collection(nameof(RealDbCollection))]
    public class ModelFactoryJsonGenerationTests
    {
        private readonly RealDbFixture _fx;
        public ModelFactoryJsonGenerationTests(RealDbFixture fx) => _fx = fx;

        [Fact]
        public void CreateServerObjects_Generates_JqlModel_Files_And_DoesNotDelete()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);

            // Generate for provisioned test tables
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Master, DbObjectType.Table));
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Detail, DbObjectType.Table));
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.Tag, DbObjectType.Table));
            factory.CreateServerObjectsFor(new DbObject(DbTestHelper.MasterTag, DbObjectType.Table));

            // Assert files exist and are left on disk for manual inspection
            Assert.True(File.Exists(JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master)));
            Assert.True(File.Exists(JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Detail)));
            Assert.True(File.Exists(JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Tag)));
            Assert.True(File.Exists(JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.MasterTag)));
        }

        [Fact]
        public void CreateNewUpdateByKey_And_HistoryTable_Generates_Model_Files_And_Keep_Them()
        {
            var factory = new JqlModelFactory(_fx.ConnectionName);

            // Create a partial update method and its history table
            factory.CreateNewUpdateByKey(
                objectName: DbTestHelper.Master,
                readByKeyApiName: "ReadByKey_Min",
                columnsToUpdate: new List<string> { "Name" },
                partialUpdateApiName: "UpdateName",
                byColumnName: "UpdatedBy",
                onColumnName: "UpdatedOn",
                historyTableName: DbTestHelper.History);

            // Ensure both source model and history model json files exist and remain
            Assert.True(File.Exists(JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.Master)));
            Assert.True(File.Exists(JqlModel.GetFullFilePath(_fx.ModelsRoot, _fx.ConnectionName, DbTestHelper.History)));
        }
    }
}
