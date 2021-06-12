﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;
using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class UpdateWithOutputTests : TestBase
	{
		private const string FeatureUpdateOutputWithOldSingle      = TestProvName.AllSqlServer2005Plus + "," + TestProvName.AllFirebird;
		private const string FeatureUpdateOutputWithOldMultiple    = TestProvName.AllSqlServer2005Plus;
		private const string FeatureUpdateOutputWithoutOldSingle   = TestProvName.AllSqlServer2005Plus + "," + TestProvName.AllFirebird + "," + TestProvName.AllPostgreSQL + "," + TestProvName.AllSQLiteClassic;
		private const string FeatureUpdateOutputWithoutOldMultiple = TestProvName.AllSqlServer2005Plus + "," + TestProvName.AllPostgreSQL + "," + TestProvName.AllSQLiteClassic;
		private const string FeatureUpdateOutputInto               = TestProvName.AllSqlServer2005Plus;

		private class UpdateOutputComparer<T> : IEqualityComparer<UpdateOutput<T>>
			where T : notnull
		{
			private readonly IEqualityComparer<T> tComparer;

			public UpdateOutputComparer()
			{
				tComparer = ComparerBuilder.GetEqualityComparer<T>();
			}

			public bool Equals(UpdateOutput<T>? x, UpdateOutput<T>? y)
				=> tComparer.Equals(x!.Deleted, y!.Deleted) && tComparer.Equals(x!.Inserted, y!.Inserted);

			public int GetHashCode(UpdateOutput<T> obj)
				=> tComparer.GetHashCode(obj.Deleted) * -1521134295 + tComparer.GetHashCode(obj.Inserted);
		}

		[Table]
		record TableWithData
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table(Schema = "TestSchema")]
		record TableWithDataAndSchema
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table]
		record DestinationTable
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		static TableWithData[] GetSourceData()
		{
			return Enumerable.Range(1, 9).Select(i =>
					new TableWithData { Id = i, Value = -i, ValueStr = "Str" + i.ToString() })
				.ToArray();
		}

		#region Update against ITable<T> target

		[Test]
		public void UpdateITableWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateITableWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, });

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						});

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						});

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateITableWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateITableWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateITableWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateITableWithProjectionOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						target,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		#endregion

		#region Update against Expression target

		[Test]
		public void UpdateExpressionWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateExpressionWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new UpdateOutput<DestinationTable>()
					{
						Inserted = new DestinationTable { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Deleted  = new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", },
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, });

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutput(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						})
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr = s.ValueStr,
						DeletedValue = s.Value + 1,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr = source.s.ValueStr,
							DeletedValue = deleted.Value,
							InsertedValue = inserted.Value,
						});

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			{
				var expected = sourceData
					.Select(s => new
					{
						SourceStr     = s.ValueStr,
						InsertedValue = s.Value,
					})
					.ToArray();

				var output = await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						(source, deleted, inserted) => new
						{
							SourceStr     = source.s.ValueStr,
							InsertedValue = inserted.Value,
						});

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateExpressionWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateExpressionWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<DestinationTable>(tableName: "Destination"))
			{
				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination);

				AreEqual(
					target.ToArray(),
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public void UpdateExpressionWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputInto(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateExpressionWithProjectionOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable(sourceData
				.Select(s => new DestinationTable { Id = s.Id, Value = s.Value + 1, ValueStr = (s.Value + 1).ToString() + "Dst", })))
			using (var destination = db.CreateLocalTable<TableWithData>(tableName: "Destination"))
			{
				var expected = sourceData
					.Select(s => new TableWithData
					{
						Id = s.Id,
						Value = s.Value + 1,
						ValueStr = s.ValueStr,
					})
					.ToArray();

				await source
					.SelectMany(s => target.Where(t => t.Id == s.Id), (s, t) => new { s, t, })
					.UpdateWithOutputIntoAsync(
						s => s.t,
						s => new DestinationTable { Id = s.s.Id, Value = s.s.Value, ValueStr = s.s.ValueStr, },
						destination,
						(source, deleted, inserted) => new TableWithData { Id = source.s.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		#endregion

		#region Update against Source

		[Test]
		public void UpdateSourceWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.UpdateWithOutput(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateSourceWithDefaultOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.UpdateWithOutput(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateSourceWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputAsync(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", });

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateSourceWithDefaultOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.UpdateWithOutputAsync(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", });

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputTestWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.UpdateWithOutput(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestAsyncWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.UpdateWithOutputAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						(deleted, inserted) => new { InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateSourceWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.UpdateWithOutputInto(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateSourceWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputIntoAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateSourceWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.UpdateWithOutputInto(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateSourceWithProjectionOutputTestIntoAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.UpdateWithOutputIntoAsync(
						s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		#endregion

		#region Update against Source

		[Test]
		public void UpdateIUpdatableWithDefaultOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput()
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateIUpdatableWithDefaultOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput()
					.ToArray();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithDefaultOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithDefaultOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new UpdateOutput<TableWithData>()
					{
						Deleted  = new TableWithData { Id = s.Id, Value = s.Value,     ValueStr = s.ValueStr, },
						Inserted = new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", },
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync();

				AreEqual(
					expected,
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTest([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTestSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTestWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputTestWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutput(
						(deleted, inserted) => new { InsertedValue = inserted.Value, })
					.ToArray();

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsync([IncludeDataSources(true, FeatureUpdateOutputWithOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsyncSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						DeletedValue  = s.Value,
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { DeletedValue = deleted.Value, InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsyncWithoutOld([IncludeDataSources(true, FeatureUpdateOutputWithoutOldMultiple)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestAsyncWithoutOldSingleRecord([IncludeDataSources(true, FeatureUpdateOutputWithoutOldSingle)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var expected = sourceData
					.Where(s => s.Id == 3)
					.Select(s => new
					{
						InsertedValue = s.Value + 1,
					})
					.ToArray();

				var output = await source
					.Where(s => s.Id == 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputAsync(
						(deleted, inserted) => new { InsertedValue = inserted.Value, });

				AreEqual(
					expected,
					output);
			}
		}

		[Test]
		public void UpdateIUpdatableWithDefaultOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputInto(
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithDefaultOutputIntoTestAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<TableWithData>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new TableWithData { Id = s.Id, Value = s.Value + 1, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputIntoAsync(
						destination);

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<TableWithData>());
			}
		}

		[Test]
		public void UpdateIUpdatableWithProjectionOutputIntoTest([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputInto(
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		[Test]
		public async Task UpdateIUpdatableWithProjectionOutputTestIntoAsync([IncludeDataSources(true, FeatureUpdateOutputInto)] string context)
		{
			var sourceData         = GetSourceData();
			using (var db          = GetDataContext(context))
			using (var source      = db.CreateLocalTable(sourceData))
			using (var destination = db.CreateLocalTable<DestinationTable>("destination"))
			{
				var expected = sourceData
					.Where(s => s.Id > 3)
					.Select(s => new DestinationTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr + "Upd", })
					.ToArray();

				await source
					.Where(s => s.Id > 3)
					.AsUpdatable()
					.Set(s => s.Value, s => s.Value + 1)
					.Set(s => s.ValueStr, s => s.ValueStr + "Upd")
					.UpdateWithOutputIntoAsync(
						destination,
						(deleted, inserted) => new DestinationTable { Id = inserted.Id, Value = deleted.Value, ValueStr = inserted.ValueStr, });

				AreEqual(
					expected,
					destination.ToArray(),
					ComparerBuilder.GetEqualityComparer<DestinationTable>());
			}
		}

		#endregion

		#region Issues
		[Test]
		public void Issue3044UpdateOutputWithTake([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var output = source
					.Where(i => i.Id >= 7)
					.OrderBy(i => i.Id)
					.Take(1)
					.UpdateWithOutput(x => new TableWithData { Id = 20, });

				AreEqual(
					new[]
					{
						new UpdateOutput<TableWithData>
						{
							Deleted = sourceData[6],
							Inserted = sourceData[6] with { Id = 7 },
						}
					},
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void Issue3044UpdateOutputWithTakeSubquery([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var output = source
					.Where(i => i.Id >= 7)
					.OrderBy(i => i.Id)
					.Take(1)
					.UpdateWithOutput(x => new TableWithData { Id = 20, });

				AreEqual(
					new[]
					{
						new UpdateOutput<TableWithData>
						{
							Deleted = sourceData[6],
							Inserted = sourceData[6] with { Id = 7 },
						}
					},
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}

		[Test]
		public void Issue3044UpdateOutputWithTakeCte([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var sourceData    = GetSourceData();
			using (var db     = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			{
				var output = source
					.Where(i => i.Id >= 7)
					.OrderBy(i => i.Id)
					.Take(1)
					.AsCte()
					.UpdateWithOutput(x => new TableWithData { Id = 20, });

				AreEqual(
					new[]
					{
						new UpdateOutput<TableWithData>
						{
							Deleted = sourceData[6],
							Inserted = sourceData[6] with { Id = 7 },
						}
					},
					output,
					new UpdateOutputComparer<TableWithData>());
			}
		}
		#endregion
	}
}
