﻿using System.Data.Common;
using System.Globalization;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace MigSharp.MySql.NUnit
{
    [TestFixture, Category("MySql")]
    public class MySqlIntegrationTests : MySqlIntegrationTestsBase
    {
        private const string TestDbName = "MigSharp_TestDb";

        protected static string Server
        {
            get
            {
                const string server = "MIGSHARP_MYSQL_SERVER";
                return GetEnvironmentVariable(server);
            }
        }

        protected static string User
        {
            get
            {
                const string user = "MIGSHARP_MYSQL_USER";
                return GetEnvironmentVariable(user);
            }
        }

        protected static string Password
        {
            get
            {
                const string password = "MIGSHARP_MYSQL_PASSWORD";
                return GetEnvironmentVariable(password);
            }
        }

        protected override DbDataAdapter GetDataAdapter(string tableName, string schemaName, bool forUpdating)
        {
            var adapter = new MySqlDataAdapter(string.Format(CultureInfo.InvariantCulture, "SELECT * FROM \"{0}\"", tableName), ConnectionString);
            if (forUpdating)
            {
                var builder = new MySqlCommandBuilder(adapter);
                adapter.InsertCommand = builder.GetInsertCommand();
            }
            return adapter;
        }

        protected override string ConnectionString
        {
            get
            {
                var builder = new MySqlConnectionStringBuilder
                    {
                        Server = Server,
                        Database = TestDbName,
                        UserID = User,
                        Password = Password,
                    };
                return builder.ConnectionString;
            }
        }

        private static string MasterConnectionString
        {
            get
            {
                var builder = new MySqlConnectionStringBuilder
                    {
                        Server = Server,
                        UserID = User,
                        Password = Password
                    };
                return builder.ConnectionString;
            }
        }

        protected override DbPlatform DbPlatform { get { return DbPlatform.MySql5; } }

        public override void Setup()
        {
            base.Setup();

            using (var connection = new MySqlConnection(MasterConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(CultureInfo.InvariantCulture, "CREATE DATABASE `{0}`;", TestDbName);
                    command.ExecuteNonQuery();
                }
            }
        }


        public override void Teardown()
        {
            using (var connection = new MySqlConnection(MasterConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(CultureInfo.InvariantCulture, "DROP DATABASE `{0}`;", TestDbName);
                    command.ExecuteNonQuery();
                }
            }

            MySqlConnection.ClearAllPools();

            base.Teardown();
        }
    }
}