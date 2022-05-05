// ---------------------------------------------------------------------------------------------------
// <auto-generated>
// This code was generated by LinqToDB scaffolding tool (https://github.com/linq2db/linq2db).
// Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ---------------------------------------------------------------------------------------------------

using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1573, 1591
#nullable enable

namespace Cli.Default.Access.Both
{
	public partial class TestDataDB : DataConnection
	{
		public TestDataDB()
		{
			InitDataContext();
		}

		public TestDataDB(string configuration)
			: base(configuration)
		{
			InitDataContext();
		}

		public TestDataDB(LinqToDBConnectionOptions<TestDataDB> options)
			: base(options)
		{
			InitDataContext();
		}

		partial void InitDataContext();

		public ITable<AllType>             AllTypes             => this.GetTable<AllType>();
		public ITable<Child>               Children             => this.GetTable<Child>();
		public ITable<DataTypeTest>        DataTypeTests        => this.GetTable<DataTypeTest>();
		public ITable<Doctor>              Doctors              => this.GetTable<Doctor>();
		public ITable<Dual>                Duals                => this.GetTable<Dual>();
		public ITable<GrandChild>          GrandChildren        => this.GetTable<GrandChild>();
		public ITable<InheritanceChild>    InheritanceChildren  => this.GetTable<InheritanceChild>();
		public ITable<InheritanceParent>   InheritanceParents   => this.GetTable<InheritanceParent>();
		public ITable<LinqDataType>        LinqDataTypes        => this.GetTable<LinqDataType>();
		public ITable<Parent>              Parents              => this.GetTable<Parent>();
		public ITable<Patient>             Patients             => this.GetTable<Patient>();
		public ITable<Person>              People               => this.GetTable<Person>();
		public ITable<TestIdentity>        TestIdentities       => this.GetTable<TestIdentity>();
		public ITable<TestMerge1>          TestMerge1           => this.GetTable<TestMerge1>();
		public ITable<TestMerge2>          TestMerge2           => this.GetTable<TestMerge2>();
		public ITable<LinqDataTypesQuery>  LinqDataTypesQueries => this.GetTable<LinqDataTypesQuery>();
		public ITable<LinqDataTypesQuery1> LinqDataTypesQuery1  => this.GetTable<LinqDataTypesQuery1>();
		public ITable<LinqDataTypesQuery2> LinqDataTypesQuery2  => this.GetTable<LinqDataTypesQuery2>();
		public ITable<PatientSelectAll>    PatientSelectAll     => this.GetTable<PatientSelectAll>();
		public ITable<PersonSelectAll>     PersonSelectAll      => this.GetTable<PersonSelectAll>();
		public ITable<ScalarDataReader>    ScalarDataReaders    => this.GetTable<ScalarDataReader>();
	}

	public static partial class ExtensionMethods
	{
		#region Table Extensions
		public static DataTypeTest? Find(this ITable<DataTypeTest> table, int dataTypeId)
		{
			return table.FirstOrDefault(e => e.DataTypeId == dataTypeId);
		}

		public static Task<DataTypeTest?> FindAsync(this ITable<DataTypeTest> table, int dataTypeId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.DataTypeId == dataTypeId, cancellationToken);
		}

		public static Doctor? Find(this ITable<Doctor> table, int personId)
		{
			return table.FirstOrDefault(e => e.PersonId == personId);
		}

		public static Task<Doctor?> FindAsync(this ITable<Doctor> table, int personId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PersonId == personId, cancellationToken);
		}

		public static InheritanceChild? Find(this ITable<InheritanceChild> table, int inheritanceChildId)
		{
			return table.FirstOrDefault(e => e.InheritanceChildId == inheritanceChildId);
		}

		public static Task<InheritanceChild?> FindAsync(this ITable<InheritanceChild> table, int inheritanceChildId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.InheritanceChildId == inheritanceChildId, cancellationToken);
		}

		public static InheritanceParent? Find(this ITable<InheritanceParent> table, int inheritanceParentId)
		{
			return table.FirstOrDefault(e => e.InheritanceParentId == inheritanceParentId);
		}

		public static Task<InheritanceParent?> FindAsync(this ITable<InheritanceParent> table, int inheritanceParentId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.InheritanceParentId == inheritanceParentId, cancellationToken);
		}

		public static Patient? Find(this ITable<Patient> table, int personId)
		{
			return table.FirstOrDefault(e => e.PersonId == personId);
		}

		public static Task<Patient?> FindAsync(this ITable<Patient> table, int personId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PersonId == personId, cancellationToken);
		}

		public static Person? Find(this ITable<Person> table, int personId)
		{
			return table.FirstOrDefault(e => e.PersonId == personId);
		}

		public static Task<Person?> FindAsync(this ITable<Person> table, int personId, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.PersonId == personId, cancellationToken);
		}

		public static TestIdentity? Find(this ITable<TestIdentity> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<TestIdentity?> FindAsync(this ITable<TestIdentity> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static TestMerge1? Find(this ITable<TestMerge1> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<TestMerge1?> FindAsync(this ITable<TestMerge1> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}

		public static TestMerge2? Find(this ITable<TestMerge2> table, int id)
		{
			return table.FirstOrDefault(e => e.Id == id);
		}

		public static Task<TestMerge2?> FindAsync(this ITable<TestMerge2> table, int id, CancellationToken cancellationToken = default)
		{
			return table.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
		}
		#endregion
	}
}
