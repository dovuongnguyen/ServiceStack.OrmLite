using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.DataAnnotations;

namespace ServiceStack.OrmLite.Tests.UseCase
{
	[TestFixture]
	public class SimpleUseCase
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			//Inject your database provider here
			OrmLiteConfig.DialectProvider = new SqliteOrmLiteDialectProvider();
		}

		public class User
		{
			public long Id { get; set; }

			[Index]
			public string Name { get; set; }

			public DateTime CreatedDate { get; set; }
		}

		[Alias("Users")]
		public class User2
		{
			[AutoIncrement]
			public long Id { get; set; }

			public long Value { get; set; }
		}

		[Test]
		public void Simple_CRUD_example()
		{
			var path = "~/App_Data/db.sqlite".MapAbsolutePath();
			//using (IDbConnection db = ":memory:".OpenDbConnection())
			using (IDbConnection db = path.OpenDbConnection())
			using (IDbCommand dbCmd = db.CreateCommand())
			{
				dbCmd.CreateTable<User>(true);

				dbCmd.Insert(new User { Id = 1, Name = "A", CreatedDate = DateTime.Now });
				dbCmd.Insert(new User { Id = 2, Name = "B", CreatedDate = DateTime.Now });
				dbCmd.Insert(new User { Id = 3, Name = "B", CreatedDate = DateTime.Now });

				var rowsB = dbCmd.Select<User>("Name = {0}", "B");
				var rowsB1 = dbCmd.Select<User>(user => user.Name == "B");

				Assert.That(rowsB, Has.Count.EqualTo(2));
				Assert.That(rowsB1, Has.Count.EqualTo(2));

				var rowIds = rowsB.ConvertAll(x => x.Id);
				Assert.That(rowIds, Is.EquivalentTo(new List<long> { 2, 3 }));

				rowsB.ForEach(x => dbCmd.Delete(x));

				rowsB = dbCmd.Select<User>("Name = {0}", "B");
				Assert.That(rowsB, Has.Count.EqualTo(0));

				var rowsLeft = dbCmd.Select<User>();
				Assert.That(rowsLeft, Has.Count.EqualTo(1));

				Assert.That(rowsLeft[0].Name, Is.EqualTo("A"));
			}

			File.Delete(path);
		}

		[Test]
		public void Simple_CRUD_example2()
		{
			var path = "~/App_Data/db2.sqlite".MapAbsolutePath();
			if(File.Exists(path))
				File.Delete(path);
			//using (IDbConnection db = ":memory:".OpenDbConnection())
			using (IDbConnection db = path.OpenDbConnection())
			using (IDbCommand dbCmd = db.CreateCommand())
			{
				dbCmd.CommandText = "PRAGMA synchronous = OFF; PRAGMA page_size = 4096; PRAGMA cache_size = 3000; PRAGMA journal_mode = OFF;";
				dbCmd.ExecuteNonQuery();

				dbCmd.CreateTable<User2>(false);

				// we have to do a custom insert because the provider base ignores AutoInc columns
				dbCmd.CommandText = "INSERT INTO Users VALUES(5000000000, -1)";
				dbCmd.ExecuteNonQuery();

				var obj1 = new User2 {Value = 6000000000L};
				dbCmd.Insert(obj1);

				var last = dbCmd.GetLastInsertId();
				Assert.AreEqual(5000000001L, last);

				var obj2 = dbCmd.QueryById<User2>(last);
				Assert.AreEqual(obj1.Value, obj2.Value);
			}
			File.Delete(path);
		}

	}

}