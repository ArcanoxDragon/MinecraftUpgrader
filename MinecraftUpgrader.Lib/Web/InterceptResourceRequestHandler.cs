using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CefSharp;
using CefSharp.Handler;

namespace MinecraftUpgrader.Web
{
	public class InterceptResourceRequestHandler : ResourceRequestHandler
	{
		public event Action                                         LoadComplete;
		public event Action<string, string>                         ResourceRedirect;
		public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

		private long resourceSize;
		private long bytesReceived;

		public InterceptResourceRequestHandler( Stream dataStream )
		{
			this.Filter = new InterceptResponseFilter( dataStream );
			this.Filter.DataReceived += bytes => {
				this.bytesReceived += bytes;

				this.DownloadProgressChanged?.Invoke( this, new DownloadProgressChangedEventArgs {
					TotalBytesToReceive = this.resourceSize,
					BytesReceived       = this.bytesReceived,
				} );
			};
		}

		public InterceptResponseFilter Filter { get; }

		protected override CefReturnValue OnBeforeResourceLoad( IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Beginning resource load for {request.Url}" );

			return base.OnBeforeResourceLoad( chromiumWebBrowser, browser, frame, request, callback );
		}

		protected override bool OnResourceResponse( IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Got resource response for {request.Url}" );

			if ( response.Headers.AllKeys.Contains( "Content-Length" ) )
			{
				var contentLengthHeader = response.Headers[ "Content-Length" ];

				if ( long.TryParse( contentLengthHeader, out var contentLength ) )
				{
					Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Resource {request.Url} is {contentLength} bytes" );

					this.resourceSize = contentLength;
				}
			}

			return base.OnResourceResponse( chromiumWebBrowser, browser, frame, request, response );
		}

		protected override void OnResourceRedirect( IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, ref string newUrl )
		{
			base.OnResourceRedirect( chromiumWebBrowser, browser, frame, request, response, ref newUrl );

			this.ResourceRedirect?.Invoke( request.Url, newUrl );
		}

		protected override void OnResourceLoadComplete( IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Resource load complete for {request.Url}" );

			this.LoadComplete?.Invoke();

			base.OnResourceLoadComplete( chromiumWebBrowser, browser, frame, request, response, status, receivedContentLength );
		}

		protected override IResponseFilter GetResourceResponseFilter( IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, IResponse response )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Got filter for {request.Url}" );

			return this.Filter;
		}
	}
}