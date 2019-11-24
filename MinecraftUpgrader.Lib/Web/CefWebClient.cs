using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MinecraftUpgrader.Web.Cef;
using Xilium.CefGlue;

namespace MinecraftUpgrader.Web
{
	/// <summary>
	/// Custom web client used to bypass CloudFlare challenges/protection on sites such as CurseForge
	///
	/// Uses an off-screen Chromium Embedded instance to retrieve pages and download files so that
	/// CloudFlare sees a real browser and doesn't flag us as a bot.
	/// </summary>
	public class CefWebClient : IDisposable
	{
		private const int DefaultTimeoutMs = 5000; // 5 seconds without data

		public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

		private readonly Cef.Client client;
		private readonly CefBrowser browser;

		public CefWebClient()
		{
			LibHelper.ExtractLibrariesIfNeeded();
			CefRuntime.Load();

			var args        = new CefMainArgs( new string[ 0 ] );
			var app         = new Cef.App();
			var cefExitCode = CefRuntime.ExecuteProcess( args, app, IntPtr.Zero );

			if ( cefExitCode != -1 )
				throw new ApplicationException( $"Could not start Chromium Embedded: {cefExitCode}" );

			var cefSettings     = new CefSettings { MultiThreadedMessageLoop = true };
			var browserSettings = new CefBrowserSettings { JavaScript        = CefState.Enabled };
			var cefWindow       = CefWindowInfo.Create();

			cefWindow.SetAsWindowless( IntPtr.Zero, true );
			CefRuntime.Initialize( args, cefSettings, app, IntPtr.Zero );

			this.client  = new Cef.Client();
			this.browser = CefBrowserHost.CreateBrowserSync( cefWindow, this.client, browserSettings );
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
			using var resourceHandler     = new InterceptResourceRequestHandler( resourceUri, destinationStream );
			using var timeoutCancelSource = new CancellationTokenSource( this.DownloadTimeout );

			cancellationToken.Register( timeoutCancelSource.Cancel ); // Relay cancellation
			resourceHandler.DownloadProgressChanged += ( sender, args ) => {
				// ReSharper disable once AccessToDisposedClosure (will literally never ever ever access it once disposed)
				timeoutCancelSource.CancelAfter( this.DownloadTimeout ); // Reset the timeout
				this.DownloadProgressChanged?.Invoke( this, args );
			};

			this.client.RequestHandler = resourceHandler;

			this.browser.GetMainFrame().LoadUrl( resourceUri );

			await resourceHandler.WaitForResourceAsync();

			this.client.RequestHandler = null;
		}

		public void Dispose()
		{
			this.browser?.Dispose();

			CefRuntime.Shutdown();
		}
	}
}