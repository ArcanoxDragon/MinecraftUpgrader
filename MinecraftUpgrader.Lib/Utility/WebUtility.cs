using System;
using System.Net.Http;
using Humanizer;

namespace MinecraftUpgrader.Utility;

public static class WebUtility
{
	private static readonly TimeSpan WebRequestTimeout = 5.Seconds();

	public static HttpClient CreateHttpClient() => new() {
		Timeout = WebRequestTimeout,
	};
}