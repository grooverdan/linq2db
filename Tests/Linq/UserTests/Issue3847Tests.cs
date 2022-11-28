using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests.UserTests.Test3847
{
	public class OutfeedTransportOrderDTO
	{
		public virtual Guid Id { get; set; }
	}

	[TestFixture]
	public class Issue3847Tests : TestBase
	{
		[Test]
		public void Test3847([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllPostgreSQL)] string configuration)
		{
			var ms = new MappingSchema();
			var mb = ms.GetFluentMappingBuilder();
			mb.Entity<OutfeedTransportOrderDTO>()
			   .HasTableName("Test3999_OutfeedTransportOrder");

			using (var db = GetDataContext(configuration, ms))
			{
				using (db.CreateLocalTable<OutfeedTransportOrderDTO>())
				{
					var _lastCheck = new Dictionary<Guid, DateTime>();
					var _nextCheck = new Dictionary<Guid, DateTime>();
					IQueryable<KeyValuePair<Guid, DateTime>> lastcheckquery = _lastCheck.AsQueryable();
					IQueryable<KeyValuePair<Guid, DateTime>> nextcheckquery = _nextCheck.AsQueryable();

					var qry = from outfeed in db.GetTable<OutfeedTransportOrderDTO>()
							  select new
							  {
								  OutfeedTransportOrder = outfeed,
								  LastCheck = lastcheckquery.Where(x => x.Key == outfeed.Id).Select(x => (DateTime?)x.Value).FirstOrDefault(),
								  NextCheck = nextcheckquery.Where(x => x.Key == outfeed.Id).Select(x => (DateTime?)x.Value).FirstOrDefault(),
								 
							  };

					var d = qry.ToList();
					var sql = ((DataConnection)db).LastQuery;
				}				
			}
		}
	}
}
