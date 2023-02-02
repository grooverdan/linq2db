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

namespace Cli.Default.Access.Odbc
{
	[Table("AllTypes")]
	public class AllType
	{
		[Column("ID"                      , IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int       Id                       { get; set; } // COUNTER
		[Column("bitDataType"                                                                          )] public bool      BitDataType              { get; set; } // BIT
		[Column("smallintDataType"                                                                     )] public short?    SmallintDataType         { get; set; } // SMALLINT
		[Column("decimalDataType"                                                                      )] public decimal?  DecimalDataType          { get; set; } // DECIMAL(18, 0)
		[Column("intDataType"                                                                          )] public int?      IntDataType              { get; set; } // INTEGER
		[Column("tinyintDataType"                                                                      )] public byte?     TinyintDataType          { get; set; } // BYTE
		[Column("moneyDataType"                                                                        )] public decimal?  MoneyDataType            { get; set; } // CURRENCY
		[Column("floatDataType"                                                                        )] public double?   FloatDataType            { get; set; } // DOUBLE
		[Column("realDataType"                                                                         )] public float?    RealDataType             { get; set; } // REAL
		[Column("datetimeDataType"                                                                     )] public DateTime? DatetimeDataType         { get; set; } // DATETIME
		[Column("charDataType"                                                                         )] public char?     CharDataType             { get; set; } // CHAR(1)
		[Column("char20DataType"                                                                       )] public string?   Char20DataType           { get; set; } // CHAR(20)
		[Column("varcharDataType"                                                                      )] public string?   VarcharDataType          { get; set; } // VARCHAR(20)
		[Column("textDataType"                                                                         )] public string?   TextDataType             { get; set; } // LONGCHAR
		[Column("ncharDataType"                                                                        )] public string?   NcharDataType            { get; set; } // CHAR(20)
		[Column("nvarcharDataType"                                                                     )] public string?   NvarcharDataType         { get; set; } // VARCHAR(20)
		[Column("ntextDataType"                                                                        )] public string?   NtextDataType            { get; set; } // LONGCHAR
		[Column("binaryDataType"                                                                       )] public byte[]?   BinaryDataType           { get; set; } // BINARY(10)
		[Column("varbinaryDataType"                                                                    )] public byte[]?   VarbinaryDataType        { get; set; } // VARBINARY(510)
		[Column("imageDataType"                                                                        )] public byte[]?   ImageDataType            { get; set; } // LONGBINARY
		[Column("oleObjectDataType"                                                                    )] public byte[]?   OleObjectDataType        { get; set; } // LONGBINARY
		[Column("uniqueidentifierDataType"                                                             )] public Guid?     UniqueidentifierDataType { get; set; } // GUID
	}
}
