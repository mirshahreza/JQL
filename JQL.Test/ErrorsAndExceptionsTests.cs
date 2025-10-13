using System;
using System.Collections;
using System.Collections.Generic;
using JQL;
using PowNet.Common;
using Xunit;

namespace JQL.Test
{
    public class ErrorsAndExceptionsTests
    {
        [Fact]
        public void JqlModel_Load_Throws_When_File_Missing()
        {
            Assert.Throws<System.Exception>(() => JqlModel.Load("C:/not-exist","Db","Nope"));
        }

        [Fact]
        public void JqlRequest_GetInstanceByQueryName_Throws_On_Invalid_Name()
        {
            Assert.Throws<System.Exception>(() => JqlRequest.GetInstanceByQueryName("OnlyTwoParts.Invalid"));
        }

        [Fact]
        public void JqlRequest_ParamValue_Returns_Null_When_Not_Found()
        {
            var q = new JqlRequest { QueryFullName = "Db.Users.ReadList" };
            var v = q.ParamValue("NotExist");
            Assert.Null(v);
        }
    }
}
