﻿using System;
using System.Globalization;

namespace LinqToDB.SqlQuery
{
	partial class QueryHelper
	{
		public static SqlParameterValue GetParameterValue(this SqlParameter parameter, IReadOnlyParameterValues? parameterValues)
		{
			if (parameterValues != null && parameterValues.TryGetValue(parameter, out var value))
			{
				return value;
			}
			return new SqlParameterValue(parameter.Value, parameter.Type);
		}

		public static bool TryEvaluateExpression(this IQueryElement expr, EvaluationContext context, out object? result)
		{
			(result, var success) = expr.TryEvaluateExpression(context);
			return success;
		}

		public static bool IsMutable(this IQueryElement expr)
		{
			if (expr.CanBeEvaluated(false))
				return false;
			return expr.CanBeEvaluated(true);
		}

		public static bool CanBeEvaluated(this IQueryElement expr, bool withParameters)
		{
			return expr.TryEvaluateExpression(new EvaluationContext(withParameters ? SqlParameterValues.Empty : null), out _);
		}

		public static bool CanBeEvaluated(this IQueryElement expr, EvaluationContext context)
		{
			return expr.TryEvaluateExpression(context, out _);
		}

		internal static (object? value, bool success) TryEvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			if (!context.TryGetValue(expr, out var info))
			{
				if (TryEvaluateExpressionInternal(expr, context, out var result))
				{
					context.Register(expr, result);
					return (result, true);
				}
				else
				{
					context.RegisterError(expr);
					return (result, false);
				}
			}

			return info.Value;
		}

		static bool TryEvaluateExpressionInternal(this IQueryElement expr, EvaluationContext context, out object? result)
		{
			result = null;
			switch (expr.ElementType)
			{
				case QueryElementType.SqlValue           :
				{
					var sqlValue = (SqlValue)expr;
					result = sqlValue.Value;
					return true;
				}
				case QueryElementType.SqlParameter       :
				{
					var sqlParameter = (SqlParameter)expr;

					if (context.ParameterValues == null)
					{
						return false;
					}

					var parameterValue = sqlParameter.GetParameterValue(context.ParameterValues);

					result = parameterValue.ProviderValue;
					return true;
				}
				case QueryElementType.IsNullPredicate:
				{
					var isNullPredicate = (SqlPredicate.IsNull)expr;
					if (!isNullPredicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;
					result = isNullPredicate.IsNot == (value != null);
					return true;
				}
				case QueryElementType.ExprExprPredicate:
				{
					var exprExpr = (SqlPredicate.ExprExpr)expr;
					/*
					var reduced = exprExpr.Reduce(context, TODO);
					if (!ReferenceEquals(reduced, expr))
						return TryEvaluateExpression(reduced, context, out result);
						*/

					if (!exprExpr.Expr1.TryEvaluateExpression(context, out var value1) ||
					    !exprExpr.Expr2.TryEvaluateExpression(context, out var value2))
						return false;

					if (value1 != null && value2 != null)
					{
						if (value1.GetType().IsEnum != value2.GetType().IsEnum)
						{
							return false;
						}
					}

					switch (exprExpr.Operator)
					{
						case SqlPredicate.Operator.Equal:
						{
							if (value1 == null)
							{
								result = value2 == null;
							}
							else
							{
								result = (value2 != null) && value1.Equals(value2);
							}
							break;
						}
						case SqlPredicate.Operator.NotEqual:
						{
							if (value1 == null)
							{
								result = value2 != null;
							}
							else
							{
								result = value2 == null || !value1.Equals(value2);
							}
							break;
						}
						default:
						{
							if (!(value1 is IComparable comp1) || !(value2 is IComparable comp2))
							{
								result = false;
								return true;
							}

							switch (exprExpr.Operator)
							{
								case SqlPredicate.Operator.Greater:
									result = comp1.CompareTo(comp2) > 0;
									break;
								case SqlPredicate.Operator.GreaterOrEqual:
									result = comp1.CompareTo(comp2) >= 0;
									break;
								case SqlPredicate.Operator.NotGreater:
									result = !(comp1.CompareTo(comp2) > 0);
									break;
								case SqlPredicate.Operator.Less:
									result = comp1.CompareTo(comp2) < 0;
									break;
								case SqlPredicate.Operator.LessOrEqual:
									result = comp1.CompareTo(comp2) <= 0;
									break;
								case SqlPredicate.Operator.NotLess:
									result = !(comp1.CompareTo(comp2) < 0);
									break;

								default:
									return false;

							}
							break;
						}
					}

					return true;
				}
				case QueryElementType.IsTruePredicate:
				{
					var isTruePredicate = (SqlPredicate.IsTrue)expr;
					if (!isTruePredicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;

					if (value == null)
					{
						result = false;
						return true;
					}

					if (value is bool boolValue)
					{
						result = boolValue != isTruePredicate.IsNot;
						return true;
					}

					return false;
				}
				case QueryElementType.SqlBinaryExpression:
				{
					var binary = (SqlBinaryExpression)expr;
					if (!binary.Expr1.TryEvaluateExpression(context, out var leftEvaluated))
						return false;
					if (!binary.Expr2.TryEvaluateExpression(context, out var rightEvaluated))
						return false;
					dynamic? left  = leftEvaluated;
					dynamic? right = rightEvaluated;
					if (left == null || right == null)
						return true;
					switch (binary.Operation)
					{
						case "+" : result = left + right; break;
						case "-" : result = left - right; break;
						case "*" : result = left * right; break;
						case "/" : result = left / right; break;
						case "%" : result = left % right; break;
						case "^" : result = left ^ right; break;
						case "&" : result = left & right; break;
						case "<" : result = left < right; break;
						case ">" : result = left > right; break;
						case "<=": result = left <= right; break;
						case ">=": result = left >= right; break;
						default:
							return false;
					}

					return true;
				}
				case QueryElementType.SqlFunction        :
				{
					var function = (SqlFunction)expr;

					switch (function.Name)
					{
						case "CASE":
						{
							if (function.Parameters.Length != 3)
							{
								return false;
							}

							if (!function.Parameters[0]
								.TryEvaluateExpression(context, out var cond))
								return false;

							if (!(cond is bool))
							{
								return false;
							}

							if ((bool)cond!)
								return function.Parameters[1]
									.TryEvaluateExpression(context, out result);
							else
								return function.Parameters[2]
									.TryEvaluateExpression(context, out result);
						}

						case "Length":
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.Length;
									return true;
								}
							}

							return false;
						}

						case PseudoFunctions.TO_LOWER:
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.ToLower(CultureInfo.InvariantCulture);
									return true;
								}
							}

							return false;
						}

