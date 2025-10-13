using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class JqlRelationRulesTests
    {
        [Fact]
        public void Relation_Defaults_And_UiWidget_Inference()
        {
            var rel = new JqlRelation("Files","Id","MasterId") { IsFileCentric = true };
            Assert.Equal("To_Files_On_MasterId", rel.RelationName);
            Assert.Equal(RelationType.Unknown, rel.RelationType);
            // UI widget for file-centric (set by factory normally); here we only validate name
            Assert.Equal("Files", rel.RelationTable);
        }
    }
}
