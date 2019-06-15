using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace WordCounter.Controllers
{
	[ApiController]
	public class WordCounterController : ControllerBase
	{
		private static readonly List<WordCountSubmission> data = new List<WordCountSubmission>();

		// GET statistics
		[HttpGet]
		[Route("statistics")]
		public ActionResult<Dictionary<string, int>> GetStatistics()
		{
			var oneMinuteAgo = DateTime.UtcNow.AddSeconds(-60);
			return
				WordCounterController.data
				.Where(x => x.SubmitDate > oneMinuteAgo)
				.SelectMany(submission => submission.WordCounts)
				.ToLookup(wordCount => wordCount.Key, wordCount => wordCount.Value)
				.ToDictionary(groupOfWordCounts => groupOfWordCounts.Key, groupOfWordCounts => groupOfWordCounts.Sum());
		}

		// POST submit
		[HttpPost]
		[Route("submit")]
		public ActionResult SubmitWords()
		{
			string text = new StreamReader(Request.Body).ReadToEnd();
			WordCountSubmission submission = new WordCountSubmission(DateTime.UtcNow, text);
			WordCounterController.data.Add(submission);
			return Ok();
		}
	}

	public class WordCountSubmission
	{
		public WordCountSubmission(DateTime submitDate, string text)
		{
			this.SubmitDate = submitDate;

			Dictionary<string, int> wordCounts = new Dictionary<string, int>();
			string[] words = text.Split(new[] { ' ' });

			foreach (var word in words)
			{
				int count = 0;
				wordCounts.TryGetValue(word, out count);
				wordCounts[word] = ++count;
			}

			this.WordCounts = wordCounts;
		}

		public DateTime SubmitDate { get; set; }
		public Dictionary<string, int> WordCounts { get; set; }
	}
}
