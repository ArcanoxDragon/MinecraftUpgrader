using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace MinecraftUpgrader.Web.Cef
{
	public class InterceptResourceRequestHandler : CefRequestHandler, IDisposable
	{
		public event EventHandler<DownloadProgressChangedEventArgs> DownloadProgressChanged;

		private readonly List<string>               interceptUris;
		private readonly TaskCompletionSource<bool> completionSource;

		private long resourceSize;
		private long bytesReceived;

		public InterceptResourceRequestHandler( string interceptUri, Stream destinationStream )
		{
			this.interceptUris    = new List<string> { interceptUri.ToLower() };
			this.completionSource = new TaskCompletionSource<bool>();

			this.Filter = new InterceptResponseFilter( destinationStream );
			this.Filter.DataReceived += bytes => {
				this.bytesReceived += bytes;

				this.DownloadProgressChanged?.Invoke( this, new DownloadProgressChangedEventArgs {
					TotalBytesToReceive = this.resourceSize,
					BytesReceived       = this.bytesReceived,
				} );
			};
		}

		public InterceptResponseFilter Filter { get; }

		public Task WaitForResourceAsync() => this.completionSource.Task;

		protected override CefReturnValue OnBeforeResourceLoad( CefBrowser browser, CefFrame frame, CefRequest request, CefRequestCallback callback )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Beginning resource load for {request.Url}" );

			return base.OnBeforeResourceLoad( browser, frame, request, callback );
		}

		protected override bool OnResourceResponse( CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Got resource response for {request.Url}" );

			if ( response.GetHeaderMap().AllKeys.Contains( "Content-Length" ) )
			{
				var contentLengthHeader = response.GetHeader( "Content-Length" );

				if ( long.TryParse( contentLengthHeader, out var contentLength ) )
				{
					Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Resource {request.Url} is {contentLength} bytes" );

					this.resourceSize = contentLength;
				}
			}

			return base.OnResourceResponse( browser, frame, request, response );
		}

		protected override void OnResourceRedirect( CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response, ref string newUrl )
		{
			base.OnResourceRedirect( browser, frame, request, response, ref newUrl );

			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Resource {request.Url} was redirected to {newUrl}" );

			if ( this.interceptUris.Contains( request.Url.ToLower() ) )
				this.interceptUris.Add( newUrl.ToLower() );
		}

		protected override void OnResourceLoadComplete( CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response, CefUrlRequestStatus status, long receivedContentLength )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Resource load complete for {request.Url}" );

			this.completionSource.TrySetResult( true );

			base.OnResourceLoadComplete( browser, frame, request, response, status, receivedContentLength );
		}

		protected override CefResponseFilter GetResourceResponseFilter( CefBrowser browser, CefFrame frame, CefRequest request, CefResponse response )
		{
			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Got filter for {request.Url}" );

			return this.Filter;
		}

		public void Dispose()
		{
			this.Filter?.Dispose();
		}
	}
}