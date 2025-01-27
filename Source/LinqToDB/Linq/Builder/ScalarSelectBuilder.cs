﻿using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class ScalarSelectBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return
				buildInfo.Expression.NodeType == ExpressionType.Lambda &&
				((LambdaExpression)buildInfo.Expression).Parameters.Count == 0;
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return new ScalarSelectContext(builder)
			{
				Parent      = buildInfo.Parent,
				Expression  = buildInfo.Expression,
				SelectQuery = buildInfo.SelectQuery
			};
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		class ScalarSelectContext : IBuildContext
		{
			public ScalarSelectContext(ExpressionBuilder builder)
			{
				Builder = builder;

				builder.Contexts.Add(this);
			}

#if DEBUG
			public string _sqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
			public string Path => this.GetPath();
#endif

			public ExpressionBuilder Builder     { get; }
			public Expression?       Expression  { get; set; }
			public SelectQuery       SelectQuery { get; set; } = null!;
			public SqlStatement?     Statement   { get; set; }
			public IBuildContext?    Parent      { get; set; }

			public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				if (expression == null)
					expression = ((LambdaExpression)Expression!).Body.Unwrap();

				switch (expression.NodeType)
				{
					case ExpressionType.New:
					case ExpressionType.MemberInit:
						{
							var expr = Builder.BuildExpression(this, expression, enforceServerSide);

							if (SelectQuery.Select.Columns.Count == 0)
								SelectQuery.Select.Expr(new SqlValue(1));

							return expr;
						}

					default :
						{
							var expr = Builder.ConvertToSql(this, expression);
							var idx  = SelectQuery.Select.Add(expr);

							return Builder.BuildSql(expression.Type, idx, expr);
						}
				}

			}

			public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return requestFlag switch
				{
					RequestFor.Expression => IsExpressionResult.True,
					_                     => IsExpressionResult.False,
				};
			}

			public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}

			public int ConvertToParentIndex(int index, IBuildContext context)
			{
				return Parent?.ConvertToParentIndex(index, context) ?? index;
			}

			public void SetAlias(string? alias)
			{
			}

			public ISqlExpression? GetSubQuery(IBuildContext context)
			{
				return null;
			}

			public virtual SqlStatement GetResultStatement()
			{
				return Statement ??= new SqlSelectStatement(SelectQuery);
			}

			public void CompleteColumns()
			{
			}
		}
	}
}
