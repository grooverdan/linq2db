// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB.Mapping;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.Access.Both
{
	[Table("Person")]
	public class Person
	{
		[Column("PersonID"  , IsPrimaryKey = true , IsIdentity = true, SkipOnInsert = true, SkipOnUpdate = true)] public int     PersonId   { get; set; } // COUNTER
		[Column("FirstName" , CanBeNull    = false                                                             )] public string  FirstName  { get; set; } = null!; // VarChar(50)
		[Column("LastName"  , CanBeNull    = false                                                             )] public string  LastName   { get; set; } = null!; // VarChar(50)
		[Column("MiddleName"                                                                                   )] public string? MiddleName { get; set; } // VarChar(50)
		[Column("Gender"                                                                                       )] public char    Gender     { get; set; } // VarChar(1)

		#region Associations
		/// <summary>
		/// PersonDoctor backreference
		/// </summary>
		[Association(ThisKey = nameof(PersonId), OtherKey = nameof(Both.Doctor.PersonId))]
		public Doctor? Doctor { get; set; }

		/// <summary>
		/// PersonPatient backreference
		/// </summary>
		[Association(ThisKey = nameof(PersonId), OtherKey = nameof(Both.Patient.PersonId))]
		public Patient? Patient { get; set; }
		#endregion
	}
}
