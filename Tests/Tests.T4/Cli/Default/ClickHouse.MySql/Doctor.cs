// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.ClickHouse.MySql
{
	[Table("Doctor")]
	public class Doctor
	{
		[Column("PersonID", IsPrimaryKey = true , SkipOnUpdate = true)] public int    PersonId { get; set; } // Int32
		[Column("Taxonomy", CanBeNull    = false                     )] public string Taxonomy { get; set; } = null!; // String
	}
}
