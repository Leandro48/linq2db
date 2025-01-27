﻿namespace LinqToDB.DataProvider.Oracle
{
	using SqlQuery;
	using System.Text;

	partial class Oracle11SqlBuilder
	{
		// Oracle doesn't support TABLE_ALIAS(COLUMN_ALIAS, ...) syntax
		protected override bool SupportsColumnAliasesInSource => false;

		// NULL value in sort leads to "cannot insert NULL into (TARGET.NON_NULL_COLUMN)" error in insert command
		// TODO: find a way to workaround it
		protected override bool isEmptyValuesSourceSupported => true;

		// VALUES(...) syntax not supported by Oracle
		protected override bool IsValuesSyntaxSupported => false;

		// bad thing that user can change this table, but broken merge will be minor issue in this case
		protected override string FakeTable => "dual";

		// dual table owner
		protected override string FakeTableSchema => "sys";

		protected override void BuildMergeInto(SqlMergeStatement merge)
		{
			StringBuilder.Append("MERGE ");

			if (merge.Hint != null)
			{
				StringBuilder
					.Append("/*+ ")
					.Append(merge.Hint)
					.Append(" */ ");
			}

			StringBuilder.Append("INTO ");
			BuildTableName(merge.Target, true, true);
			StringBuilder.AppendLine();
		}

		protected override void BuildMergeOperationInsert(SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("WHEN NOT MATCHED THEN")
				.Append("INSERT");

			var insertClause = new SqlInsertClause();
			insertClause.Items.AddRange(operation.Items);

			BuildInsertClause(new SqlInsertOrUpdateStatement(null), insertClause, null, false, false);

			if (operation.Where != null)
			{
				StringBuilder.Append(" WHERE ");
				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition: true);
			}
		}

		protected override void BuildMergeOperationUpdate(SqlMergeOperationClause operation)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("WHEN MATCHED THEN")
				.AppendLine("UPDATE");

			var update = new SqlUpdateClause();
			update.Items.AddRange(operation.Items);
			BuildUpdateSet(null, update);

			if (operation.Where != null)
			{
				StringBuilder
					.AppendLine("WHERE")
					.Append('\t');

				BuildSearchCondition(Precedence.Unknown, operation.Where, wrapCondition: true);
			}
		}

		protected override void BuildMergeOperationUpdateWithDelete(SqlMergeOperationClause operation)
		{
			BuildMergeOperationUpdate(operation);

			StringBuilder
				.AppendLine()
				.AppendLine("DELETE WHERE")
				.Append('\t');

			BuildSearchCondition(Precedence.Unknown, operation.WhereDelete!, wrapCondition: true);
		}
	}
}
