using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace WordCounter.Controllers
{
	[ApiController]
	public class WordCounterController : ControllerBase
	{
		// In a real system, this would be a call to some external database or cache
		private static readonly ConcurrentDictionary<DateTime, WordCountSubmission> data = new ConcurrentDictionary<DateTime, WordCountSubmission>();

		/// <summary>
		/// Retrieves counts for all words submitted within the last 60 seconds.
		/// </summary>
		[HttpGet, Route("statistics")]
		public ActionResult<Dictionary<string, int>> GetStatistics()
		{
			DateTime oneMinuteAgo = this.GetCurrentTime().AddSeconds(-60);

			return
				WordCounterController.data
				.Where(entry => entry.Key > oneMinuteAgo)
				.SelectMany(entry => entry.Value.WordCounts)
				.ToLookup(wordCount => wordCount.Key, wordCount => wordCount.Value)
				.ToDictionary(groupOfWordCounts => groupOfWordCounts.Key, groupOfWordCounts => groupOfWordCounts.Sum());
		}

		/// <summary>
		/// Submits words for counting.
		/// </summary>
		[HttpPost, Route("submit")]
		public ActionResult SubmitWords()
		{
			DateTime submitDateTime = this.GetCurrentTime();
			string text = new StreamReader(Request.Body).ReadToEnd();

			WordCounterController.data.AddOrUpdate(
				key: submitDateTime,
				addValue: new WordCountSubmission(submitDateTime, text),
				updateValueFactory: (existingTime, existingSubmission) => { existingSubmission.AddWords(text); return existingSubmission; });

			return Ok();
		}

		/// <summary>
		/// Gets the current UTC time truncated to whole seconds.
		/// </summary>
		/// <returns>Current UTC time truncated to whole seconds.</returns>
		private DateTime GetCurrentTime()
		{
			return DateTime.UtcNow.AddTicks(-(DateTime.UtcNow.Ticks % TimeSpan.FromSeconds(1).Ticks));
		}

		private class WordCountSubmission
		{
			public WordCountSubmission(DateTime submitDateTime, string text)
			{
				if (text == null) throw new ArgumentNullException(nameof(text));
				this.WordCounts = new ConcurrentDictionary<string, int>();
				this.AddWords(text);
			}

			public ConcurrentDictionary<string, int> WordCounts { get; private set; }

			public void AddWords(string text)
			{
				if (text == null) throw new ArgumentNullException(nameof(text));

				// TODO: read this as a stream in case we're processing the entire library of congress in a single submission
				foreach (string word in text.Split(new[] { ' ' }))
				{
					this.AddWordCount(word, 1);
				}
			}

			public void AddWords(ConcurrentDictionary<string, int> otherWordCounts)
			{
				if (otherWordCounts == null) throw new ArgumentNullException(nameof(otherWordCounts));

				foreach (KeyValuePair<string, int> kvp in otherWordCounts)
				{
					this.AddWordCount(kvp.Key, kvp.Value);
				}
			}

			private void AddWordCount(string word, int count)
			{
				if (word == null) throw new ArgumentNullException(nameof(word));
				if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));

				this.WordCounts.AddOrUpdate(
					key: word,
					addValue: count,
					updateValueFactory: (existingWord, existingCount) => existingCount + count);
			}
		}
	}
}
