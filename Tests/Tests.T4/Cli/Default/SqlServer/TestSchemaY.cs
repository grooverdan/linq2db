// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.SqlServer
{
	[Table("TestSchemaY")]
	public class TestSchemaY
	{
		[Column("TestSchemaXID"      )] public int TestSchemaXid       { get; set; } // int
		[Column("ParentTestSchemaXID")] public int ParentTestSchemaXid { get; set; } // int
		[Column("OtherID"            )] public int OtherId             { get; set; } // int

		#region Associations
		/// <summary>
		/// FK_TestSchemaY_OtherID
		/// </summary>
		[Association(CanBeNull = false, ThisKey = nameof(TestSchemaXid), OtherKey = nameof(SqlServer.TestSchemaX.TestSchemaXid))]
		public TestSchemaX TestSchemaX { get; set; } = null!;

		/// <summary>
		/// FK_TestSchemaY_ParentTestSchemaX
		/// </summary>
		[Association(CanBeNull = false, ThisKey = nameof(ParentTestSchemaXid), OtherKey = nameof(SqlServer.TestSchemaX.TestSchemaXid))]
		public TestSchemaX ParentTestSchemaX { get; set; } = null!;
		#endregion
	}
}
