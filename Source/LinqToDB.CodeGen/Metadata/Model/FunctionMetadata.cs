﻿using LinqToDB.CodeGen.Schema;

namespace LinqToDB.CodeGen.Metadata
{
	public class FunctionMetadata
	{
		public ObjectName? Name { get; set; }
		public int[]? ArgIndices { get; set; }
		public int? Precedence { get; set; }
		public string? Configuration { get; set; }
		public bool? ServerSideOnly { get; set; }
		public bool? PreferServerSide { get; set; }
		public bool? InlineParameters { get; set; }
		public bool? IsPredicate { get; set; }
		public bool? IsAggregate { get; set; }
		public bool? IsWindowFunction { get; set; }
		public bool? IsPure { get; set; }
		public bool? CanBeNull { get; set; }
		public Sql.IsNullableType? IsNullable { get; set; }
	}
}
