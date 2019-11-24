using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using MinecraftUpgrader.Extensions;

namespace MinecraftUpgrader.Web
{
	/// <summary>
	/// Custom web client used to bypass CloudFlare challenges/protection on sites such as CurseForge
	///
	/// Uses an off-screen Chromium Embedded instance to retrieve pages and download files so that
	/// CloudFlare sees a real browser and doesn't flag us as a bot.
	/// </summary>
	public class CfWebClient : IDisposable
	{
		private const int DefaultTimeoutMs = 5000; // 5 seconds without data

		public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

		private readonly RequestContext     requestContext;
		private readonly ChromiumWebBrowser browser;

		public CfWebClient()
		{
			var browserSettings = new BrowserSettings { Javascript = CefState.Enabled };

			this.requestContext = new RequestContext();
			this.browser        = new ChromiumWebBrowser( "about:blank", browserSettings, this.requestContext );
		}

		public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromMilliseconds( DefaultTimeoutMs );

		public async Task DownloadFileTaskAsync( string uri, string filename, CancellationToken cancellationToken = default )
		{
			var directoryName = Path.GetDirectoryName( filename );

			if ( !Directory.Exists( directoryName ) )
				Directory.CreateDirectory( directoryName );

			using var fileStream = File.Open( filename, FileMode.Create, FileAccess.Write, FileShare.None );

			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Saving {uri} to {filename}..." );

			await this.StreamResourceTo( uri, fileStream, cancellationToken );
		}

		public async Task<string> DownloadStringTaskAsync( string uri, CancellationToken cancellationToken = default )
		{
			using var memoryStream = new MemoryStream();

			await this.StreamResourceTo( uri, memoryStream, cancellationToken );

			return Encoding.UTF8.GetString( memoryStream.ToArray() );
		}

		private async Task StreamResourceTo( string resourceUri, Stream destinationStream, CancellationToken cancellationToken )
		{
			if ( !this.browser.IsBrowserInitialized )
				await this.browser.WaitForInitializeAsync();

			var       resourceHandlerFactory = new InterceptResourceRequestHandlerFactory( resourceUri, destinationStream );
			using var timeoutCancelSource    = new CancellationTokenSource( this.DownloadTimeout );

			cancellationToken.Register( timeoutCancelSource.Cancel ); // Relay cancellation
			resourceHandlerFactory.DownloadProgressChanged += ( sender, args ) => {
				// ReSharper disable once AccessToDisposedClosure (will literally never ever ever access it once disposed)
				timeoutCancelSource.CancelAfter( this.DownloadTimeout ); // Reset the timeout
				this.DownloadProgressChanged?.Invoke( this, args );
			};

			this.browser.ResourceRequestHandlerFactory = resourceHandlerFactory;

			var pageLoadTask = this.browser.LoadPageAsync( resourceUri );
			var resourceTask = resourceHandlerFactory.WaitForResourceAsync();

			await Task.WhenAll( pageLoadTask, resourceTask );

			this.browser.ResourceRequestHandlerFactory = null;
		}

		public void Dispose()
		{
			this.browser?.Dispose();
			this.requestContext?.Dispose();
		}
	}
}