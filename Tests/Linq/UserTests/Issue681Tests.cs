﻿using System;
using System.Linq;
using System.Threading.Tasks;

#if NETFRAMEWORK
using System.ServiceModel;
#else
using Grpc.Core;
#endif

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue681Tests : TestBase
	{
		[Table("Issue681Table")]
		sealed class TestTable
		{
			[PrimaryKey]
			public int ID    { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Table("Issue681Table4")]
		sealed class TestTableWithIdentity
		{
			[PrimaryKey, Identity]
			public int ID { get; set; }

			[Column]
			public int Value { get; set; }
		}

		[Test]
		public async Task TestITable(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) => { t.ToList(); return Task.CompletedTask; });
		}

		[Test]
		public async Task TestInsert(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Insert(new TestTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestUpdate(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Update(new TestTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestDelete(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.Delete(new TestTable() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestInsertOrReplace(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				var record = new TestTable() { ID = 5, Value = 10 };
				// insert
				db.InsertOrReplace(record, databaseName: d, serverName: s, schemaName: u);
				// replace
				db.InsertOrReplace(record, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestInsertWithIdentity(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTableWithIdentity>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				db.InsertWithIdentity(new TestTableWithIdentity() { ID = 5, Value = 10 }, databaseName: d, serverName: s, schemaName: u);
				return Task.CompletedTask;
			});
		}

		[Test]
		public async Task TestCreate(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				try
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", throwExceptionIfNotExists: false);
					db.CreateTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u);
				}
				finally
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", throwExceptionIfNotExists: false);
				}
				return Task.CompletedTask;
			}, TestProvName.AllSqlServer);
		}

		[Test]
		public async Task TestCreateAsync(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, async (db, t, u, d, s) =>
			{
				try
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", throwExceptionIfNotExists: false);
					await db.CreateTableAsync<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u);
				}
				finally
				{
					await db.DropTableAsync<TestTable>(tableName: "Issue681Table2", throwExceptionIfNotExists: false);
				}
			}, TestProvName.AllSqlServer);
		}

		[Test]
		public async Task TestDrop(
			[DataSources] string context,
			[Values] bool withServer,
			[Values] bool withDatabase,
			[Values] bool withSchema)
		{
			await TestTableFQN<TestTable>(context, withServer, withDatabase, withSchema, (db, t, u, d, s) =>
			{
				try
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", throwExceptionIfNotExists: false);
					db.CreateTable<TestTable>(tableName: "Issue681Table2");
				}
				finally
				{
					db.DropTable<TestTable>(tableName: "Issue681Table2", databaseName: d, serverName: s, schemaName: u);
				}
				return Task.CompletedTask;
			}, TestProvName.AllSqlServer);
		}

		public async Task TestTableFQN<TTable>(
			string context,
			bool withServer, bool withDatabase, bool withSchema,
			Func<IDataContext, ITable<TTable>, string?, string?, string?, Task> operation,
			string? withServerThrows = null)
			where TTable: class
		{
			// for SAP HANA cross-server queries see comments how to configure SAP HANA in TestUtils.GetServerName() method
			var throws             = false;
			var throwsSqlException = false;

			string? serverName;
			string? schemaName;
			string? dbName;

			using var _  = new DisableBaseline("Use instance name is SQL", context.IsAnyOf(TestProvName.AllSqlServer) && !context.IsAnyOf(TestProvName.AllSqlAzure) && withServer);
			using var db = GetDataContext(context, testLinqService : false);
			using var t  = db.CreateLocalTable<TTable>();

			if (withServer && (!withDatabase || !withSchema) && context.IsAnyOf(TestProvName.AllSqlServer))
			{
				// SQL Server FQN requires schema and db components for linked-server query
				throws = true;
			}

			if (withServerThrows != null && withServer && context.IsAnyOf(withServerThrows))
			{
				throws = true;
				if (context.IsAnyOf(TestProvName.AllSqlServer))
					throwsSqlException = true;
			}

			if (withServer && withDatabase && withSchema && context.IsAnyOf(TestProvName.AllSqlAzure))
			{
				// linked servers not supported by Azure
				// "Reference to database and/or server name in '...' is not supported in this version of SQL Server."
				throws = true;
				throwsSqlException = true;
			}

			if (withServer && !withDatabase && context.IsAnyOf(TestProvName.AllInformix))
			{
				// Informix requires db name for linked server queries
				throws = true;
			}

			if (withServer && !withSchema && context.IsAnyOf(TestProvName.AllSapHana))
			{
				// SAP HANA requires schema name for linked server queries
				throws = true;
			}

			if (withDatabase && !withSchema && context.IsAnyOf(ProviderName.DB2))
			{
				throws = true;
			}

			using (new DisableLogging())
			{
				serverName = withServer   ? TestUtils.GetServerName  (db, context) : null;
				dbName     = withDatabase ? TestUtils.GetDatabaseName(db, context) : null;
				schemaName = withSchema   ? TestUtils.GetSchemaName  (db, context) : null;
			}

			var table = db.GetTable<TTable>();

			if (withServer  ) table = table.ServerName  (serverName);
			if (withDatabase) table = table.DatabaseName(dbName);
			if (withSchema  ) table = table.SchemaName  (schemaName);

			if (throws && context.Contains(".LinqService"))
			{
#if NETFRAMEWORK
				Assert.ThrowsAsync<FaultException>(() => operation(db, table, schemaName, dbName, serverName));
#else
				Assert.ThrowsAsync<RpcException>(() => operation(db, table, schemaName, dbName, serverName));
#endif
			}
			else if (throws)
			{
				if (throwsSqlException)
					// https://www.youtube.com/watch?v=Qji5x8gBVX4
					Assert.ThrowsAsync(
						((SqlServerDataProvider)((DataConnection)db).DataProvider).Adapter.SqlExceptionType,
						() => operation(db, table, schemaName, dbName, serverName));
				else
					Assert.ThrowsAsync<LinqToDBException>(() => operation(db, table, schemaName, dbName, serverName));
			}
			else
			{
				await operation(db, table, schemaName, dbName, serverName);
			}
		}
	}
}
