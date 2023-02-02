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

namespace Cli.Default.Access.Both
{
	[Table("TestMerge1")]
	public class TestMerge1
	{
		[Column("Id"             , IsPrimaryKey = true)] public int       Id              { get; set; } // Long
		[Column("Field1"                              )] public int?      Field1          { get; set; } // Long
		[Column("Field2"                              )] public int?      Field2          { get; set; } // Long
		[Column("Field3"                              )] public int?      Field3          { get; set; } // Long
		[Column("Field4"                              )] public int?      Field4          { get; set; } // Long
		[Column("Field5"                              )] public int?      Field5          { get; set; } // Long
		[Column("FieldBoolean"                        )] public bool      FieldBoolean    { get; set; } // Bit
		[Column("FieldString"                         )] public string?   FieldString     { get; set; } // VarChar(20)
		[Column("FieldNString"                        )] public string?   FieldNString    { get; set; } // VarChar(20)
		[Column("FieldChar"                           )] public char?     FieldChar       { get; set; } // CHAR(1)
		[Column("FieldNChar"                          )] public char?     FieldNChar      { get; set; } // CHAR(1)
		[Column("FieldFloat"                          )] public float?    FieldFloat      { get; set; } // Single
		[Column("FieldDouble"                         )] public double?   FieldDouble     { get; set; } // Double
		[Column("FieldDateTime"                       )] public DateTime? FieldDateTime   { get; set; } // DateTime
		[Column("FieldBinary"                         )] public byte[]?   FieldBinary     { get; set; } // VARBINARY(20)
		[Column("FieldGuid"                           )] public Guid?     FieldGuid       { get; set; } // GUID
		[Column("FieldDecimal"                        )] public decimal?  FieldDecimal    { get; set; } // Decimal(24, 10)
		[Column("FieldDate"                           )] public DateTime? FieldDate       { get; set; } // DateTime
		[Column("FieldTime"                           )] public DateTime? FieldTime       { get; set; } // DateTime
		[Column("FieldEnumString"                     )] public string?   FieldEnumString { get; set; } // VarChar(20)
		[Column("FieldEnumNumber"                     )] public int?      FieldEnumNumber { get; set; } // Long
	}
}
