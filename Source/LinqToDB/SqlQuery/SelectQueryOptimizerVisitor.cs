﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqToDB.Common;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
	public class SelectQueryOptimizerVisitor : SqlQueryVisitor
	{
		SqlProviderFlags  _flags             = default!;
		DataOptions       _dataOptions       = default!;
		EvaluationContext _evaluationContext = default!;
		IQueryElement     _rootElement       = default!;
		int               _level             = default!;
		IQueryElement[]   _dependencies      = default!;
		SelectQuery?      _correcting        = default!;
		int               _version;

		SelectQuery?    _parentSelect;
		SqlSetOperator? _currentSetOperator;

		public SelectQueryOptimizerVisitor() : base(VisitMode.Modify)
		{
		}


		public IQueryElement OptimizeQueries(IQueryElement root, SqlProviderFlags flags, DataOptions dataOptions,
			EvaluationContext evaluationContext, IQueryElement rootElement, int level,
			params IQueryElement[] dependencies)
		{
#if DEBUG
			if (root.ElementType == QueryElementType.SelectStatement)
			{

			}
#endif

			_flags             = flags;
			_dataOptions       = dataOptions;
			_evaluationContext = evaluationContext;
			_rootElement       = rootElement;
			_level             = level;
			_dependencies      = dependencies;
			_parentSelect      = default!;

			return ProcessElement(root);
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_flags             = default!;
			_dataOptions       = default!;
			_evaluationContext = default!;
			_rootElement       = default!;
			_level             = default!;
			_dependencies      = default!;
			_parentSelect      = default!;
			_version           = default;
		}

		public override IQueryElement NotifyReplaced(IQueryElement newElement, IQueryElement oldElement)
		{
			++_version;
			return base.NotifyReplaced(newElement, oldElement);
		}

		public override IQueryElement VisitFuncLikePredicate(SqlPredicate.FuncLike element)
		{
			foreach (var arg in element.Function.Parameters)
			{
				if (arg is SelectQuery sq && !sq.OrderBy.IsEmpty && !sq.Select.HasModifier)
				{
					sq.OrderBy.Items.Clear();
				}
			}

			return base.VisitFuncLikePredicate(element);
		}

		public override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			var saveParent = _parentSelect;

			_parentSelect = selectQuery;
			var newQuery = (SelectQuery)base.VisitSqlQuery(selectQuery);

			if (_correcting == null)
			{
				_parentSelect         = saveParent;
				newQuery.ParentSelect = _parentSelect;

				FinalizeAndValidateInternal(newQuery);
			}

			return newQuery;
		}

		void CorrectOrderBy(SelectQuery selectQuery)
		{
			//if (_currentSetOperator?.SelectQuery == selectQuery &&
			//    _currentSetOperator.Operation    != SetOperation.UnionAll)
			//{
			//	selectQuery.OrderBy.Items.Clear();
			//}

			if (!selectQuery.HasSetOperators)
				return;

			if (selectQuery.SetOperators[0].Operation != SetOperation.UnionAll)
			{
				selectQuery.OrderBy.Items.Clear();
			}

			foreach(var setOperator in selectQuery.SetOperators)
			{
				if (setOperator.Operation != SetOperation.UnionAll)
				{
					setOperator.SelectQuery.OrderBy.Items.Clear();
				}
			}
		}

		public override IQueryElement VisitSqlSetOperator(SqlSetOperator element)
		{
			var saveCurrent = _currentSetOperator;
			_currentSetOperator = element;
			base.VisitSqlSetOperator(element);

			_currentSetOperator = saveCurrent;

			return element;
		}

		void OptimizeUnions(SelectQuery selectQuery)
		{
			if (!selectQuery.HasSetOperators)
				return;

			for (var index = 0; index < selectQuery.SetOperators.Count; index++)
			{
				var setOperator = selectQuery.SetOperators[index];

				if (setOperator.SelectQuery.From.Tables.Count == 1 &&
				    setOperator.SelectQuery.From.Tables[0].Source is SelectQuery { HasSetOperators: true } subQuery)
				{
					if (subQuery.SetOperators.All(so => so.Operation == setOperator.Operation))
					{
						var allColumns = setOperator.Operation != SetOperation.UnionAll;

						if (allColumns)
						{
							if (subQuery.Select.Columns.Count != selectQuery.Select.Columns.Count)
								continue;
						}

						var newIndexes =
							new Dictionary<ISqlExpression, int>(Utils.ObjectReferenceEqualityComparer<ISqlExpression>
								.Default);

						for (var i = 0; i < setOperator.SelectQuery.Select.Columns.Count; i++)
						{
							var scol = setOperator.SelectQuery.Select.Columns[i];

							newIndexes[scol.Expression] = i;
						}

						if (!CheckSetColumns(newIndexes, subQuery, setOperator.Operation))
							continue;

						UpdateSetIndexes(newIndexes, subQuery, setOperator.Operation);

						setOperator.Modify(subQuery);
						selectQuery.SetOperators.InsertRange(index + 1, subQuery.SetOperators);
						subQuery.SetOperators.Clear();
						--index;
					}
				}
			}
		}

		static void UpdateSetIndexes(Dictionary<ISqlExpression, int> newIndexes, SelectQuery setQuery, SetOperation setOperation)
		{
			if (setOperation == SetOperation.UnionAll)
			{
				for (var index = 0; index < setQuery.Select.Columns.Count; index++)
				{
					var column = setQuery.Select.Columns[index];
					if (!newIndexes.ContainsKey(column))
					{
						setQuery.Select.Columns.RemoveAt(index);

						foreach (var op in setQuery.SetOperators)
						{
							op.SelectQuery.Select.Columns.RemoveAt(index);
						}

						--index;
					}
				}
			}

			foreach (var pair in newIndexes.OrderBy(x => x.Value))
			{
				var currentIndex = setQuery.Select.Columns.FindIndex(c => ReferenceEquals(c, pair.Key));
				if (currentIndex < 0)
				{
					if (setOperation != SetOperation.UnionAll)
						throw new InvalidOperationException();

					foreach (var op in setQuery.SetOperators)
					{
						op.SelectQuery.Select.Columns.Insert(pair.Value, new SqlColumn(op.SelectQuery, pair.Key));
					}

					continue;
				}

				var newIndex = pair.Value;
				if (currentIndex != newIndex)
				{
					var uc = setQuery.Select.Columns[currentIndex];
					setQuery.Select.Columns.RemoveAt(currentIndex);
					setQuery.Select.Select.Columns.Insert(newIndex, uc);

					// change indexes in SetOperators
					foreach (var op in setQuery.SetOperators)
					{
						var column = op.SelectQuery.Select.Columns[currentIndex];
						op.SelectQuery.Select.Columns.RemoveAt(currentIndex);
						op.SelectQuery.Select.Columns.Insert(newIndex, column);
					}
				}
			}
		}

		static bool CheckSetColumns(Dictionary<ISqlExpression, int> newIndexes, SelectQuery setQuery, SetOperation setOperation)
		{
			foreach (var pair in newIndexes.OrderBy(x => x.Value))
			{
				var currentIndex = setQuery.Select.Columns.FindIndex(c => ReferenceEquals(c, pair.Key));
				if (currentIndex < 0)
				{
					if (setOperation != SetOperation.UnionAll)
						return false;

					if (!QueryHelper.IsConstantFast(pair.Key))
						return false;
				}
			}

			return true;
		}

		void FinalizeAndValidateInternal(SelectQuery selectQuery)
		{
			selectQuery.Visit((this, selectQuery), static (context, e) =>
			{
				if (e is SelectQuery sql && sql != context.selectQuery)
				{
					sql.ParentSelect = context.selectQuery;

					if (sql.IsParameterDependent)
						context.selectQuery.IsParameterDependent = true;
				}
			});

			var currentVersion = _version;

			RemoveEmptyJoins(selectQuery);
			OptimizeGroupBy(selectQuery);
			//OptimizeApplies(selectQuery, _flags.IsApplyJoinSupported);
			//OptimizeSubQueries(selectQuery, _flags.IsApplyJoinSupported);
			//OptimizeApplies(selectQuery,_flags.IsApplyJoinSupported);
			RemoveEmptyJoins(selectQuery);

			OptimizeUnions(selectQuery);
			CorrectOrderBy(selectQuery);

			OptimizeGroupBy(selectQuery);
			OptimizeDistinct(selectQuery);
			OptimizeDistinctOrderBy(selectQuery);
			CorrectColumns(selectQuery);

			if (currentVersion != _version)
			{
				EnsureReferencesCorrected(selectQuery);
			}
		}

		void OptimizeGroupBy(SelectQuery selectQuery)
		{
			if (!selectQuery.GroupBy.IsEmpty)
			{
				// Remove constants.
				//
				for (int i = selectQuery.GroupBy.Items.Count - 1; i >= 0; i--)
				{
					if (QueryHelper.IsConstantFast(selectQuery.GroupBy.Items[i]))
					{
						selectQuery.GroupBy.Items.RemoveAt(i);
					}
				}
			}
		}

		void CorrectColumns(SelectQuery selectQuery)
		{
			if (!selectQuery.GroupBy.IsEmpty && selectQuery.Select.Columns.Count == 0)
			{
				foreach (var item in selectQuery.GroupBy.Items)
				{
					selectQuery.Select.Add(item);
				}
			}
		}

		public static SqlCondition OptimizeCondition(SqlCondition condition)
		{
			if (condition.Predicate is SqlSearchCondition search)
			{
				if (search.Conditions.Count == 1)
				{
					var sc = search.Conditions[0];
					return new SqlCondition(condition.IsNot != sc.IsNot, sc.Predicate, condition.IsOr);
				}
			}
			else if (condition.Predicate.ElementType == QueryElementType.ExprPredicate)
			{
				var exprPredicate = (SqlPredicate.Expr)condition.Predicate;
				if (exprPredicate.Expr1 is ISqlPredicate predicate)
				{
					return new SqlCondition(condition.IsNot, predicate, condition.IsOr);
				}
			}

			if (condition.IsNot && condition.Predicate is IInvertibleElement invertibleElement && invertibleElement.CanInvert())
			{
				return new SqlCondition(false, (ISqlPredicate)invertibleElement.Invert(), condition.IsOr);
			}

			return condition;
		}

		internal static SqlSearchCondition OptimizeSearchCondition(SqlSearchCondition inputCondition, EvaluationContext context)
		{
			var searchCondition = inputCondition;

			void ClearAll()
			{
				searchCondition = new SqlSearchCondition();
			}

			void EnsureCopy()
			{
				if (!ReferenceEquals(searchCondition, inputCondition))
					return;

				searchCondition = new SqlSearchCondition(inputCondition.Conditions.Select(static c => new SqlCondition(c.IsNot, c.Predicate, c.IsOr)));
			}

			for (var i = 0; i < searchCondition.Conditions.Count; i++)
			{
				var cond    = OptimizeCondition(searchCondition.Conditions[i]);
				var newCond = cond;
				if (cond.Predicate.ElementType == QueryElementType.ExprExprPredicate)
				{
					var exprExpr = (SqlPredicate.ExprExpr)cond.Predicate;

					if (cond.IsNot && exprExpr.CanInvert())
					{
						exprExpr = (SqlPredicate.ExprExpr)exprExpr.Invert();
						newCond  = new SqlCondition(false, exprExpr, newCond.IsOr);
					}

					if ((exprExpr.Operator == SqlPredicate.Operator.Equal ||
					     exprExpr.Operator == SqlPredicate.Operator.NotEqual)
					    && exprExpr.Expr1 is SqlValue value1 && value1.Value != null
					    && exprExpr.Expr2 is SqlValue value2 && value2.Value != null
					    && value1.GetType()                                  == value2.GetType())
					{
						newCond = new SqlCondition(newCond.IsNot, new SqlPredicate.Expr(new SqlValue(
							(value1.Value.Equals(value2.Value) == (exprExpr.Operator == SqlPredicate.Operator.Equal)))), newCond.IsOr);
					}

					if ((exprExpr.Operator == SqlPredicate.Operator.Equal ||
					     exprExpr.Operator == SqlPredicate.Operator.NotEqual)
					    && exprExpr.Expr1 is SqlParameter p1 && !p1.CanBeNull
					    && exprExpr.Expr2 is SqlParameter p2 && Equals(p1, p2))
					{
						newCond = new SqlCondition(newCond.IsNot, new SqlPredicate.Expr(new SqlValue(true)), newCond.IsOr);
					}
				}

				if (newCond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr = (SqlPredicate.Expr)newCond.Predicate;

					if (newCond.IsNot)
					{
						var boolValue = QueryHelper.GetBoolValue(expr.Expr1, context);
						if (boolValue != null)
						{
							newCond = new SqlCondition(false, new SqlPredicate.Expr(new SqlValue(!boolValue.Value)), newCond.IsOr);
						}
						else if (expr.Expr1 is SqlSearchCondition expCond && expCond.Conditions.Count == 1)
						{
							if (expCond.Conditions[0].Predicate is IInvertibleElement invertible && invertible.CanInvert())
								newCond = new SqlCondition(false, (ISqlPredicate)invertible.Invert(), newCond.IsOr);
						}
					}
				}

				if (!ReferenceEquals(cond, newCond))
				{
					EnsureCopy();
					searchCondition.Conditions[i] = newCond;
					cond                          = newCond;
				}

				if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
				{
					var expr      = (SqlPredicate.Expr)cond.Predicate;
					var boolValue = QueryHelper.GetBoolValue(expr.Expr1, context);

					if (boolValue != null)
					{
						var   isTrue    = cond.IsNot ? !boolValue.Value : boolValue.Value;
						bool? leftIsOr  = i     > 0 ? searchCondition.Conditions[i - 1].IsOr : null;
						bool? rightIsOr = i + 1 < searchCondition.Conditions.Count ? cond.IsOr : null;

						if (isTrue)
						{
							if ((leftIsOr == true || leftIsOr == null) && (rightIsOr == true || rightIsOr == null))
							{
								ClearAll();
								break;
							}

							EnsureCopy();
							searchCondition.Conditions.RemoveAt(i);
							if (leftIsOr == false && rightIsOr != null)
								searchCondition.Conditions[i - 1].IsOr = rightIsOr.Value;
							--i;
						}
						else
						{
							if (leftIsOr == false)
							{
								EnsureCopy();
								searchCondition.Conditions.RemoveAt(i - 1);
								--i;
							}
							else if (rightIsOr == false)
							{
								EnsureCopy();
								searchCondition.Conditions[i].IsOr = searchCondition.Conditions[i + 1].IsOr;
								searchCondition.Conditions.RemoveAt(i + 1);
								--i;
							}
							else
							{
								if (rightIsOr != null || leftIsOr != null)
								{
									EnsureCopy();
									searchCondition.Conditions.RemoveAt(i);
									if (leftIsOr != null && rightIsOr != null)
										searchCondition.Conditions[i - 1].IsOr = rightIsOr.Value;
									--i;
								}
							}
						}

					}
				}
				else if (cond.Predicate is SqlSearchCondition sc)
				{
					var newSc = OptimizeSearchCondition(sc, context);
					if (!ReferenceEquals(newSc, sc))
					{
						EnsureCopy();
						searchCondition.Conditions[i] = new SqlCondition(cond.IsNot, newSc, cond.IsOr);
						sc                            = newSc;
					}

					if (sc.Conditions.Count == 0)
					{
						EnsureCopy();
						var inlinePredicate = new SqlPredicate.Expr(new SqlValue(!cond.IsNot));
						searchCondition.Conditions[i] =
							new SqlCondition(false, inlinePredicate, searchCondition.Conditions[i].IsOr);
						--i;
					}
					else if (sc.Conditions.Count == 1)
					{
						// reduce nesting
						EnsureCopy();

						var isNot = searchCondition.Conditions[i].IsNot;
						if (sc.Conditions[0].IsNot)
							isNot = !isNot;

						var predicate = sc.Conditions[0].Predicate;
						if (isNot && predicate is IInvertibleElement invertible && invertible.CanInvert())
						{
							predicate = (ISqlPredicate)invertible.Invert();
							isNot     = !isNot;
						}

						var inlineCondition = new SqlCondition(isNot, predicate, searchCondition.Conditions[i].IsOr);

						searchCondition.Conditions[i] = inlineCondition;

						--i;
					}
					else
					{
						if (!cond.IsNot)
						{
							var allIsOr = true;
							foreach (var c in sc.Conditions)
							{
								if (c.IsOr != cond.IsOr)
								{
									allIsOr = false;
									break;
								}
							}

							if (allIsOr)
							{
								// we can merge sub condition
								EnsureCopy();

								var current = (SqlSearchCondition)searchCondition.Conditions[i].Predicate;
								searchCondition.Conditions.RemoveAt(i);

								// insert items and correct their IsOr value
								searchCondition.Conditions.InsertRange(i, current.Conditions);
							}
						}
					}
				}
			}

			if (searchCondition.Conditions.Count == 1)
			{
				var cond = searchCondition.Conditions[0];
				if (!cond.IsNot && cond.Predicate is SqlSearchCondition subSc && subSc.Conditions.Count == 1)
				{
					var subCond = subSc.Conditions[0];
					if (!subCond.IsNot)
						return subSc;
				}
			}

			return searchCondition;
		}

		void EnsureReferencesCorrected(SelectQuery selectQuery)
		{
			if (_correcting != null)
				throw new InvalidOperationException();

			_correcting = selectQuery;

			base.Visit(selectQuery);

			_correcting = null;
		}

		internal void ResolveWeakJoins(SelectQuery selectQuery)
		{
			EnsureReferencesCorrected(selectQuery);

			foreach (var table in selectQuery.From.Tables)
			{
				for (var i = table.Joins.Count - 1; i >= 0; i--)
				{
					var join = table.Joins[i];

					if (join.IsWeak)
					{
						var sources = new HashSet<ISqlTableSource>(QueryHelper.EnumerateAccessibleSources(join.Table));
						var ignore  = new HashSet<IQueryElement> { join };
						if (QueryHelper.IsDependsOnSources(selectQuery, sources, ignore))
						{
							join.IsWeak = false;
							continue;
						}

						var moveNext = false;
						foreach (var d in _dependencies)
						{
							if (QueryHelper.IsDependsOnSources(d, sources, ignore))
							{
								join.IsWeak = false;
								moveNext    = true;
								break;
							}
						}

						if (moveNext)
							continue;

						table.Joins.RemoveAt(i);
					}
				}
			}
		}

		static bool IsLimitedToOneRecord(SelectQuery query)
		{
			if (query.Select.TakeValue is SqlValue value && Equals(value.Value, 1))
				return true;

			if (query.From.Tables.Count == 1 && query.From.Tables[0].Source is SelectQuery subQuery)
				return IsLimitedToOneRecord(subQuery);

			return false;
		}

		static bool IsComplexQuery(SelectQuery query)
		{
			var accessibleSources = new HashSet<ISqlTableSource>();

			var complexFound = false;
			foreach (var source in QueryHelper.EnumerateAccessibleSources(query))
			{
				accessibleSources.Add(source);
				if (source is SelectQuery q && (q.From.Tables.Count != 1 || q.GroupBy.IsEmpty && QueryHelper.EnumerateJoins(q).Any()))
				{
					complexFound = true;
					break;
				}
			}

			if (complexFound)
				return true;

			var usedSources = new HashSet<ISqlTableSource>();
			QueryHelper.CollectUsedSources(query, usedSources);

			return usedSources.Count > accessibleSources.Count;
		}

		void OptimizeDistinct(SelectQuery selectQuery)
		{
			if (!selectQuery.Select.IsDistinct || !selectQuery.Select.OptimizeDistinct)
				return;

			if (IsComplexQuery(selectQuery))
				return;

			if (IsLimitedToOneRecord(selectQuery))
			{
				// we can simplify query if we take only one record
				selectQuery.Select.IsDistinct = false;
				return;
			}

			var table = selectQuery.From.Tables[0];

			var keys = new List<IList<ISqlExpression>>();

			QueryHelper.CollectUniqueKeys(selectQuery, includeDistinct: false, keys);
			QueryHelper.CollectUniqueKeys(table, keys);
			if (keys.Count == 0)
				return;

			var expressions = new HashSet<ISqlExpression>(selectQuery.Select.Columns.Select(static c => c.Expression));
			var foundUnique = false;

			foreach (var key in keys)
			{
				foundUnique = true;
				foreach (var expr in key)
				{
					if (!expressions.Contains(expr))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					break;

				foundUnique = true;
				foreach (var expr in key)
				{
					var underlyingField = QueryHelper.GetUnderlyingField(expr);
					if (underlyingField == null || !expressions.Contains(underlyingField))
					{
						foundUnique = false;
						break;
					}
				}

				if (foundUnique)
					break;
			}

			if (foundUnique)
			{
				// We have found that distinct columns has unique key, so we can remove distinct
				selectQuery.Select.IsDistinct = false;
			}
		}

		static void ApplySubsequentOrder(SelectQuery mainQuery, SelectQuery subQuery)
		{
			if (subQuery.OrderBy.Items.Count > 0)
			{
				var filterItems = mainQuery.Select.IsDistinct || !mainQuery.GroupBy.IsEmpty;

				foreach (var item in subQuery.OrderBy.Items)
				{
					if (filterItems)
					{
						var skip = true;
						foreach (var column in mainQuery.Select.Columns)
						{
							if (column.Expression.Equals(item.Expression))
							{
								skip = false;
								break;
							}
						}

						if (skip)
							continue;
					}

					mainQuery.OrderBy.Expr(item.Expression, item.IsDescending);
				}
			}
		}

		static void ApplySubQueryExtensions(SelectQuery mainQuery, SelectQuery subQuery)
		{
			if (subQuery.SqlQueryExtensions is not null)
				(mainQuery.SqlQueryExtensions ??= new()).AddRange(subQuery.SqlQueryExtensions);
		}

		bool OptimizeApply(SelectQuery parentQuery, HashSet<ISqlTableSource> parentTableSources, SqlTableSource tableSource, SqlJoinedTable joinTable, bool isApplySupported)
		{
			var joinSource = joinTable.Table;

			var optimized = false;

			if (joinSource.Joins.Count > 0)
			{
				var joinSources = new HashSet<ISqlTableSource>(parentTableSources);
				joinSources.Add(joinTable.Table);

				foreach (var join in joinSource.Joins)
				{
					if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply|| join.JoinType == JoinType.FullApply || join.JoinType == JoinType.RightApply)
					{
						if (OptimizeApply(parentQuery, joinSources, joinSource, join, isApplySupported))
							optimized = true;
					}

					joinSources.AddRange(QueryHelper.EnumerateAccessibleSources(join.Table));
				}
			}

			if (!joinTable.CanConvertApply)
				return optimized;

			if (joinSource.Source.ElementType == QueryElementType.SqlQuery)
			{
				var sql   = (SelectQuery)joinSource.Source;
				var isAgg = sql.Select.Columns.Any(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression));

				isApplySupported = isApplySupported && (joinTable.JoinType == JoinType.CrossApply ||
				                                        joinTable.JoinType == JoinType.OuterApply);

				if (isApplySupported && sql.Select.HasModifier && _flags.IsSubQueryTakeSupported)
					return optimized;

				if (isApplySupported && isAgg)
					return optimized;

				if (isAgg)
					return optimized;
				
				var skipValue = sql.Select.SkipValue;
				var takeValue = sql.Select.TakeValue;

				ISqlExpression?       rnExpression = null;
				List<ISqlExpression>? partitionBy  = null;

				if (skipValue != null || takeValue != null)
				{
					var parameters = new List<ISqlExpression>();

					var sources = QueryHelper.EnumerateAccessibleSources(sql).ToArray();
					var found   = new HashSet<ISqlExpression>();
					QueryHelper.CollectDependencies(sql.Where, sources, found, singleColumnLevel: true);
					if (found.Count > 0)
					{
						partitionBy = found.ToList();
					}

					var rnBuilder = new StringBuilder(); 
					rnBuilder.Append("ROW_NUMBER() OVER (");

					if (partitionBy != null)
					{
						rnBuilder.Append("PARTITION BY ");
						for (int i = 0; i < partitionBy.Count; i++)
						{
							if (i > 0)
								rnBuilder.Append(", ");

							rnBuilder.Append($"{{{parameters.Count}}}");
							parameters.Add(partitionBy[i]);
						}
					}


					var orderByItems = sql.OrderBy.Items.ToList();

					if (sql.OrderBy.IsEmpty)
					{
						if (partitionBy != null)
							orderByItems.Add(new SqlOrderByItem(partitionBy[0], false));
						else if (!_flags.SupportsRowNumberWithoutOrderBy)
						{
							if (sql.Select.Columns.Count == 0)
							{
								throw new InvalidOperationException("OrderBy not specified for limited recordset.");
							}
							orderByItems.Add(new SqlOrderByItem(sql.Select.Columns[0].Expression, false));
						}
					}

					if (orderByItems.Count > 0)
					{
						if (partitionBy != null)
							rnBuilder.Append(' ');

						rnBuilder.Append("ORDER BY ");
						for (int i = 0; i < orderByItems.Count; i++)
						{
							if (i > 0)
								rnBuilder.Append(", ");

							var orderItem = orderByItems[i];
							rnBuilder.Append($"{{{parameters.Count}}}");
							if (orderItem.IsDescending)
								rnBuilder.Append(" DESC");

							parameters.Add(orderItem.Expression);
						}
					}

					rnBuilder.Append(')');

					rnExpression = new SqlExpression(typeof(long), rnBuilder.ToString(), Precedence.Primary,
						SqlFlags.IsWindowFunction, ParametersNullabilityType.NotNullable, null, parameters.ToArray());
				}

				var whereToIgnore = new HashSet<IQueryElement> { sql.Where, sql.Select };

				// add join conditions
				foreach (var join in sql.From.Tables.SelectMany(t => t.Joins))
				{
					if (join.JoinType == JoinType.Inner || join.JoinType == JoinType.Left)
						whereToIgnore.Add(join.Condition);
				}

				// we cannot optimize apply because reference to parent sources are used inside the query
				if (QueryHelper.IsDependsOnSources(sql, parentTableSources, whereToIgnore))
					return optimized;

				var searchCondition = new List<SqlCondition>();

				var conditions = sql.Where.SearchCondition.Conditions;

				var toIgnore = new HashSet<IQueryElement> { joinTable };

				if (conditions.Count > 0)
				{
					for (var i = conditions.Count - 1; i >= 0; i--)
					{
						var condition = conditions[i];

						var contains = QueryHelper.IsDependsOnSources(condition, parentTableSources, toIgnore);

						if (contains)
						{
							searchCondition.Insert(0, condition);
							conditions.RemoveAt(i);
						}
					}
				}

				if (rnExpression != null)
				{
					// processing ROW_NUMBER

					var rnColumn = sql.Select.AddNewColumn(rnExpression);
					rnColumn.RawAlias = "rn";

					sql.Select.SkipValue = null;
					sql.Select.TakeValue = null;

					if (skipValue != null)
					{
						searchCondition.Add(new SqlCondition(false,
							new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.Greater, skipValue, null)));

						if (takeValue != null)
						{
							searchCondition.Add(new SqlCondition(false, new SqlPredicate.ExprExpr(rnColumn,
								SqlPredicate.Operator.LessOrEqual, new SqlBinaryExpression(skipValue.SystemType!,
									skipValue, "+", takeValue), null)));
						}
					}
					else if (takeValue != null)
					{
						searchCondition.Add(new SqlCondition(false,
							new SqlPredicate.ExprExpr(rnColumn, SqlPredicate.Operator.LessOrEqual, takeValue, null)));

					}
				}

				var toCheck = new HashSet<ISqlTableSource>();

				toCheck.AddRange(QueryHelper.EnumerateAccessibleSources(sql));

				for (int i = 0; i < searchCondition.Count; i++)
				{
					var cond = searchCondition[i];
					var newCond = cond.Convert((sql, toCheck, toIgnore, isAgg), static (visitor, e) =>
					{
						if (e.ElementType == QueryElementType.Column || e.ElementType == QueryElementType.SqlField)
						{
							if (QueryHelper.IsDependsOnSources(e, visitor.Context.toCheck))
							{
								if (e is not SqlColumn clm || clm.Parent != visitor.Context.sql)
								{
									var newExpr = visitor.Context.sql.Select.AddNewColumn((ISqlExpression)e);

									if (visitor.Context.isAgg)
									{
										visitor.Context.sql.Select.GroupBy.Items.Add((ISqlExpression)e);
									}

									return newExpr;
								}
							}
						}

						return e;
					});

					searchCondition[i] = newCond;
				}

				var newJoinType = joinTable.JoinType switch
				{
					JoinType.CrossApply => JoinType.Inner,
					JoinType.OuterApply => JoinType.Left,
					JoinType.FullApply  => JoinType.Full,
					JoinType.RightApply => JoinType.Right,
					_ => throw new InvalidOperationException($"Invalid APPLY Join: {joinTable.JoinType}"),
				};

				joinTable.JoinType = newJoinType;
				joinTable.Condition.Conditions.AddRange(searchCondition);

				optimized = true;
				/*
				if (newJoinType == JoinType.Full)
				{
					joinTable.Condition = QueryHelper.CorrectComparisonForJoin(joinTable.Condition);
				}
				*/
			}

			return optimized;
		}

		static void ConcatSearchCondition(SqlWhereClause where1, SqlWhereClause where2)
		{
			if (where1.IsEmpty)
			{
				where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
			else
			{
				if (where1.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc1 = new SqlSearchCondition();

					sc1.Conditions.AddRange(where1.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Clear();
					where1.SearchCondition.Conditions.Add(new SqlCondition(false, sc1));
				}

				if (where2.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc2 = new SqlSearchCondition();

					sc2.Conditions.AddRange(where2.SearchCondition.Conditions);

					where1.SearchCondition.Conditions.Add(new SqlCondition(false, sc2));
				}
				else
					where1.SearchCondition.Conditions.AddRange(where2.SearchCondition.Conditions);
			}
		}

		bool IsColumnExpressionValid(SelectQuery parentQuery, SqlColumn column, ISqlExpression columnExpression)
		{
			if (columnExpression.ElementType == QueryElementType.Column ||
			    columnExpression.ElementType == QueryElementType.SqlField)
			{
				return true;
			}

			var underlying = QueryHelper.UnwrapExpression(columnExpression, false);
			if (!ReferenceEquals(underlying, columnExpression))
			{
				return IsColumnExpressionValid(parentQuery, column, underlying);
			}

			// check that column has at least one reference
			//

			int found = 1;
			parentQuery.VisitParentFirstAll(e =>
			{
				if (e.ElementType == QueryElementType.SelectClause)
					return false;

				if (ReferenceEquals(e, column))
				{
					++found;
				}

				return found > 1;
			});

			return found < 2;
		}

		bool MoveSubQueryUp(SelectQuery selectQuery, SqlTableSource tableSource)
		{
			if (tableSource.Source is not SelectQuery subQuery)
				return false;

			if (subQuery.From.Tables.Count != 1)
				return false;

			if (_currentSetOperator?.SelectQuery == selectQuery || selectQuery.HasSetOperators)
			{
				// processing parent query as part of Set operation
				//

				if (subQuery.Select.HasModifier)
					return false;

				/*
				if (!subQuery.Select.GroupBy.IsEmpty || !subQuery.Select.Where.IsEmpty)
					return false;
					*/

				if (!subQuery.Select.OrderBy.IsEmpty)
				{
					if (selectQuery.HasSetOperators && selectQuery.SetOperators[0].Operation == SetOperation.UnionAll)
						return false;
				}

				/*
				if (QueryHelper.EnumerateAccessibleSources(subQuery).Skip(1).Take(2).Count() > 1)
					return false;
			*/
			}

			if (!subQuery.GroupBy.IsEmpty && !selectQuery.GroupBy.IsEmpty)
				return false;

			if (selectQuery.Select.IsDistinct)
			{
				// Common check for Distincts

				if (subQuery.Select.SkipValue    != null || subQuery.Select.TakeValue    != null ||
				    selectQuery.Select.SkipValue != null || selectQuery.Select.TakeValue != null)
				{
					return false;
				}

				// Common column check for Distincts

				foreach (var parentColumn in selectQuery.Select.Columns)
				{
					if (parentColumn.Expression is not SqlColumn column || column.Parent != subQuery)
					{
						return false;
					}
				}
			}


			if (subQuery.Select.HasModifier && selectQuery.Select.HasModifier)
			{
				// handling case when we have two DISTINCT
				// Note, columns already checked above
				//
				if (!subQuery.Select.IsDistinct || !selectQuery.Select.IsDistinct)
				{
					return false;
				}
			}

			if (subQuery.Select.HasModifier || !subQuery.GroupBy.IsEmpty)
			{
				if (tableSource.Joins.Count > 0)
					return false;
				if (selectQuery.From.Tables.Count > 1)
					return false;
			}

			if (subQuery.Select.HasModifier || !subQuery.Where.IsEmpty)
			{
				if (tableSource.Joins.Any(j => j.JoinType == JoinType.Right || j.JoinType == JoinType.RightApply ||
				                               j.JoinType == JoinType.Full  || j.JoinType == JoinType.FullApply))
				{
					return false;
				}
			}

			if (subQuery.Select.Columns.Any(c => QueryHelper.IsAggregationOrWindowFunction(c.Expression) || !IsColumnExpressionValid(selectQuery, c, c.Expression)))
				return false;

			if (subQuery.HasSetOperators)
			{
				if (selectQuery.Select.Columns.Count != subQuery.Select.Columns.Count)
				{
					if (subQuery.SetOperators.Any(so => so.Operation != SetOperation.UnionAll))
						return false;
				}

				if (!selectQuery.Select.Where.IsEmpty || !selectQuery.Select.Having.IsEmpty || selectQuery.Select.HasModifier)
					return false;

				var operation = subQuery.SetOperators[0].Operation;

				if (_currentSetOperator != null && _currentSetOperator.Operation != operation)
					return false;

				if (!subQuery.SetOperators.All(so => so.Operation == operation))
					return false;

				if (selectQuery.HasSetOperators && !selectQuery.SetOperators.All(so => so.Operation == operation))
					return false;

				var newIndexes =
					new Dictionary<ISqlExpression, int>(Utils.ObjectReferenceEqualityComparer<ISqlExpression>
						.Default);

				for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
				{
					var scol = selectQuery.Select.Columns[i];

					newIndexes[scol.Expression] = i;
				}

				if (!CheckSetColumns(newIndexes, subQuery, operation))
					return false;

				UpdateSetIndexes(newIndexes, subQuery, operation);

				selectQuery.SetOperators.InsertRange(0, subQuery.SetOperators);
				subQuery.SetOperators.Clear();
			}

			// Actual modification starts from this point
			//

			selectQuery.QueryName ??= subQuery.QueryName;

			if (!subQuery.Where.IsEmpty)
			{
				ConcatSearchCondition(selectQuery.Where, subQuery.Where);
			}

			if (!subQuery.GroupBy.IsEmpty)
			{
				selectQuery.GroupBy.Items.AddRange(subQuery.GroupBy.Items);
			}

			if (!subQuery.Having.IsEmpty)
			{
				ConcatSearchCondition(selectQuery.Having, subQuery.Having);
			}

			if (subQuery.Select.IsDistinct) 
				selectQuery.Select.IsDistinct = true;

			if (subQuery.Select.TakeValue != null)
			{
				selectQuery.Select.TakeValue = subQuery.Select.TakeValue;
			}

			if (subQuery.Select.SkipValue != null)
			{
				selectQuery.Select.SkipValue = subQuery.Select.SkipValue;
			}

			/*if (selectQuery.Select.Columns.Count == 0)
			{
				foreach(var column in subQuery.Select.Columns)
				{
					selectQuery.Select.AddColumn(column.Expression);
				}
			}*/

			foreach (var column in subQuery.Select.Columns)
			{
				NotifyReplaced(column.Expression, column);
			}

			var subQueryTableSource = subQuery.From.Tables[0];
			tableSource.Joins.InsertRange(0, subQueryTableSource.Joins);

			tableSource.Source = subQueryTableSource.Source;

			if (subQuery.HasUniqueKeys)
			{
				subQueryTableSource.UniqueKeys.AddRange(subQuery.UniqueKeys);
			}

			ApplySubQueryExtensions(selectQuery, subQuery);

			if (subQuery.OrderBy.Items.Count > 0 && !selectQuery.Select.Columns.All(static c => QueryHelper.IsAggregationOrWindowFunction(c.Expression)))
			{
				ApplySubsequentOrder(selectQuery, subQuery);
			}

			return true;
		}

		bool JoinMoveSubQueryUp(SelectQuery selectQuery, SqlJoinedTable joinTable)
		{
			if (joinTable.Table.Source is not SelectQuery subQuery)
				return false;

			if (subQuery.From.Tables.Count != 1)
				return false;

			if (!subQuery.GroupBy.IsEmpty)
				return false;

			if (subQuery.Select.HasModifier)
				return false;

			if (subQuery.HasSetOperators)
				return false;

			if (!subQuery.GroupBy.IsEmpty)
				return false;

			if (joinTable.JoinType != JoinType.Inner && joinTable.JoinType != JoinType.Left)
				return false;

			if (subQuery.Select.Columns.Any(c => QueryHelper.IsAggregationOrWindowFunction(c.Expression) || !IsColumnExpressionValid(selectQuery, c, c.Expression)))
				return false;

			// Actual modification starts from this point
			//

			if (!subQuery.Where.IsEmpty)
			{
				joinTable.Condition.EnsureConjunction().Conditions.AddRange(subQuery.Where.SearchCondition.Conditions);
			}

			if (selectQuery.Select.Columns.Count == 0)
			{
				foreach(var column in subQuery.Select.Columns)
				{
					selectQuery.Select.AddColumn(column.Expression);
				}
			}

			foreach (var column in subQuery.Select.Columns)
			{
				NotifyReplaced(column.Expression, column);
			}

			var subQueryTableSource = subQuery.From.Tables[0];
			joinTable.Table.Joins.AddRange(subQueryTableSource.Joins);
			joinTable.Table.Source = subQueryTableSource.Source;
			if (joinTable.Table.RawAlias == null && subQueryTableSource.RawAlias != null)
				joinTable.Table.Alias = subQueryTableSource.RawAlias;

			return true;
		}

		public override IQueryElement VisitSqlFromClause(SqlFromClause element)
		{
			element = (SqlFromClause)base.VisitSqlFromClause(element);

			if (_correcting != null)
				return element;

			var currentVersion = _version;

			OptimizeSubQueries(element.SelectQuery);

			if (currentVersion != _version)
				EnsureReferencesCorrected(element.SelectQuery);

			MoveOuterJoinsToSubQuery(element.SelectQuery);

			if (OptimizeApplies(element.SelectQuery, _flags.IsApplyJoinSupported))
				OptimizeSubQueries(element.SelectQuery);

			ResolveWeakJoins(element.SelectQuery);

			return element;
		}

		void OptimizeSubQueries(SelectQuery selectQuery)
		{
			var replaced    = false;

			for (var i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var tableSource = selectQuery.From.Tables[i];
				if (MoveSubQueryUp(selectQuery, tableSource))
				{
					replaced = true;
					--i; // repeat again

					continue;
				}

				if (tableSource.Joins.Count > 0)
				{
					foreach (var join in tableSource.Joins)
					{
						if (JoinMoveSubQueryUp(selectQuery, join))
							replaced = true;
					}
				}
			}

			if (replaced)
			{
				base.VisitSqlFromClause(selectQuery.From);
				base.VisitSqlSelectClause(selectQuery.Select);
			}
		}

		bool OptimizeApplies(SelectQuery selectQuery, bool isApplySupported)
		{
			EnsureReferencesCorrected(selectQuery);

			var tableSources = new HashSet<ISqlTableSource>();

			var optimized = false;

			foreach (var table in selectQuery.From.Tables)
			{
				tableSources.Add(table.Source);

				if (table.Source is SelectQuery sq)
					tableSources.AddRange(QueryHelper.EnumerateAccessibleSources(sq));

				foreach (var join in table.Joins)
				{
					if (join.JoinType == JoinType.CrossApply || join.JoinType == JoinType.OuterApply|| join.JoinType == JoinType.FullApply|| join.JoinType == JoinType.RightApply)
					{
						if (OptimizeApply(selectQuery, tableSources, table, join, isApplySupported))
							optimized = true;
					}

					join.Visit(tableSources, static (tableSources, e) =>
					{
						if (e is ISqlTableSource ts && !tableSources.Contains(ts))
							tableSources.Add(ts);
					});
				}
			}

			return optimized;
		}

		void RemoveEmptyJoins(SelectQuery selectQuery)
		{
			if (_flags.IsCrossJoinSupported)
				return;

			for (var tableIndex = 0; tableIndex < selectQuery.From.Tables.Count; tableIndex++)
			{
				var table = selectQuery.From.Tables[tableIndex];
				for (var joinIndex = 0; joinIndex < table.Joins.Count; joinIndex++)
				{
					var join = table.Joins[joinIndex];
					if (join.JoinType == JoinType.Inner && join.Condition.Conditions.Count == 0)
					{
						selectQuery.From.Tables.Insert(tableIndex + 1, join.Table);
						table.Joins.RemoveAt(joinIndex);
						--joinIndex;
					}
				}
			}
		}

		public override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			expression = base.VisitSqlColumnExpression(column, expression);

			expression = QueryHelper.SimplifyColumnExpression(expression);
			
			return expression;
		}

		void OptimizeDistinctOrderBy(SelectQuery selectQuery)
		{
			// algorythm works with whole Query, so skipping sub optimizations

			if (_level > 0)
				return;

			var information = new QueryInformation(selectQuery);

			foreach (var query in information.GetQueriesParentFirst())
			{
				// removing duplicate order items
				query.OrderBy.Items.RemoveDuplicates(static o => o.Expression, Utils.ObjectReferenceEqualityComparer<ISqlExpression>.Default);

				// removing sorting for subselects
				if (QueryHelper.CanRemoveOrderBy(query, _flags, information))
				{
					query.OrderBy.Items.Clear();
					continue;
				}

				if (query.Select.IsDistinct)
				{
					QueryHelper.TryRemoveDistinct(query, information);
				}

				if (query.Select.IsDistinct && !query.Select.OrderBy.IsEmpty)
				{
					// nothing to do - DISTINCT ORDER BY supported
					if (_flags.IsDistinctOrderBySupported)
						continue;

					if (_dataOptions.LinqOptions.KeepDistinctOrdered)
					{
						// trying to convert to GROUP BY quivalent
						QueryHelper.TryConvertOrderedDistinctToGroupBy(query, _flags);
					}
					else
					{
						// removing ordering if no select columns
						var projection = new HashSet<ISqlExpression>(query.Select.Columns.Select(static c => c.Expression));
						for (var i = query.OrderBy.Items.Count - 1; i >= 0; i--)
						{
							if (!projection.Contains(query.OrderBy.Items[i].Expression))
								query.OrderBy.Items.RemoveAt(i);
						}
					}
				}
			}
		}

		static bool IsLimitedToOneRecord(SelectQuery selectQuery, EvaluationContext context, out bool byTake)
		{
			if (selectQuery.Select.TakeValue != null &&
			    selectQuery.Select.TakeValue.TryEvaluateExpression(context, out var takeValue))
			{
				byTake = true;
				if (takeValue is int intValue)
				{
					return intValue == 1;
				}
			}

			byTake = false;

			if (selectQuery.Select.Columns.Count == 1)
			{
				var column = selectQuery.Select.Columns[0];
				if (QueryHelper.IsAggregationFunction(column.Expression))
					return true;

				if (selectQuery.Select.From.Tables.Count == 0)
					return true;
			}

			return false;
		}

		static bool IsUniqueUsage(SelectQuery rootQuery, SqlColumn column)
		{
			int counter = 0;

			rootQuery.VisitParentFirstAll(e =>
			{
				// do not search in the same query
				if (e is SelectQuery sq && sq == column.Parent)
					return false;

				if (e == column)
				{
					++counter;
				}

				return counter < 2;
			});

			return counter <= 1;
		}

		void MoveOuterJoinsToSubQuery(SelectQuery selectQuery)
		{
			if (!_flags.IsSubQueryColumnSupported)
				return;

			var currentVersion = _version;

			EvaluationContext? evaluationContext = null;

			foreach (var sq in QueryHelper.EnumerateAccessibleSources(selectQuery).OfType<SelectQuery>())
			{
				for (var ti = 0; ti < sq.From.Tables.Count; ti++)
				{
					var table = sq.From.Tables[ti];
					for (int j = table.Joins.Count - 1; j >= 0; j--)
					{
						var join = table.Joins[j];
						if (join.JoinType == JoinType.OuterApply || join.JoinType == JoinType.CrossApply ||
						    join.JoinType == JoinType.Left)
						{
							evaluationContext ??= new EvaluationContext();

							if (join.Table.Source is SelectQuery tsQuery &&
							    tsQuery.Select.Columns.Count == 1        &&
							    IsLimitedToOneRecord(tsQuery, evaluationContext, out var byTake))
							{
								if (byTake && !_flags.IsSubQueryTakeSupported)
									continue;

								// where we can start analyzing that we can move join to subquery
								var testedColumn = tsQuery.Select.Columns[0];

								if (!IsUniqueUsage(sq, testedColumn))
								{
									QueryHelper.MoveDuplicateUsageToSubQuery(sq);
									// will be processed in the next step
									ti = -1;
									break;
								}

								if (testedColumn.Expression is SqlFunction function)
								{
									if (function.IsAggregate)
									{
										if (!_flags.IsCountSubQuerySupported)
											continue;
									}
								}

								var mainQuery = table.Source as SelectQuery;
								/*
								if (mainQuery?.Select.HasModifier == true)
									continue;
									*/

								// moving whole join to subquery

								table.Joins.RemoveAt(j);
								tsQuery.Where.ConcatSearchCondition(join.Condition);

								if (mainQuery != null)
								{
									// moving into FROM query

									var idx       = mainQuery.Select.Add(tsQuery);
									var newColumn = mainQuery.Select.Columns[idx];
									newColumn.RawAlias = testedColumn.RawAlias;

									NotifyReplaced(testedColumn, newColumn);

									foreach (var c in mainQuery.Select.Columns)
									{
										NotifyReplaced(c.Expression, c);
									}

									newColumn.Expression = mainQuery;
								}
								else
								{
									// replacing column with subquery

									NotifyReplaced(tsQuery, testedColumn);
								}
							}
						}
					}
				}
			}

			if (_version != currentVersion)
				EnsureReferencesCorrected(selectQuery);
		}

	}
}