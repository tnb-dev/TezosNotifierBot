using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NornPool.Model;

namespace TezosNotifyBot
{
	public class TwitterClient
	{
		readonly string consumerKey, consumerKeySecret, accessToken, accessTokenSecret, twitterApiBaseUrl = "https://api.twitter.com/1.1/";
		readonly HMACSHA1 sigHasher;
		readonly DateTime epochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private readonly ILogger<TwitterClient> _logger;

		/// <summary>
		/// Creates an object for sending tweets to Twitter using Single-user OAuth.
		/// 
		/// Get your access keys by creating an app at apps.twitter.com then visiting the
		/// "Keys and Access Tokens" section for your app. They can be found under the
		/// "Your Access Token" heading.
		/// </summary>
		public TwitterClient(IOptions<TwitterOptions> options, ILogger<TwitterClient> logger)
		{
			consumerKey = options.Value.ConsumerKey;
			consumerKeySecret = options.Value.ConsumerKeySecret;
			accessToken = options.Value.AccessToken;
			accessTokenSecret = options.Value.AccessTokenSecret;

			sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", consumerKeySecret, accessTokenSecret)));

			_logger = logger;
		}

		public delegate int TwitHandler(string text);
		public delegate void TwitterResponseHandler(int twitId, string response);
		public event TwitHandler OnTwit;
		public event TwitterResponseHandler OnTwitResponse;

		/// <summary>
		/// Sends a tweet with the supplied text and returns the response from the Twitter API.
		/// </summary>
		public async Task TweetAsync(string text)
		{
			var data = new Dictionary<string, string> {
				{ "status", text },
				{ "trim_user", "1" }
			};
			_logger.LogDebug("Tweet: " + text);
			int twitId = OnTwit.Invoke(text);
			if (consumerKey != null)
			{
				var result = await SendRequestAsync("statuses/update.json", data);
				_logger.LogDebug("Twitter response: " + result);
				OnTwitResponse(twitId, result);
			}
		}

		async public Task<string> DeleteTweetAsync(string id)
		{
			var data = new Dictionary<string, string> {
				{ "id", id },
				{ "trim_user", "1" }
			};
			_logger.LogDebug("Delete tweet: " + id);
			return await SendRequestAsync($"statuses/destroy/{id}.json", data);
		}

		async public Task<string> GetSettings()
		{
			return await SendRequestAsync("account/settings.json");
		}

		async Task<string> SendRequestAsync(string url, Dictionary<string, string> data)
		{
			var fullUrl = twitterApiBaseUrl + url;

			// Timestamps are in seconds since 1/1/1970.
			var timestamp = (int)((DateTime.UtcNow - epochUtc).TotalSeconds);

			// Add all the OAuth headers we'll need to use when constructing the hash.
			data.Add("oauth_consumer_key", consumerKey);
			data.Add("oauth_signature_method", "HMAC-SHA1");
			data.Add("oauth_timestamp", timestamp.ToString());
			data.Add("oauth_nonce", "a"); // Required, but Twitter doesn't appear to use it, so "a" will do.
			data.Add("oauth_token", accessToken);
			data.Add("oauth_version", "1.0");

			// Generate the OAuth signature and add it to our payload.
			data.Add("oauth_signature", GenerateSignature(fullUrl, "POST", data));

			// Build the OAuth HTTP Header from the data.
			string oAuthHeader = GenerateOAuthHeader(data);

			// Build the form data (exclude OAuth stuff that's already in the header).
			var formData = new FormUrlEncodedContent(data.Where(kvp => !kvp.Key.StartsWith("oauth_")));

			return await SendRequestAsync(fullUrl, oAuthHeader, formData);
		}
		async Task<string> SendRequestAsync(string url)
		{
			var fullUrl = twitterApiBaseUrl + url;

			// Timestamps are in seconds since 1/1/1970.
			var timestamp = (int)((DateTime.UtcNow - epochUtc).TotalSeconds);

			Dictionary<string, string> data = new Dictionary<string, string>();
			// Add all the OAuth headers we'll need to use when constructing the hash.
			data.Add("oauth_consumer_key", consumerKey);
			data.Add("oauth_signature_method", "HMAC-SHA1");
			data.Add("oauth_timestamp", timestamp.ToString());
			data.Add("oauth_nonce", "a"); // Required, but Twitter doesn't appear to use it, so "a" will do.
			data.Add("oauth_token", accessToken);
			data.Add("oauth_version", "1.0");

			// Generate the OAuth signature and add it to our payload.
			data.Add("oauth_signature", GenerateSignature(fullUrl, "GET", data));

			// Build the OAuth HTTP Header from the data.
			string oAuthHeader = GenerateOAuthHeader(data);

			return await SendRequestAsync(fullUrl, oAuthHeader);
		}
		/// <summary>
		/// Generate an OAuth signature from OAuth header values.
		/// </summary>
		string GenerateSignature(string url, string method, Dictionary<string, string> data)
		{
			var sigString = string.Join(
				"&",
				data
					.Union(data)
					.Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
					.OrderBy(s => s)
			);

			var fullSigData = string.Format(
				"{0}&{1}&{2}",
				method,
				Uri.EscapeDataString(url),
				Uri.EscapeDataString(sigString)
			);

			return Convert.ToBase64String(sigHasher.ComputeHash(new ASCIIEncoding().GetBytes(fullSigData)));
		}

		/// <summary>
		/// Generate the raw OAuth HTML header from the values (including signature).
		/// </summary>
		string GenerateOAuthHeader(Dictionary<string, string> data)
		{
			return "OAuth " + string.Join(
				", ",
				data
					.Where(kvp => kvp.Key.StartsWith("oauth_"))
					.Select(kvp => string.Format("{0}=\"{1}\"", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
					.OrderBy(s => s)
			);
		}

		/// <summary>
		/// Send HTTP Request and return the response.
		/// </summary>
		async Task<string> SendRequestAsync(string fullUrl, string oAuthHeader, FormUrlEncodedContent formData)
		{
			using (var http = new HttpClient())
			{
				http.DefaultRequestHeaders.Add("Authorization", oAuthHeader);

				var httpResp = await http.PostAsync(fullUrl, formData);
				var respBody = await httpResp.Content.ReadAsStringAsync();

				return respBody;
			}
		}

		async Task<string> SendRequestAsync(string fullUrl, string oAuthHeader)
		{
			using (var http = new HttpClient())
			{
				http.DefaultRequestHeaders.Add("Authorization", oAuthHeader);
				return await http.GetStringAsync(fullUrl);
			}
		}
	}
}
