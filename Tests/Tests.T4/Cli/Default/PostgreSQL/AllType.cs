// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;
using NpgsqlTypes;
using System;
using System.Collections;
using System.Net;
using System.Net.NetworkInformation;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.PostgreSQL
{
	[Table("AllTypes")]
	public class AllType
	{
		[Column("ID"                 , IsPrimaryKey = true, IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int                         Id                  { get; set; } // integer
		[Column("bigintDataType"                                                                                       )] public long?                       BigintDataType      { get; set; } // bigint
		[Column("numericDataType"                                                                                      )] public decimal?                    NumericDataType     { get; set; } // numeric
		[Column("smallintDataType"                                                                                     )] public short?                      SmallintDataType    { get; set; } // smallint
		[Column("intDataType"                                                                                          )] public int?                        IntDataType         { get; set; } // integer
		[Column("moneyDataType"                                                                                        )] public decimal?                    MoneyDataType       { get; set; } // money
		[Column("doubleDataType"                                                                                       )] public double?                     DoubleDataType      { get; set; } // double precision
		[Column("realDataType"                                                                                         )] public float?                      RealDataType        { get; set; } // real
		[Column("timestampDataType"                                                                                    )] public DateTime?                   TimestampDataType   { get; set; } // timestamp (6) without time zone
		[Column("timestampTZDataType"                                                                                  )] public DateTimeOffset?             TimestampTzDataType { get; set; } // timestamp (6) with time zone
		[Column("dateDataType"                                                                                         )] public DateTime?                   DateDataType        { get; set; } // date
		[Column("timeDataType"                                                                                         )] public TimeSpan?                   TimeDataType        { get; set; } // time without time zone
		[Column("timeTZDataType"                                                                                       )] public DateTimeOffset?             TimeTzDataType      { get; set; } // time with time zone
		[Column("intervalDataType"                                                                                     )] public TimeSpan?                   IntervalDataType    { get; set; } // interval
		[Column("intervalDataType2"                                                                                    )] public TimeSpan?                   IntervalDataType2   { get; set; } // interval
		[Column("charDataType"                                                                                         )] public char?                       CharDataType        { get; set; } // character(1)
		[Column("char20DataType"                                                                                       )] public string?                     Char20DataType      { get; set; } // character(20)
		[Column("varcharDataType"                                                                                      )] public string?                     VarcharDataType     { get; set; } // character varying(20)
		[Column("textDataType"                                                                                         )] public string?                     TextDataType        { get; set; } // text
		[Column("binaryDataType"                                                                                       )] public byte[]?                     BinaryDataType      { get; set; } // bytea
		[Column("uuidDataType"                                                                                         )] public Guid?                       UuidDataType        { get; set; } // uuid
		[Column("bitDataType"                                                                                          )] public BitArray?                   BitDataType         { get; set; } // bit(3)
		[Column("booleanDataType"                                                                                      )] public bool?                       BooleanDataType     { get; set; } // boolean
		[Column("colorDataType"                                                                                        )] public string?                     ColorDataType       { get; set; } // color
		[Column("pointDataType"                                                                                        )] public NpgsqlPoint?                PointDataType       { get; set; } // point
		[Column("lsegDataType"                                                                                         )] public NpgsqlLSeg?                 LsegDataType        { get; set; } // lseg
		[Column("boxDataType"                                                                                          )] public NpgsqlBox?                  BoxDataType         { get; set; } // box
		[Column("pathDataType"                                                                                         )] public NpgsqlPath?                 PathDataType        { get; set; } // path
		[Column("polygonDataType"                                                                                      )] public NpgsqlPolygon?              PolygonDataType     { get; set; } // polygon
		[Column("circleDataType"                                                                                       )] public NpgsqlCircle?               CircleDataType      { get; set; } // circle
		[Column("lineDataType"                                                                                         )] public NpgsqlLine?                 LineDataType        { get; set; } // line
		[Column("inetDataType"                                                                                         )] public IPAddress?                  InetDataType        { get; set; } // inet
		[Column("cidrDataType"                                                                                         )] public ValueTuple<IPAddress, int>? CidrDataType        { get; set; } // cidr
		[Column("macaddrDataType"                                                                                      )] public PhysicalAddress?            MacaddrDataType     { get; set; } // macaddr
		[Column("macaddr8DataType"                                                                                     )] public PhysicalAddress?            Macaddr8DataType    { get; set; } // macaddr8
		[Column("jsonDataType"                                                                                         )] public string?                     JsonDataType        { get; set; } // json
		[Column("jsonbDataType"                                                                                        )] public string?                     JsonbDataType       { get; set; } // jsonb
		[Column("xmlDataType"                                                                                          )] public string?                     XmlDataType         { get; set; } // xml
		[Column("varBitDataType"                                                                                       )] public BitArray?                   VarBitDataType      { get; set; } // bit varying
		[Column("strarray"                                                                                             )] public string[]?                   Strarray            { get; set; } // text[]
		[Column("intarray"                                                                                             )] public int[]?                      Intarray            { get; set; } // integer[]
		[Column("int2darray"                                                                                           )] public int[][]?                    Int2Darray          { get; set; } // integer[][]
		[Column("longarray"                                                                                            )] public long[]?                     Longarray           { get; set; } // bigint[]
		[Column("intervalarray"                                                                                        )] public TimeSpan[]?                 Intervalarray       { get; set; } // interval[]
		[Column("doublearray"                                                                                          )] public double[]?                   Doublearray         { get; set; } // double precision[]
		[Column("numericarray"                                                                                         )] public decimal[]?                  Numericarray        { get; set; } // numeric[]
		[Column("decimalarray"                                                                                         )] public decimal[]?                  Decimalarray        { get; set; } // numeric[]
	}
}
