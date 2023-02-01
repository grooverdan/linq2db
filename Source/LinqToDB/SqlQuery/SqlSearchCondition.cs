﻿using System;

namespace LinqToDB.SqlQuery
{
	public class SqlSearchCondition : ConditionBase<SqlSearchCondition, SqlSearchCondition.Next>, ISqlPredicate, ISqlExpression, IInvertibleElement
	{
		public SqlSearchCondition()
		{
		}

		public SqlSearchCondition(SqlCondition condition)
		{
			Conditions.Add(condition);
		}

		public SqlSearchCondition(SqlCondition condition1, SqlCondition condition2)
		{
			Conditions.Add(condition1);
			Conditions.Add(condition2);
		}

		public SqlSearchCondition(IEnumerable<SqlCondition> list)
		{
			Conditions.AddRange(list);
		}

		public class Next
		{
			internal Next(SqlSearchCondition parent)
			{
				_parent = parent;
			}

			readonly SqlSearchCondition _parent;

			public SqlSearchCondition Or  => _parent.SetOr(true);
			public SqlSearchCondition And => _parent.SetOr(false);

			public ISqlExpression  ToExpr() { return _parent; }
		}

		public List<SqlCondition> Conditions { get; private set; } = new();

		public void Modify(List<SqlCondition> conditions)
		{
			Conditions = conditions;
		}

		protected override SqlSearchCondition Search => this;

		protected override Next GetNext()
		{
			return new Next(this);
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IPredicate Members

		public int Precedence
		{
			get
			{
				if (Conditions.Count == 0) return SqlQuery.Precedence.Unknown;
				if (Conditions.Count == 1) return Conditions[0].Precedence;

				return Conditions.Select(_ =>
					_.IsNot ? SqlQuery.Precedence.LogicalNegation :
						_.IsOr  ? SqlQuery.Precedence.LogicalDisjunction :
							SqlQuery.Precedence.LogicalConjunction).Min();
			}
		}

		public Type SystemType => typeof(bool);

		ISqlExpression ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			foreach (var condition in Conditions)
				condition.Predicate.Walk(options, context, func);

			return func(context, this);
		}

		#endregion

		#region IInvertibleElement Members

		public bool CanInvert()
		{
			return Conditions.Count > 0 && Conditions.Count(c => c.IsNot) > Conditions.Count / 2;
		}

		public IQueryElement Invert()
		{
			if (Conditions.Count == 0)
			{
				return new SqlSearchCondition(new SqlCondition(false,
					new SqlPredicate.ExprExpr(new SqlValue(1), SqlPredicate.Operator.Equal, new SqlValue(0), null)));
			}

			var newConditions = Conditions.Select(c =>
			{
				var condition = new SqlCondition(!c.IsNot, c.Predicate, !c.IsOr);
				return condition;
			});

			return new SqlSearchCondition(newConditions);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return this == other;
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool CanBeNull => false;

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return other is ISqlPredicate otherPredicate
				&& Equals(otherPredicate, comparer);
		}

		public bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlSearchCondition otherCondition
				|| Conditions.Count != otherCondition.Conditions.Count)
				return false;

			for (var i = 0; i < Conditions.Count; i++)
				if (!Conditions[i].Equals(otherCondition.Conditions[i], comparer))
					return false;

			return true;
		}

		#endregion

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.SearchCondition;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			foreach (IQueryElement c in Conditions)
				writer.AppendElement(c);

			if (Conditions.Count > 0)
				writer.Length -= 5;

			writer.RemoveVisited(this);

			return writer;
		}

		#endregion
	}
}
