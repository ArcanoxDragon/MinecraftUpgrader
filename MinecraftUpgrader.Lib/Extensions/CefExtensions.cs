using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;

namespace MinecraftUpgrader.Extensions
{
	public static class CefExtensions
	{
		public static Task WaitForPageLoadAsync( this IWebBrowser browser ) => browser.LoadPageAsync( default );

		public static Task LoadPageAsync( this IWebBrowser browser, string address )
		{
			var taskSource = new TaskCompletionSource<bool>( TaskCreationOptions.RunContinuationsAsynchronously );

			void OnLoadingStateChanged( object sender, LoadingStateChangedEventArgs e )
			{
				if ( !e.IsLoading )
				{
					browser.LoadingStateChanged -= OnLoadingStateChanged;
					taskSource.TrySetResult( true );
				}
			}

			browser.LoadingStateChanged += OnLoadingStateChanged;

			if ( !string.IsNullOrEmpty( address ) )
			{
				browser.Load( address );
			}

			return taskSource.Task;
		}

		public static Task WaitForInitializeAsync( this ChromiumWebBrowser browser )
		{
			var taskCompletionSource = new TaskCompletionSource<bool>();

			browser.BrowserInitialized += ( sender, e ) => taskCompletionSource.TrySetResult( true );

			// If browser got initialized since the start of this method, resolve immediately
			if ( browser.IsBrowserInitialized )
				taskCompletionSource.TrySetResult( true );

			return taskCompletionSource.Task;
		}
	}
}