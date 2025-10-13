using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using JQL;
using PowNet.Configuration;
using PowNet.Extensions;
using Xunit;

namespace JQL.Test.Integration
{
    public class RealDbFixture : IDisposable
    {
        public readonly string ConnectionName = "DefaultConnection";
        public readonly string ModelsRoot;

        public RealDbFixture()
        {
            // Ensure models root exists at configured PowNet path
            ModelsRoot = PowNetConfiguration.ServerPath;
            if (string.IsNullOrWhiteSpace(ModelsRoot))
                ModelsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JqlModels");
            Directory.CreateDirectory(ModelsRoot);

            // Optional: verify connection
            var dbConf = DatabaseConfiguration.FromSettings(ConnectionName);
            Assert.True(JqlRun.TestConnection(dbConf));

            // Provision test objects and models
            DbTestHelper.EnsureProvisioned(ConnectionName);
        }

        private void TryEnsureModel(string objectName) { }

        public void Dispose()
        {
        }
    }
}
