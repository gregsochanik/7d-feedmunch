﻿using System;
using SevenDigital.Api.FeedReader.Dates;

namespace SevenDigital.Api.FeedReader
{
	public abstract class Feed
	{
		public const DayOfWeek FULL_FEED_DAY_OF_WEEK = DayOfWeek.Monday;

		public abstract string GetLatest();
		public abstract FeedCatalogueType FeedCatalogueType();
		public abstract FeedType FeedType();

		protected string GetPreviousFullFeedDate()
		{
			return DateTime.Now.PreviousDayOfWeek(FULL_FEED_DAY_OF_WEEK).ToString("yyyyMMdd");
		}

		protected string GetPreviousIncrementalFeedDate()
		{
			return DateTime.Now.PreviousDayOfWeek().ToString("yyyyMMdd");
		}
	}
}