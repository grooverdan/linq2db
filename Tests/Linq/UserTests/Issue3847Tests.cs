using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests.Test3847
{
	[TestFixture]
	public class Issue3847Tests : TestBase
	{
		public class OutfeedTransportOrderDTO
		{
			public virtual Guid Id { get; set; }
		}

		[Test]
		public void Test3847([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllPostgreSQL)] string configuration)
		{
			using var db = GetDataContext(configuration);
			using var tb = db.CreateLocalTable<OutfeedTransportOrderDTO>();

			var lastcheckquery = new Dictionary<Guid, DateTime>()
			{
				{ TestData.Guid1, TestData.DateTime0 }
			}.AsQueryable();

			var nextcheckquery = new Dictionary<Guid, DateTime>()
			{
				{ TestData.Guid2, TestData.DateTime0 }
			}.AsQueryable();

			var qry = from outfeed in tb
					  select new
					  {
						  OutfeedTransportOrder = outfeed,
						  LastCheck = lastcheckquery.Where(x => x.Key == outfeed.Id).Select(x => (DateTime?)x.Value).FirstOrDefault(),
						  NextCheck = nextcheckquery.Where(x => x.Key == outfeed.Id).Select(x => (DateTime?)x.Value).FirstOrDefault(),
					  };

			var d = qry.ToList();
		}
	}
}
