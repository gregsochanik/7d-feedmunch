﻿using System.Collections.Generic;
using NUnit.Framework;
using SevenDigital.FeedMunch.Feeds.Schema;

namespace SevenDigital.FeedMunch.Unit.Tests
{
	[TestFixture]
	public class ArtistFilteringTest
	{
		private IEnumerable<Artist> _artists;

		[SetUp]
		public void SetUp()
		{
			_artists = Artists();
		}

		[Test]
		public void Can_find_row_with_single_value_if_exists()
		{
			var filter = new Filter("name=mandela");

			var filteredArtists = filter.FiltrateToList(_artists);

			Assert.That(filteredArtists.Count, Is.EqualTo(1));
			Assert.That(filteredArtists[0].name, Is.EqualTo("mandela"));
		}

		[Test]
		public void Can_find_row_with_range_of_values()
		{
			var filter = new Filter("tags=india,south-africa");

			var filteredArtists = filter.FiltrateToList(_artists);

			Assert.That(filteredArtists.Count, Is.EqualTo(2));
			Assert.That(filteredArtists[0].name, Is.EqualTo("gandhi"));
			Assert.That(filteredArtists[1].name, Is.EqualTo("mandela"));
		}
		
		[Test]
		public void Can_find_row_with_negative_single_value()
		{
			var filter = new Filter("tags!=third-reich");

			var filteredArtists = filter.FiltrateToList(_artists);

			Assert.That(filteredArtists.Count, Is.EqualTo(2));
			Assert.That(filteredArtists[0].name, Is.EqualTo("gandhi"));
			Assert.That(filteredArtists[1].name, Is.EqualTo("mandela"));
		}

		[Test]
		public void Can_find_row_with_negative_range_of_values()
		{
			var filter = new Filter("tags!=india,south-africa");

			var filteredArtists = filter.FiltrateToList(_artists);

			Assert.That(filteredArtists.Count, Is.EqualTo(1));
			Assert.That(filteredArtists[0].name, Is.EqualTo("hitler"));
		}

		[Test]
		public void Can_create_a_blank_filter()
		{
			var filter = new Filter("");

			var filteredArtists = filter.FiltrateToList(_artists);

			Assert.That(filteredArtists.Count, Is.EqualTo(3));
		}
		
		private static IEnumerable<Artist> Artists()
		{
			var gandhi = new Artist
			{
				name = "gandhi",
				popularity = "1",
				tags = "india"
			};

			var mandela = new Artist
			{
				name = "mandela",
				popularity = "1",
				tags = "south-africa"
			};

			var hitler = new Artist
			{
				name = "hitler",
				popularity = "0.1",
				tags = "third-reich"
			};

			var artists = new List<Artist>
			{
				gandhi,
				mandela,
				hitler
			};
			return artists;
		}
	}
}