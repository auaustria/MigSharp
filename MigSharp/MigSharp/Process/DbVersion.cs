using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

using MigSharp.Core;
using MigSharp.Providers;

namespace MigSharp.Process
{
    internal class DbVersion : IDbVersion
    {
        private readonly DbVersionDataSet _dataSet;
        private readonly DbProviderFactory _factory;

        internal static string TableName { get { return "DbVersion"; } }

        private DbVersion(DbVersionDataSet dataSet, DbProviderFactory factory)
        {
            Debug.Assert(dataSet.DbVersion.TableName == TableName);
            Debug.Assert(dataSet.DbVersion.ModuleColumn.MaxLength == MigrationExportAttribute.MaximumModuleLength);

            _dataSet = dataSet;
            _factory = factory;
        }

        public static DbVersion Create(ConnectionInfo connectionInfo, IProviderFactory providerFactory, IDbConnectionFactory connectionFactory)
        {
            // execute boostrap migration step to ensure that the DbVersion table exists
            var step = new MigrationStep(new BootstrapMigration(), new BootstrapMetaData(), connectionInfo, providerFactory, connectionFactory);
            step.Execute(null);

            // create and fill DataSet
            var dataSet = new DbVersionDataSet();
            DbProviderFactory factory = connectionFactory.GetDbProviderFactory(connectionInfo);
            using (IDbConnection connection = connectionFactory.OpenConnection(connectionInfo))
            {
                DbDataAdapter adapter = CreateAdapter(factory, connection, dataSet);
                adapter.Fill(dataSet.DbVersion); 
            }
            var dbVersion = new DbVersion(dataSet, factory);
            return dbVersion;
        }

        private static DbDataAdapter CreateAdapter(DbProviderFactory factory, IDbConnection connection, DbVersionDataSet dataSet)
        {
            DbCommand selectCommand = factory.CreateCommand();
            selectCommand.Connection = (DbConnection)connection;
            selectCommand.CommandText = string.Format("SELECT {0}, {1}, {2} FROM {3}",
                dataSet.DbVersion.TimestampColumn.ColumnName,
                dataSet.DbVersion.ModuleColumn.ColumnName,
                dataSet.DbVersion.TagColumn.ColumnName,
                dataSet.DbVersion.TableName);
            DbDataAdapter adapter = factory.CreateDataAdapter();
            adapter.SelectCommand = selectCommand;
            return adapter;
        }

        /// <summary>
        /// Used for unit testing.
        /// </summary>
        internal static DbVersion Create(DbVersionDataSet dataSet)
        {
            return new DbVersion(dataSet, null);
        }

        public bool Includes(IMigrationMetaData metaData)
        {
            return _dataSet.DbVersion.FindByTimestampModule(metaData.Timestamp(), metaData.Module) != null;
        }

        public void Update(IMigrationMetaData metaData, IDbConnection connection, IDbTransaction transaction)
        {
            Debug.Assert(!(metaData is BootstrapMetaData));

            DateTime start = DateTime.Now;

            _dataSet.DbVersion.AddDbVersionRow(metaData.Timestamp(), metaData.Module, metaData.Tag);

            DbDataAdapter adapter = CreateAdapter(_factory, connection, _dataSet);
            adapter.SelectCommand.Transaction = (DbTransaction)transaction;
            DbCommandBuilder builder = _factory.CreateCommandBuilder();
            builder.DataAdapter = adapter;
            adapter.InsertCommand = builder.GetInsertCommand();
            //adapter.UpdateCommand = builder.GetUpdateCommand();
            //adapter.DeleteCommand = builder.GetDeleteCommand();
            Log.Info(LogCategory.Performance, "Adapter creation took {0}ms", (DateTime.Now - start).TotalMilliseconds);

            adapter.Update(_dataSet.DbVersion); // write new row to database
            Log.Info(LogCategory.Performance, "Version update took {0}ms", (DateTime.Now - start).TotalMilliseconds);
        }

        private class BootstrapMigration : IMigration
        {
            public void Up(IDatabase db)
            {
                db.CreateTable(TableName).IfNotExists()
                    .WithPrimaryKeyColumn("Timestamp", DbType.DateTime)
                    .WithPrimaryKeyColumn("Module", DbType.StringFixedLength).OfLength(MigrationExportAttribute.MaximumModuleLength)
                    .WithNullableColumn("Tag", DbType.String);
            }
        }

        private class BootstrapMetaData : IMigrationMetaData
        {
            public int Year { get { throw new NotSupportedException(); } }
            public int Month { get { throw new NotSupportedException(); } }
            public int Day { get { throw new NotSupportedException(); } }
            public int Hour { get { throw new NotSupportedException(); } }
            public int Minute { get { throw new NotSupportedException(); } }
            public int Second { get { throw new NotSupportedException(); } }
            public string Tag { get { throw new NotSupportedException(); } }
            public string Module { get { throw new NotSupportedException(); } }
        }
    }
}