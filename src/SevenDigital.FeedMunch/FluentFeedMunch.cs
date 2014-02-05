﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using DeCsv;
using SevenDigital.Api.FeedReader;
using SevenDigital.Api.FeedReader.Feeds;
using CsvSerializer = ServiceStack.Text.CsvSerializer;

namespace SevenDigital.FeedMunch
{
	public class FluentFeedMunch
	{
		private readonly IFeedDownload _feedDownload;
		private readonly GenericFeedReader _genericFeedReader;
		private readonly IFileHelper _fileHelper;
		private readonly ILogAdapter _logLog;

		public FeedMunchConfig Config { get; private set; }

		public FluentFeedMunch(IFeedDownload feedDownload, GenericFeedReader genericFeedReader, IFileHelper fileHelper, ILogAdapter logLog)
		{
			_feedDownload = feedDownload;
			_genericFeedReader = genericFeedReader;
			_fileHelper = fileHelper;
			_logLog = logLog;
		}

		public FluentFeedMunch WithConfig(FeedMunchConfig config)
		{
			Config = config;
			return this;
		}

		public void Invoke<T>()
		{
			var feed = new Feed(Config.Feed, Config.Catalog) { Country = Config.Country };
			_logLog.Info(feed.ToString());

			if (!string.IsNullOrEmpty(Config.Existing))
			{
				UseExistingFeed<T>(feed);
			}
			else
			{
				DownloadFromFeedsApi<T>(feed);
			}
		}

		private void DownloadFromFeedsApi<T>(Feed feed)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var downloadFromFeedsApiTask = _feedDownload.SaveLocally(feed);

			_logLog.Info(string.Format("Downloading to {0}", _feedDownload.CurrentFileName));

			downloadFromFeedsApiTask.ContinueWith(task =>
			{
				if (task.Exception != null)
				{
					_logLog.Info("An error occured downloading the feed");
					_logLog.Info(task.Exception.InnerExceptions.First().Message);
					return;
				}

				_logLog.Info("Finished downloading");
				stopwatch.Stop();
				_logLog.Info(String.Format("Took {0} milliseconds to download", stopwatch.ElapsedMilliseconds));

				FilterFeedAndWrite<T>(feed);
			}, TaskContinuationOptions.LongRunning);
		}

		private void UseExistingFeed<T>(Feed feed)
		{
			feed.ExistingPath = Config.Existing;
			FilterFeedAndWrite<T>(feed);
		}

		private void FilterFeedAndWrite<T>(Feed feed)
		{
			var filteredFeed = ReadAndFilterAllRows<T>(feed);
			var outputFeedPath = GenerateOutputFeedLocation(Config.Output);

			_logLog.Info(string.Format("Writing filtered feed out to {0}", outputFeedPath));

			var timeFilteredFeedWrite = TimerHelper.TimeMe(() => TryOutputFilteredFeed(outputFeedPath, filteredFeed));

			_logLog.Info(string.Format("Took {0} milliseconds to output filtered feed", timeFilteredFeedWrite.ElapsedMilliseconds));
		}

		private void TryOutputFilteredFeed<T>(string outputFeedPath, IEnumerable<T> filteredFeed)
		{
			try
			{
				using (var compressedFileStream = File.Create(outputFeedPath))
				{
					using (var compressionStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
					{
						CsvSerializer.SerializeToStream(filteredFeed, compressionStream);
					}
				}
				TryChangeExtension(outputFeedPath, ".tmp", ".gz");
			}
			catch (CsvDeserializationException ex)
			{
				_logLog.Info(ex.ToString());
				throw;
			}
		}

		private string GenerateOutputFeedLocation(string output)
		{
			_fileHelper.GetOrCreateFeedsFolder();
			var filename = Path.GetFileNameWithoutExtension(output);
			var directoryPath = Path.GetDirectoryName(output);
			var outputDirectory = _fileHelper.GetOrCreateOutputFolder(directoryPath);
			return Path.Combine(outputDirectory, filename + ".tmp");
		}

		private IEnumerable<T> ReadAndFilterAllRows<T>(Feed feed)
		{
			_logLog.Info("Reading data into list");
			var readIntoList = _genericFeedReader.ReadIntoList<T>(feed);
			var rows = Config.Limit > 0 ? readIntoList.Take(Config.Limit) : readIntoList;
			var filter = new Filter(Config.Filter);
			return rows.Where(row => filter.ApplyToRow(row));
		}

		private static void TryChangeExtension(string path, string from, string to)
		{
			var completedFilePath = path.Replace(from, to);
			if (File.Exists(completedFilePath))
			{
				File.Delete(completedFilePath);
			}
			File.Move(path, completedFilePath);
		}
	}
}