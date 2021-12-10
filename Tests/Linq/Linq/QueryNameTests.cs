﻿using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryNameTests : TestBase
	{
		[Test]
		public void TableTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.Parent.QueryName("PARENT");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /* PARENT */").Or.Contains("SELECT /*+ QB_NAME(PARENT) */"));
			Assert.That(LastQuery, Is.Not.Contains("(SELECT /* PARENT */").And.Not.Contains("(SELECT /*+ QB_NAME(PARENT) */"));
		}

		[Test]
		public void FromTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Parent.QueryName("PARENT")
				from c in db.Child.QueryName("CHILD")
				where p.ParentID == c.ParentID
				select p;

			_ = q.ToList();

			Assert.That(LastQuery.Clean(), Contains.Substring("FROM(SELECT /* PARENT */".Clean()).Or.Contains("FROM(SELECT /*+ QB_NAME(PARENT) */".Clean()));
			Assert.That(LastQuery.Clean(), Contains.Substring(",(SELECT /* CHILD */".    Clean()).Or.Contains(",(SELECT /*+ QB_NAME(CHILD) */".    Clean()));
		}

		[Test]
		public void MainInlineTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q =
			(
				from c in
				(
					from c in db.Child
					group c by c.ParentID into g
					select new
					{
						ParentID = g.Key,
						Count    = g.Count()
					}
				)
				.QueryName("Inline")
				from p in db.Parent
				where p.ParentID == c.ParentID
				select new
				{
					p,
					c.Count
				}
			)
			.QueryName("Main")
			;

			_ = q.ToList();

			Assert.That(LastQuery.Clean(), Contains.Substring("SELECT /* Main */".  Clean()).Or.Contains("SELECT /*+ QB_NAME(Main) */".  Clean()));
			Assert.That(LastQuery.Clean(), Contains.Substring("SELECT /* Inline */".Clean()).Or.Contains("SELECT /*+ QB_NAME(Inline) */".Clean()));
			Assert.That(LastQuery.Clean(), Is.Not.Contains("(SELECT /* Main */".Clean()).And.Not.Contains("(SELECT /*+ QB_NAME(Main) */".Clean()));
		}
	}
}
