﻿using System;
using System.Collections.Generic;
using System.Linq;
using SmartStore.Core;
using SmartStore.Core.Data;
using SmartStore.Core.Domain.Logging;

namespace SmartStore.Services.Logging
{
	public partial class DbLogService : ILogService
	{
		private const int _deleteNumberOfEntries = 1000;

		private readonly IRepository<Log> _logRepository;
		private readonly IDbContext _dbContext;

		public DbLogService(IRepository<Log> logRepository)
		{
			_logRepository = logRepository;
			_dbContext = logRepository.Context;
		}

		public virtual void DeleteLog(Log log)
		{
			Guard.NotNull(log, nameof(log));

			_logRepository.Delete(log);
		}

		public virtual void ClearLog()
		{
			try
			{
				_dbContext.ExecuteSqlCommand("TRUNCATE TABLE [Log]");
			}
			catch
			{
				try
				{
					for (int i = 0; i < 100000; ++i)
					{
						if (_dbContext.ExecuteSqlCommand("Delete Top ({0}) From [Log]", false, null, _deleteNumberOfEntries) < _deleteNumberOfEntries)
							break;
					}
				}
				catch { }

				try
				{
					_dbContext.ExecuteSqlCommand("DBCC CHECKIDENT('Log', RESEED, 0)");
				}
				catch
				{
					try
					{
						_dbContext.ExecuteSqlCommand("Alter Table [Log] Alter Column [Id] Identity(1,1)");
					}
					catch { }
				}
			}

			_dbContext.ShrinkDatabase();
		}

		public virtual void ClearLog(DateTime toUtc, LogLevel logLevel)
		{
			try
			{
				string sqlDelete = "Delete Top ({0}) From [Log] Where LogLevelId < {1} And CreatedOnUtc <= {2}";

				for (int i = 0; i < 100000; ++i)
				{
					if (_dbContext.ExecuteSqlCommand(sqlDelete, false, null, _deleteNumberOfEntries, (int)logLevel, toUtc) < _deleteNumberOfEntries)
						break;
				}

				_dbContext.ShrinkDatabase();
			}
			catch { }
		}

		public virtual IPagedList<Log> GetAllLogs(DateTime? fromUtc, DateTime? toUtc, string message, LogLevel? logLevel, int pageIndex, int pageSize, int minFrequency)
		{
			var query = _logRepository.Table;

			if (fromUtc.HasValue)
				query = query.Where(l => fromUtc.Value <= l.CreatedOnUtc || fromUtc.Value <= l.UpdatedOnUtc);
			if (toUtc.HasValue)
				query = query.Where(l => toUtc.Value >= l.CreatedOnUtc || toUtc.Value >= l.UpdatedOnUtc);
			if (logLevel.HasValue)
			{
				int logLevelId = (int)logLevel.Value;
				query = query.Where(l => logLevelId == l.LogLevelId);
			}
			if (!String.IsNullOrEmpty(message))
				query = query.Where(l => l.ShortMessage.Contains(message) || l.FullMessage.Contains(message));

			query = query.OrderByDescending(l => l.UpdatedOnUtc).ThenByDescending(l => l.CreatedOnUtc);

			if (minFrequency > 0)
				query = query.Where(l => l.Frequency >= minFrequency);

			//query = _logRepository.Expand(query, x => x.Customer);

			var log = new PagedList<Log>(query, pageIndex, pageSize);
			return log;
		}

		public virtual Log GetLogById(int logId)
		{
			if (logId == 0)
				return null;

			var log = _logRepository.GetById(logId);
			return log;
		}

		public virtual IList<Log> GetLogByIds(int[] logIds)
		{
			if (logIds == null || logIds.Length == 0)
				return new List<Log>();

			var query = from l in _logRepository.Table
						where logIds.Contains(l.Id)
						select l;
			var logItems = query.ToList();
			//sort by passed identifiers
			var sortedLogItems = new List<Log>();
			foreach (int id in logIds)
			{
				var log = logItems.Find(x => x.Id == id);
				if (log != null)
					sortedLogItems.Add(log);
			}
			return sortedLogItems;
		}
	}
}
