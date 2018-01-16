﻿using MigSharp.NUnit.Integration;
using NUnit.Framework;

namespace MigSharp.MySql.NUnit 
{
    public abstract class MySqlIntegrationTestsBase : IntegrationTestsBase 
    {
        [Test]
        public override void TestMigrationWithinTransactionScopeComplete()
        {
            // we don't execute this test yet since
            // TransactionScope is not fully supported
            // by this provider
        }

        [Test]
        public override void TestMigrationWithinTransactionScopeRollback()
        {
            // we don't execute this test yet since
            // TransactionScope is not fully supported
            // by this provider
        }
    }
}