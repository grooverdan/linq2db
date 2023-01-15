﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using Extensions;
	using Reflection;
	using SqlQuery;

	internal class SqlGenericConstructorExpression : Expression, IEquatable<SqlGenericConstructorExpression>
	{
		public class Assignment
		{
			public static ReadOnlyCollection<Assignment> EmptyCollection = new (new List<Assignment>());

			public Assignment(MemberInfo memberInfo, Expression expression, bool isMandatory, bool isLoaded)
			{
				if (!memberInfo.GetMemberType().IsAssignableFrom(expression.Type))
					throw new InvalidOperationException($"Member '{memberInfo.Name}:{memberInfo.GetMemberType().Name}' cannot accept Expression Type '{expression.Type}'.");

				MemberInfo  = memberInfo;
				Expression  = expression;
				IsMandatory = isMandatory;
				IsLoaded    = isLoaded;
			}

			public MemberInfo MemberInfo  { get;  }
			public Expression Expression  { get; }
			public bool       IsMandatory { get; }
			public bool       IsLoaded    { get; }

			public Assignment WithExpression(Expression expression)
			{
				if (expression == Expression)
					return this;
				return new Assignment(MemberInfo, expression, IsMandatory, IsLoaded);
			}

			public override string ToString()
			{
				return $"{MemberInfo.Name} = {Expression}";
			}

			sealed class AssignmentEqualityComparer : IEqualityComparer<Assignment>
			{
				public bool Equals(Assignment x, Assignment y)
				{
					if (ReferenceEquals(x, y))
					{
						return true;
					}

					if (ReferenceEquals(x, null))
					{
						return false;
					}

					if (ReferenceEquals(y, null))
					{
						return false;
					}

					if (x.GetType() != y.GetType())
					{
						return false;
					}

					return x.MemberInfo.Equals(y.MemberInfo) &&
					       x.IsMandatory == y.IsMandatory    && x.IsLoaded == y.IsLoaded &&
					       ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression);
				}

				public int GetHashCode(Assignment obj)
				{
					unchecked
					{
						var hashCode = obj.MemberInfo.GetHashCode();
						hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression);
						hashCode = (hashCode * 397) ^ obj.IsMandatory.GetHashCode();
						hashCode = (hashCode * 397) ^ obj.IsLoaded.GetHashCode();
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<Assignment> AssignmentComparer { get; } = new AssignmentEqualityComparer();
		}

		public new class Parameter
		{
			public static ReadOnlyCollection<Parameter> EmptyCollection = new (new List<Parameter>());

			public Parameter(Expression expression, Type paramType, MemberInfo? memberInfo)
			{
				MemberInfo = memberInfo;
				Expression = expression;
				ParamType  = paramType;
			}

			public MemberInfo? MemberInfo { get; }
			public Expression  Expression { get; }
			public Type        ParamType  { get; }

			public Parameter WithExpression(Expression expression)
			{
				if (ReferenceEquals(expression, Expression))
					return this;
				return new Parameter(expression, ParamType, MemberInfo);
			}

			public override string ToString()
			{
				if (MemberInfo == null)
					return $"{Expression}";
				return $"{MemberInfo.Name}={Expression}";
			}

			sealed class MemberInfoExpressionParamTypeEqualityComparer : IEqualityComparer<Parameter>
			{
				public bool Equals(Parameter x, Parameter y)
				{
					if (ReferenceEquals(x, y))
					{
						return true;
					}

					if (ReferenceEquals(x, null))
					{
						return false;
					}

					if (ReferenceEquals(y, null))
					{
						return false;
					}

					if (x.GetType() != y.GetType())
					{
						return false;
					}

					return Equals(x.MemberInfo, y.MemberInfo) && x.ParamType.Equals(y.ParamType) &&
					       ExpressionEqualityComparer.Instance.Equals(x.Expression, y.Expression);
				}

				public int GetHashCode(Parameter obj)
				{
					unchecked
					{
						var hashCode = (obj.MemberInfo != null ? obj.MemberInfo.GetHashCode() : 0);
						hashCode = (hashCode * 397) ^ ExpressionEqualityComparer.Instance.GetHashCode(obj.Expression);
						hashCode = (hashCode * 397) ^ obj.ParamType.GetHashCode();
						return hashCode;
					}
				}
			}

			public static IEqualityComparer<Parameter> ParameterComparer { get; } = new MemberInfoExpressionParamTypeEqualityComparer();
		}

		SqlGenericConstructorExpression()
		{
			Parameters  = null!;
			Assignments = null!;
			ObjectType  = null!;
		}

		public SqlGenericConstructorExpression(CreateType createType, Type objectType, ReadOnlyCollection<Parameter>? parameters, ReadOnlyCollection<Assignment>? assignments) : this()
		{
			ObjectType    = objectType;
			ConstructType = createType;
			Parameters    = parameters  ?? Parameter.EmptyCollection;
			Assignments   = assignments ?? Assignment.EmptyCollection;
		}

		public SqlGenericConstructorExpression(SqlGenericConstructorExpression basedOn) : this()
		{
			ObjectType        = basedOn.ObjectType;
			ConstructType     = CreateType.Incompatible;
			Constructor       = basedOn.Constructor;
			ConstructorMethod = basedOn.ConstructorMethod;
			Parameters        = Parameter.EmptyCollection;
			Assignments       = Assignment.EmptyCollection;
		}

		public SqlGenericConstructorExpression(Type objectType, ReadOnlyCollection<MemberBinding> bindings) : this()
		{
			ObjectType    = objectType;
			ConstructType = CreateType.Auto;
			Parameters    = Parameter.EmptyCollection;

			var assignments = GetBindingsAssignments(bindings);
			Assignments = new ReadOnlyCollection<Assignment>(assignments);
		}

		public SqlGenericConstructorExpression(NewExpression newExpression) : this()
		{
			var items = new List<Assignment>(newExpression.Members?.Count ?? 0);

			if (newExpression.Members != null)
			{
				for (var i = 0; i < newExpression.Members.Count; i++)
				{
					var member = newExpression.Members[i];
					items.Add(new Assignment(member, newExpression.Arguments[i], true, false));
				}

				Parameters = Parameter.EmptyCollection;
			}
			else
			{
				Parameters = GetMethodParameters(newExpression.Constructor, newExpression.Arguments);
			}

			Constructor   = newExpression.Constructor;
			ConstructType = CreateType.New;
			ObjectType    = newExpression.Type;
			Assignments   = items.AsReadOnly();
		}

		public SqlGenericConstructorExpression(MethodCallExpression methodCall) : this()
		{
			ConstructorMethod = methodCall.Method;
			ConstructType     = CreateType.New;
			ObjectType        = methodCall.Type;
			Parameters        = GetMethodParameters(methodCall.Method, methodCall.Arguments);
			Assignments       = Assignment.EmptyCollection;
		}

		public static MemberInfo? FindMember(Type inType, ParameterInfo parameter)
		{
			var found = FindMember(TypeAccessor.GetAccessor(inType).Members, parameter);
			return found;
		}

		public static MemberInfo? FindMember(IReadOnlyCollection<MemberAccessor> members, ParameterInfo parameter)
		{
			MemberInfo? exactMatch   = null;
			MemberInfo? nonCaseMatch = null;

			foreach (var member in members)
			{
				if (member.Type == parameter.ParameterType)
				{
					if (member.Name == parameter.Name)
					{
						exactMatch = member.MemberInfo;
						break;
					}

					if (member.Name.Equals(parameter.Name, StringComparison.InvariantCultureIgnoreCase))
					{
						nonCaseMatch = member.MemberInfo;
						break;
					}

				}
			}

			return exactMatch ?? nonCaseMatch;
		}

		public SqlGenericConstructorExpression(MemberInitExpression memberInitExpression)
		{
			if (memberInitExpression.NewExpression.Members != null)
				throw new NotImplementedException();

			Parameters = GetMethodParameters(memberInitExpression.NewExpression.Constructor, memberInitExpression.NewExpression.Arguments);
			
			var items = GetBindingsAssignments(memberInitExpression.Bindings);

			NewExpression = memberInitExpression.NewExpression;
			ConstructType = CreateType.MemberInit;
			ObjectType    = memberInitExpression.Type;
			Assignments   = items.AsReadOnly();
		}

		static ReadOnlyCollection<Parameter> GetMethodParameters(MethodBase method, ReadOnlyCollection<Expression> arguments)
		{
			if (arguments.Count == 0)
				return Parameter.EmptyCollection;

			var methodParameters = method.GetParameters();

			var parameters = new List<Parameter>(methodParameters.Length);
			for (int i = 0; i < arguments.Count; i++)
			{
				var methodParameter = methodParameters[i];
				parameters.Add(new Parameter(arguments[i], methodParameter.ParameterType,
					FindMember(method.GetMemberType(), methodParameter)));
			}

			return parameters.AsReadOnly();
		}

		static List<Assignment> GetBindingsAssignments(IList<MemberBinding> bindings)
		{
			var items = new List<Assignment>(bindings.Count);

			for (var i = 0; i < bindings.Count; i++)
			{
				var binding = bindings[i];
				switch (binding.BindingType)
				{
					case MemberBindingType.Assignment:
					{
						var a = (MemberAssignment)binding;
						items.Add(new Assignment(a.Member, a.Expression, true, false));
						break;
					}

					case MemberBindingType.MemberBinding:
					{
						var memberMemberBinding = (MemberMemberBinding)binding;
						items.Add(new Assignment(memberMemberBinding.Member,
							new SqlGenericConstructorExpression(memberMemberBinding.Member.GetMemberType(),
								memberMemberBinding.Bindings), true, false));
						break;
					}

					default:
						throw new NotImplementedException();
				}
			}

			return items;
		}

		public Expression?      NewExpression     { get; private set; }
		public ConstructorInfo? Constructor       { get; private set; }
		public MethodInfo?      ConstructorMethod { get; private set; }
		public CreateType       ConstructType     { get; private set; }
		public Type             ObjectType        { get; private set; }

		public ReadOnlyCollection<Parameter>  Parameters  { get; private set; }
		public ReadOnlyCollection<Assignment> Assignments { get; private set; }

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => ObjectType;
		public override bool           CanReduce => false;

		public override string ToString()
		{
			var parameters  = string.Join(", ", Parameters.Select(p =>
			{
				if (p.MemberInfo == null)
					return p.Expression.ToString();
				return $"{p.MemberInfo.Name} = {p.Expression}";
			}));

			var assignments = string.Join(",\n", Assignments.Select(a => $"\t{a.MemberInfo.Name} = {a.Expression}"));

			if (!string.IsNullOrEmpty(parameters))
			{
				parameters = "(" + parameters + ")";
			}

			var result = $"construct {Type.Name}[{ConstructType}]{parameters}\n{{\n{assignments}\n}}";

			return result;
		}

		public enum CreateType
		{
			Incompatible,
			Auto,
			Full,
			Keys,
			New,
			MemberInit,
			MethodCall
		}

		public SqlGenericConstructorExpression AppendAssignment(Assignment assignment)
		{
			var newAssignments = new List<Assignment>(Assignments.Count + 1);
			newAssignments.AddRange(Assignments);
			newAssignments.Add(assignment);

			return ReplaceAssignments(newAssignments);
		}

		public SqlGenericConstructorExpression ReplaceAssignments(List<Assignment> assignment)
		{
			var createNew = assignment.Count != Assignments.Count;

			if (!createNew)
			{
				createNew = !Assignments.SequenceEqual(assignment);
			}

			if (!createNew)
				return this;

			var result = new SqlGenericConstructorExpression
			{
				Assignments       = assignment.AsReadOnly(),
				Parameters        = Parameters,
				ObjectType        = ObjectType,
				Constructor       = Constructor,
				ConstructorMethod = ConstructorMethod,
				ConstructType     = ConstructType,
				NewExpression     = NewExpression,
			};

			return result;
		}

		public SqlGenericConstructorExpression ReplaceParameters(List<Parameter> parameters)
		{
			var createNew = parameters.Count != Parameters.Count;

			if (!createNew)
			{
				createNew = !Parameters.SequenceEqual(parameters);
			}

			if (!createNew)
				return this;

			var result = new SqlGenericConstructorExpression
			{
				Parameters        = parameters.AsReadOnly(),
				Assignments       = Assignments,
				ObjectType        = ObjectType,
				Constructor       = Constructor,
				ConstructorMethod = ConstructorMethod,
				ConstructType     = ConstructType,
				NewExpression     = NewExpression,
			};

			return result;
		}

		public static Expression Parse(Expression createExpression)
		{
			switch (createExpression.NodeType)
			{
				case ExpressionType.New:
				{
					return new SqlGenericConstructorExpression((NewExpression)createExpression);
				}

				case ExpressionType.MemberInit:
				{
					return new SqlGenericConstructorExpression((MemberInitExpression)createExpression);
				}

				case ExpressionType.Call:
				{
					//TODO: Do we still need Alias?
					var mc = (MethodCallExpression)createExpression;
					if (mc.IsSameGenericMethod(Methods.LinqToDB.SqlExt.Alias))
						return Parse(mc.Arguments[0]);

					if (mc.IsQueryable())
						return mc;

					if (!mc.Method.IsStatic)
						break;

					if (mc.Method.IsSqlPropertyMethodEx() || mc.IsSqlRow() || mc.Method.DeclaringType == typeof(string))
						break;

					return new SqlGenericConstructorExpression(mc);
				}
			}

			return createExpression;
		}

		static int GetHashCode<T>(ICollection<T> collection, IEqualityComparer<T> comparer)
		{
			unchecked
			{
				var hashCode = 0;
				foreach (var item in collection)
				{
					hashCode = (hashCode * 397) ^ comparer.GetHashCode(item);
				}

				return hashCode;
			}
		}

		static bool EqualsLists<T>(IList<T> list1, IList<T> list2, IEqualityComparer<T> comparer)
		{
			if (list1.Count != list2.Count) return false;

			for (int i = 0; i < list1.Count; i++)
			{
				if (!comparer.Equals(list1[i], list2[i]))
					return false;
			}

			return true;
		}

		public bool Equals(SqlGenericConstructorExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(NewExpression, other.NewExpression) && Equals(Constructor, other.Constructor) &&
			       Equals(ConstructorMethod, other.ConstructorMethod) && ConstructType == other.ConstructType &&
			       ObjectType.Equals(other.ObjectType) &&
			       EqualsLists(Parameters, other.Parameters, Parameter.ParameterComparer) &&
			       EqualsLists(Assignments, other.Assignments, Assignment.AssignmentComparer);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((SqlGenericConstructorExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (NewExpression != null ? NewExpression.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Constructor       != null ? Constructor.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (ConstructorMethod != null ? ConstructorMethod.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (int)ConstructType;
				hashCode = (hashCode * 397) ^ ObjectType.GetHashCode();
				hashCode = (hashCode * 397) ^ GetHashCode(Parameters, Parameter.ParameterComparer);
				hashCode = (hashCode * 397) ^ GetHashCode(Assignments, Assignment.AssignmentComparer);
				return hashCode;
			}
		}

		public static bool operator ==(SqlGenericConstructorExpression? left, SqlGenericConstructorExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlGenericConstructorExpression? left, SqlGenericConstructorExpression? right)
		{
			return !Equals(left, right);
		}
	}
}