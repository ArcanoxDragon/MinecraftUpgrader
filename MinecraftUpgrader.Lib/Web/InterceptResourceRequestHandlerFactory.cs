using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.Handler;

namespace MinecraftUpgrader.Web
{
	public class InterceptResourceRequestHandlerFactory : IResourceRequestHandlerFactory
	{
		public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

		private readonly Stream                     destinationStream;
		private readonly List<string>               interceptUris;
		private readonly TaskCompletionSource<bool> completionSource;

		public InterceptResourceRequestHandlerFactory( string interceptUri, Stream destinationStream )
		{
			this.destinationStream = destinationStream;
			this.interceptUris     = new List<string> { interceptUri.ToLower() };
			this.completionSource  = new TaskCompletionSource<bool>();
		}

		public bool HasHandlers => true;

		public IResourceRequestHandler GetResourceRequestHandler( IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool isNavigation, bool isDownload, string requestInitiator, ref bool disableDefaultHandling )
		{
			var key = request.Url.ToLower();

			if ( this.interceptUris.Contains( key ) )
			{
				Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Creating intercept handler for {request.Url}" );

				var handler = new InterceptResourceRequestHandler( this.destinationStream );

				handler.LoadComplete += () => this.completionSource.TrySetResult( true );
				handler.ResourceRedirect += ( resourceUrl, newUrl ) => {
					Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Resource {resourceUrl} was redirected to {newUrl}" );

					if ( this.interceptUris.Contains( resourceUrl.ToLower() ) )
						this.interceptUris.Add( newUrl.ToLower() );
				};
				handler.DownloadProgressChanged += ( sender, args ) => this.DownloadProgressChanged?.Invoke( this, args );

				return handler;
			}

			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Using default handler for {request.Url}" );

			return new ResourceRequestHandler();
		}

		public Task WaitForResourceAsync() => this.completionSource.Task;
	}
}