// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;
using System;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.SqlServerNorthwind
{
	[Table("Sales Totals by Amount", IsView = true)]
	public class SalesTotalsByAmount
	{
		[Column("SaleAmount"                    )] public decimal?  SaleAmount  { get; set; } // money
		[Column("OrderID"                       )] public int       OrderId     { get; set; } // int
		[Column("CompanyName", CanBeNull = false)] public string    CompanyName { get; set; } = null!; // nvarchar(40)
		[Column("ShippedDate"                   )] public DateTime? ShippedDate { get; set; } // datetime
	}
}
