// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;
using System.Collections.Generic;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.SqlServerNorthwind
{
	[Table("Customers")]
	public class Customer
	{
		[Column("CustomerID"  , CanBeNull = false, IsPrimaryKey = true)] public string  CustomerId   { get; set; } = null!; // nchar(5)
		[Column("CompanyName" , CanBeNull = false                     )] public string  CompanyName  { get; set; } = null!; // nvarchar(40)
		[Column("ContactName"                                         )] public string? ContactName  { get; set; } // nvarchar(30)
		[Column("ContactTitle"                                        )] public string? ContactTitle { get; set; } // nvarchar(30)
		[Column("Address"                                             )] public string? Address      { get; set; } // nvarchar(60)
		[Column("City"                                                )] public string? City         { get; set; } // nvarchar(15)
		[Column("Region"                                              )] public string? Region       { get; set; } // nvarchar(15)
		[Column("PostalCode"                                          )] public string? PostalCode   { get; set; } // nvarchar(10)
		[Column("Country"                                             )] public string? Country      { get; set; } // nvarchar(15)
		[Column("Phone"                                               )] public string? Phone        { get; set; } // nvarchar(24)
		[Column("Fax"                                                 )] public string? Fax          { get; set; } // nvarchar(24)

		#region Associations
		/// <summary>
		/// FK_CustomerCustomerDemo_Customers backreference
		/// </summary>
		[Association(ThisKey = nameof(CustomerId), OtherKey = nameof(CustomerCustomerDemo.CustomerId))]
		public IEnumerable<CustomerCustomerDemo> CustomerCustomerDemos { get; set; } = null!;

		/// <summary>
		/// FK_Orders_Customers backreference
		/// </summary>
		[Association(ThisKey = nameof(CustomerId), OtherKey = nameof(Order.CustomerId))]
		public IEnumerable<Order> Orders { get; set; } = null!;
		#endregion
	}
}
