﻿using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Extensions;
	using Reflection;
	using SqlQuery;
	using System.Collections.Generic;

	internal sealed class SetOperationBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames = { "Concat", "UnionAll", "Union", "Except", "Intersect", "ExceptAll", "IntersectAll" };

		#region Builder

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.Arguments.Count == 2 && methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence1 = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var sequence2 = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			SetOperation setOperation;
			switch (methodCall.Method.Name)
			{
				case "Concat"       : 
				case "UnionAll"     : setOperation = SetOperation.UnionAll;     break;
				case "Union"        : setOperation = SetOperation.Union;        break;
				case "Except"       : setOperation = SetOperation.Except;       break;
				case "ExceptAll"    : setOperation = SetOperation.ExceptAll;    break;
				case "Intersect"    : setOperation = SetOperation.Intersect;    break;
				case "IntersectAll" : setOperation = SetOperation.IntersectAll; break;
				default:
					throw new ArgumentException($"Invalid method name {methodCall.Method.Name}.");
			}

			var needsEmulation = !builder.DataContext.SqlProviderFlags.IsAllSetOperationsSupported &&
			                     (setOperation == SetOperation.ExceptAll || setOperation == SetOperation.IntersectAll)
			                     ||
			                     !builder.DataContext.SqlProviderFlags.IsDistinctSetOperationsSupported &&
			                     (setOperation == SetOperation.Except || setOperation == SetOperation.Intersect);

			if (needsEmulation)
			{
				// emulation

				var sequence = new SubQueryContext(sequence1);
				var query    = sequence2;
				var except   = query.SelectQuery;

				var sql = sequence.SelectQuery;

				if (setOperation == SetOperation.Except || setOperation == SetOperation.Intersect)
					sql.Select.IsDistinct = true;

				except.ParentSelect = sql;

				if (setOperation == SetOperation.Except || setOperation == SetOperation.ExceptAll)
					sql.Where.Not.Exists(except);
				else
					sql.Where.Exists(except);

				throw new NotImplementedException();
				/*builder.ConvertCompareExpression(query, ExpressionType.Equal, ...)

				var keys1 = sequence.ConvertToSql(null, 0, ConvertFlags.All);
				var keys2 = query.   ConvertToSql(null, 0, ConvertFlags.All);

				if (keys1.Length != keys2.Length)
					throw new InvalidOperationException();

				for (var i = 0; i < keys1.Length; i++)
				{
					except.Where
						.Expr(keys1[i].Sql)
						.Equal
						.Expr(keys2[i].Sql);
				}

				return sequence;*/
			}

			var set1 = new SubQueryContext(sequence1);
			var set2 = new SubQueryContext(sequence2);

			var setOperator = new SqlSetOperator(set2.SelectQuery, setOperation);

			set1.SelectQuery.SetOperators.Add(setOperator);

			var setContext = new SetOperationContext(setOperation, set1, set2, methodCall);


			if (setOperation != SetOperation.UnionAll)
			{
				var sqlExpr = builder.BuildSqlExpression(setContext, new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], setContext), buildInfo.GetFlags());
			}

			return setContext;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		#endregion

		#region Context

		sealed class SetOperationContext : SubQueryContext
		{
			public SetOperationContext(SetOperation setOperation, SubQueryContext sequence1, SubQueryContext sequence2,
				MethodCallExpression                methodCall)
				: base(sequence1)
			{
				_setOperation = setOperation;
				_sequence1    = sequence1;
				_sequence2    = sequence2;
				_methodCall   = methodCall;

				_sequence2.Parent = this;

				_type = _methodCall.Method.GetGenericArguments()[0];
			}

			readonly Type                 _type;
			readonly MethodCallExpression _methodCall;
			readonly SetOperation         _setOperation;
			readonly SubQueryContext      _sequence1;
			readonly SubQueryContext      _sequence2;
			SqlPlaceholderExpression?     _setIdPlaceholder;
			Expression?                   _setIdReference;

			readonly Dictionary<Expression, SqlPlaceholderExpression> _createdSQL = new(ExpressionEqualityComparer.Instance);


			static string? GenerateColumnAlias(Expression expr)
			{
				var     current = expr;
				string? alias   = null;
				while (current is MemberExpression memberExpression)
				{
					if (alias != null)
						alias = memberExpression.Member.Name + "_" + alias;
					else
						alias = memberExpression.Member.Name;
					current = memberExpression.Expression;
				}

				return alias;
			}

			static MethodInfo _keySetIdMethosInfo =
				Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(typeof(int));

			const           string     ProjectionSetIdFieldName = "__projection__set_id__";
			static readonly Expression _setIdFieldName          = Expression.Constant(ProjectionSetIdFieldName);

			Expression MakeConditionalConstructExpression(Type type, Expression leftExpression, Expression rightExpression)
			{
				var sequenceLeftSetId  = Builder.GenerateSetId(_sequence1.SubQuery.SelectQuery.SourceID);
				var sequenceRightSetId = Builder.GenerateSetId(_sequence2.SubQuery.SelectQuery.SourceID);

				if (_setIdReference == null)
				{

					var sqlValueLeft  = new SqlValue(sequenceLeftSetId);
					var sqlValueRight = new SqlValue(sequenceRightSetId);

					var thisRef  = new ContextRefExpression(_type, this);

					_setIdReference = Expression.Call(_keySetIdMethosInfo, thisRef, Expression.Constant(ProjectionSetIdFieldName));

					var leftRef  = new ContextRefExpression(_type, _sequence1);
					var rightRef = new ContextRefExpression(_type, _sequence2);

					var keyLeft  = Expression.Call(_keySetIdMethosInfo, leftRef, _setIdFieldName);
					var keyRight = Expression.Call(_keySetIdMethosInfo, rightRef, _setIdFieldName);

					var leftIdPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence1, sqlValueLeft, keyLeft, alias: ProjectionSetIdFieldName);
					leftIdPlaceholder = (SqlPlaceholderExpression)Builder.UpdateNesting(this, leftIdPlaceholder);

					var rightIdPlaceholder = ExpressionBuilder.CreatePlaceholder(_sequence2, sqlValueRight,
						keyRight, alias: ProjectionSetIdFieldName);
					rightIdPlaceholder = Builder.MakeColumn(SelectQuery, rightIdPlaceholder, asNew: true);

					_setIdPlaceholder = leftIdPlaceholder.WithPath(_setIdReference).WithTrackingPath(_setIdReference);
				}

				if (leftExpression.Type != type)
				{
					leftExpression = Expression.Convert(leftExpression, type);
				}

				if (rightExpression.Type != type)
				{
					rightExpression = Expression.Convert(rightExpression, type);
				}

				var resultExpr = Expression.Condition(
					Expression.Equal(_setIdReference, Expression.Constant(sequenceLeftSetId)),
					leftExpression,
					rightExpression
				);

				return resultExpr;
			}

			Expression BuildProjectionExpression(SubQueryContext context, ProjectFlags projectFlags)
			{
				var thisRef = new ContextRefExpression(_type, this);
				return BuildProjectionExpression(thisRef, context, projectFlags);
			}

			Expression BuildProjectionExpression(Expression path, SubQueryContext context, ProjectFlags projectFlags)
			{
				var correctedPath = SequenceHelper.ReplaceContext(path, this, context);

				var projectionExpression = Builder.ConvertToSqlExpr(context, correctedPath, projectFlags.SqlFlag());
				projectionExpression = Builder.BuildSqlExpression(context, projectionExpression, projectFlags.SqlFlag());

				var remaped = SequenceHelper.RemapToNewPath(projectionExpression, path);

				return remaped;
			}

			// For Set we have to ensure hat columns are not optimized
			protected override bool OptimizeColumns => false;

			bool IsIncompatible(Expression expression)
			{
				if (expression is SqlGenericConstructorExpression generic && generic.ConstructType == SqlGenericConstructorExpression.CreateType.Full)
				{
					var ed = Builder.MappingSchema.GetEntityDescriptor(generic.ObjectType);
					if (ed.InheritanceMapping.Count > 0)
						return true;
				}

				var isIncompatible = null != expression.Find(expression, (_, e) =>
				{
					if (e is MemberExpression || e is ContextRefExpression || e is SqlGenericConstructorExpression)
						return false;

					return true;
				});

				return isIncompatible;
			}

			static bool IsEqualProjections(Expression left, Expression right)
			{
				if (left is SqlGenericConstructorExpression leftGeneric &&
				    right is SqlGenericConstructorExpression rightGeneric)
				{
					if (leftGeneric.ConstructType  == SqlGenericConstructorExpression.CreateType.Full &&
					    rightGeneric.ConstructType == SqlGenericConstructorExpression.CreateType.Full &&
					    leftGeneric.ObjectType     == rightGeneric.ObjectType)
					{
						return true;
					}
				}

				if (ExpressionEqualityComparer.Instance.Equals(left, right))
					return true;

				return false;
			}


			public Expression MergeProjections(Type objectType, IEnumerable<Expression> projections, ref bool incompatible)
			{
				return MergeProjections(objectType,
					projections.Select(e => (e, GetMemberPath(e))).ToList(),
					0, ref incompatible);
			}

			class MemberOrParameter : IEquatable<MemberOrParameter>
			{
				public MemberOrParameter(MemberInfo memberInfo)
				{
					Member = memberInfo;
				}

				public MemberOrParameter(Parameter? parameter)
				{
					Parameter = parameter;
				}

				public readonly MemberInfo? Member;
				public readonly Parameter?  Parameter;

				public bool Equals(MemberOrParameter? other)
				{
					if (ReferenceEquals(null, other))
					{
						return false;
					}

					if (ReferenceEquals(this, other))
					{
						return true;
					}

					return Equals(Member, other.Member) && Equals(Parameter, other.Parameter);
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

					return Equals((MemberOrParameter)obj);
				}

				public override int GetHashCode()
				{
					unchecked
					{
						return ((Member != null ? Member.GetHashCode() : 0) * 397) ^ (Parameter != null ? Parameter.GetHashCode() : 0);
					}
				}

				public static bool operator ==(MemberOrParameter? left, MemberOrParameter? right)
				{
					return Equals(left, right);
				}

				public static bool operator !=(MemberOrParameter? left, MemberOrParameter? right)
				{
					return !Equals(left, right);
				}
			}

			class Parameter
			{
				public Parameter(int paramIndex)
				{
					ParamIndex = paramIndex;
				}

				public readonly MethodInfo? Method = null;
				public readonly int         ParamIndex;
			}

			static List<Expression> CollectDataExpressions(Expression expression)
			{
				var result = new List<Expression>();
				expression.Visit(result, (items, e) =>
				{
					if (e is MemberExpression me)
					{
						var current = e;
						while (current is MemberExpression cm)
						{
							current = cm.Expression;
						}

						if (current is ContextRefExpression)
						{
							items.Add(e);
							return false;
						}
					}

					return true;
				});

				return result;
			}

			static List<MemberOrParameter> GetMemberPath(Expression expr)
			{
				var result  = new List<MemberOrParameter>();
				var current = expr;

				while (true)
				{
					MemberOrParameter item;
					if (current is MemberExpression memberExpression)
					{
						item = new MemberOrParameter(memberExpression.Member.DeclaringType?.GetMemberEx(memberExpression.Member) ?? throw new InvalidOperationException());
						current = memberExpression.Expression;
					}
					else if (current is SqlGenericParamAccessExpression paramAccess)
					{
						throw new NotImplementedException();
						item    = new MemberOrParameter(new Parameter(paramAccess.ParamIndex));
						current = paramAccess.Constructor;
					}
					else break;
					result.Insert(0, item);
				}

				return result;
			}

			Expression MergeProjections(Type objectType, List<(Expression path, List<MemberOrParameter> pathList)> pathList, int level, ref bool incompatible)
			{
				var grouped = pathList.GroupBy(p => p.pathList[level])
					.Where(g => g.Key.Member != null)
					.Select(g => new { g.Key, Members = g.ToList() });

				var assignments  = new List<SqlGenericConstructorExpression.Assignment>();

				foreach (var g in grouped)
				{
					var member = g.Key.Member;

					List<(Expression path, List<MemberOrParameter> pathList)>? newList = null;
					(Expression path, List<MemberOrParameter> pathList)        found   = default;

					foreach (var c in g.Members)
					{
						if (c.pathList.Count == level + 1)
						{
							if (found.path == null)
							{
								found = c;
							}
						}
						else
						{
							newList ??= new();
							newList.Add(c);
						}
					}

					if (newList != null)
					{
						if (found.path != null)
							incompatible = true;

						assignments.Add(new SqlGenericConstructorExpression.Assignment(member!, MergeProjections(member!.GetMemberType(), newList, level + 1, ref incompatible), false, false));
					}
					else
					{
						if (found.path != null)
						{
							assignments.Add(new SqlGenericConstructorExpression.Assignment(member!, found.path, false, false));
						}
					}
				}

				return new SqlGenericConstructorExpression(SqlGenericConstructorExpression.CreateType.Auto, objectType, null, assignments.AsReadOnly());
			}

			static Expression NormalizeToDeclaringTypExpression(Expression expression)
			{
				if (expression is MemberExpression me)
				{
					if (me.Expression is not MemberExpression)
					{
						if (me.Expression.Type != me.Member.DeclaringType && me.Member.DeclaringType != null)
						{
							if (me.Expression is ContextRefExpression contextRef)
								return Expression.MakeMemberAccess(contextRef.WithType(me.Member.DeclaringType),
									me.Member);
						}
					}
					return me.Update(NormalizeToDeclaringTypExpression(me.Expression));
				}

				return expression;
			}

			Expression MatchConstructors(Expression path, Expression expr1, Expression expr2,  ProjectFlags flags)
			{
				if (ExpressionEqualityComparer.Instance.Equals(expr1, path))
				{
					expr1 = Expression.Default(expr1.Type);
				}

				if (ExpressionEqualityComparer.Instance.Equals(expr2, path))
				{
					expr2 = Expression.Default(expr2.Type);
				}

				if (IsEqualProjections(expr1, expr2))
					return expr1;

				if (flags.HasFlag(ProjectFlags.Expression))
				{
					if (IsEqualProjections(expr1, expr2))
						return expr1;

					if (IsIncompatible(expr1) || IsIncompatible(expr2))
					{
						return MakeConditionalExpression(path, expr1, expr2);
					}

					var incompatible = false;
					var sqlExpr = MergeProjections(path.Type, CollectDataExpressions(expr1).Concat(CollectDataExpressions(expr2)), ref incompatible);
					if (sqlExpr is SqlGenericConstructorExpression generic)
					{
						if (incompatible || Builder.TryConstruct(Builder.MappingSchema, generic, this, flags) == null)
						{
							// fallback to set
							return MakeConditionalExpression(path, expr1, expr2);
						}
					}
					return sqlExpr;
				}
				else
				{
					var incompatible = false;
					var sqlExpr = MergeProjections(path.Type, CollectDataExpressions(expr1).Concat(CollectDataExpressions(expr2)), ref incompatible);

					return sqlExpr;
				}
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) &&
				    (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot)))
				{
					return path;
				}

				if (flags.HasFlag(ProjectFlags.Root))
					return path;

				if (_createdSQL.TryGetValue(path, out var foundPlaceholder))
					return foundPlaceholder;

				if (_setIdReference != null && ExpressionEqualityComparer.Instance.Equals(_setIdReference, path))
				{
					return _setIdPlaceholder!;
				}

				var expr1 = BuildProjectionExpression(path, _sequence1, flags);
				var expr2 = BuildProjectionExpression(path, _sequence2, flags);

				if (expr1 is SqlGenericConstructorExpression || expr2 is SqlGenericConstructorExpression)
				{
					return MatchConstructors(path, expr1, expr2, flags);
				}

				var path1 = SequenceHelper.ReplaceContext(path, this, _sequence1);
				var path2 = SequenceHelper.ReplaceContext(path, this, _sequence2);

				var convertFlags = flags.SqlFlag();

				var sql1 = Builder.ConvertToSqlExpr(_sequence1, path1, convertFlags.TestFlag());
				var sql2 = Builder.ConvertToSqlExpr(_sequence2, path2, convertFlags.TestFlag());

				var placeholder1 = sql1 as SqlPlaceholderExpression;
				var placeholder2 = sql2 as SqlPlaceholderExpression;

				if (placeholder1 == null && placeholder2 == null)
					return path;

				if (flags.IsTest())
					return placeholder1 ?? placeholder2!;

				// convert again
				sql1         = Builder.ConvertToSqlExpr(_sequence1, path1, convertFlags);
				sql2         = Builder.ConvertToSqlExpr(_sequence2, path2, convertFlags);
				placeholder1 = sql1 as SqlPlaceholderExpression;
				placeholder2 = sql2 as SqlPlaceholderExpression;

				if (placeholder1 == null)
				{
					placeholder2 = (SqlPlaceholderExpression)Builder.ConvertToSqlExpr(_sequence2, path2, convertFlags);
					placeholder1 = ExpressionBuilder.CreatePlaceholder(_sequence1,
						new SqlValue(QueryHelper.GetDbDataType(placeholder2.Sql), null), path1);
				}
				else if (placeholder2 == null)
				{
					placeholder1 = (SqlPlaceholderExpression)Builder.ConvertToSqlExpr(_sequence1, path1, convertFlags);
					placeholder2 = ExpressionBuilder.CreatePlaceholder(_sequence2.SubQuery,
						new SqlValue(QueryHelper.GetDbDataType(placeholder1.Sql), null), path2);
				}
				else
				{
					placeholder1 = (SqlPlaceholderExpression)Builder.ConvertToSqlExpr(_sequence1, path1, convertFlags);
					placeholder2 = (SqlPlaceholderExpression)Builder.ConvertToSqlExpr(_sequence2, path2, convertFlags);
				}

				placeholder1 =
					(SqlPlaceholderExpression)SequenceHelper.CorrectSelectQuery(placeholder1, _sequence1.SelectQuery);
				placeholder2 =
					(SqlPlaceholderExpression)SequenceHelper.CorrectSelectQuery(placeholder2, _sequence2.SelectQuery);

				var alias   = GenerateColumnAlias(path1);
				var column1 = Builder.MakeColumn(SelectQuery, placeholder1.WithAlias(alias), true);
				var column2 = Builder.MakeColumn(SelectQuery, placeholder2.WithAlias(alias), true);

				var resultPlaceholder = column1;

				_createdSQL.Add(path, resultPlaceholder);

				return resultPlaceholder;
			}

			Expression MakeConditionalExpression(Expression path, Expression expr1, Expression expr2)
			{
				if (_setOperation != SetOperation.UnionAll)
				{
					throw new LinqToDBException(
						$"Could not decide which construction typer to use `query.Select(x => new {expr1.Type.Name} {{ ... }})` to specify projection.");
				}

				return MakeConditionalConstructExpression(path.Type, expr1, expr2);
			}
		}

		#endregion
	}
}