						case PseudoFunctions.TO_UPPER:
						{
							if (function.Parameters[0]
								.TryEvaluateExpression(context, out var strValue))
							{
								if (strValue == null)
									return true;
								if (strValue is string str)
								{
									result = str.ToUpper(CultureInfo.InvariantCulture);
									return true;
								}
							}

							return false;
						}

						default:
							return false;
					}
				}

				case QueryElementType.SearchCondition    :
				{
					var cond     = (SqlSearchCondition)expr;

					if (cond.Conditions.Count == 0)
					{
						result = true;
						return true;
					}

					for (var i = 0; i < cond.Conditions.Count; i++)
					{
						var condition = cond.Conditions[i];
						if (condition.TryEvaluateExpression(context, out var evaluated))
						{
							if (evaluated is bool boolValue)
							{
								if (i == cond.Conditions.Count - 1 || condition.IsOr == boolValue)
								{
									result = boolValue;
									return true;
								}
							}
							else if (!condition.IsOr)
							{
								return false;
							}
						}
					}

					return false;
				}
				case QueryElementType.ExprPredicate      :
				{
					var predicate = (SqlPredicate.Expr)expr;
					if (!predicate.Expr1.TryEvaluateExpression(context, out var value))
						return false;

					result = value;
					return true;
				}
				case QueryElementType.Condition          :
				{
					var cond = (SqlCondition)expr;
					if (cond.Predicate.TryEvaluateExpression(context, out var evaluated))
					{
						if (evaluated is bool boolValue)
						{
							result = cond.IsNot ? !boolValue : boolValue;
							return true;
						}
						else
						{
							return false;
						}
					}

					return false;
				}
				case QueryElementType.SqlNullabilityExpression:
				{
					var nullability = (SqlNullabilityExpression)expr;
					if (nullability.SqlExpression.TryEvaluateExpression(context, out var evaluated))
					{
						result = evaluated;
						return true;
					}

					return false;
				}

				default:
				{
					return false;
				}
			}
		}

		public static object? EvaluateExpression(this IQueryElement expr, EvaluationContext context)
		{
			var (value, success) = expr.TryEvaluateExpression(context);
			if (!success)
				throw new LinqToDBException($"Cannot evaluate expression: {expr}");

			return value;
		}

		public static bool? EvaluateBoolExpression(this IQueryElement expr, EvaluationContext context, bool? defaultValue = null)
		{
			var evaluated = expr.EvaluateExpression(context);

			if (evaluated is bool boolValue)
				return boolValue;

			return defaultValue;
		}
	}
}
