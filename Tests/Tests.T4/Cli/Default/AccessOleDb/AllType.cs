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

namespace Cli.Default.Access.OleDb
{
	[Table("AllTypes")]
	public class AllType
	{
		[Column("ID"                      )] public int       Id                       { get; set; } // Long
		[Column("bitDataType"             )] public bool      BitDataType              { get; set; } // Bit
		[Column("smallintDataType"        )] public short?    SmallintDataType         { get; set; } // Short
		[Column("decimalDataType"         )] public decimal?  DecimalDataType          { get; set; } // Decimal(18, 0)
		[Column("intDataType"             )] public int?      IntDataType              { get; set; } // Long
		[Column("tinyintDataType"         )] public byte?     TinyintDataType          { get; set; } // Byte
		[Column("moneyDataType"           )] public decimal?  MoneyDataType            { get; set; } // Currency
		[Column("floatDataType"           )] public double?   FloatDataType            { get; set; } // Double
		[Column("realDataType"            )] public float?    RealDataType             { get; set; } // Single
		[Column("datetimeDataType"        )] public DateTime? DatetimeDataType         { get; set; } // DateTime
		[Column("charDataType"            )] public char?     CharDataType             { get; set; } // CHAR(1)
		[Column("char20DataType"          )] public string?   Char20DataType           { get; set; } // CHAR(20)
		[Column("varcharDataType"         )] public string?   VarcharDataType          { get; set; } // VarChar(20)
		[Column("textDataType"            )] public string?   TextDataType             { get; set; } // LongText
		[Column("ncharDataType"           )] public string?   NcharDataType            { get; set; } // CHAR(20)
		[Column("nvarcharDataType"        )] public string?   NvarcharDataType         { get; set; } // VarChar(20)
		[Column("ntextDataType"           )] public string?   NtextDataType            { get; set; } // LongText
		[Column("binaryDataType"          )] public byte[]?   BinaryDataType           { get; set; } // VARBINARY(10)
		[Column("varbinaryDataType"       )] public byte[]?   VarbinaryDataType        { get; set; } // VARBINARY(510)
		[Column("imageDataType"           )] public byte[]?   ImageDataType            { get; set; } // LongBinary
		[Column("oleObjectDataType"       )] public byte[]?   OleObjectDataType        { get; set; } // LongBinary
		[Column("uniqueidentifierDataType")] public Guid?     UniqueidentifierDataType { get; set; } // GUID
	}
}
