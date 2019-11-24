using System;
using System.IO;
using System.Reflection;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Zip;

namespace MinecraftUpgrader.Web.Cef
{
	public static class LibHelper
	{
#if PLATFORM_WIN
		private const string LibCefName = "libcef.dll";
		private const string CefZipName = "cef_win_x64.zip";
#elif PLATFORM_MAC
		private const string LibCefName = "Chromium Embedded Framework.framework\\Chromium Embedded Framework";
		private const string CefZipName = "cef_osx_x64.zip";
#endif

		public static void ExtractLibrariesIfNeeded()
		{
			var appAssembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException( $"{nameof(ExtractLibrariesIfNeeded)} can only be called within a running program" );
			var libAssembly = Assembly.GetExecutingAssembly();
			var appLocation = Path.GetDirectoryName( appAssembly.Location );
			var cefLibFile  = Path.Combine( appLocation, LibCefName );

			if ( !File.Exists( cefLibFile ) )
			{
				// Default Namespace + resource name
				var cefZipResourceName = $"{nameof(MinecraftUpgrader)}.{CefZipName}";

				using var cefStream = libAssembly.GetManifestResourceStream( cefZipResourceName );
				using var cefZip    = new ZipFile( cefStream );

				if ( !cefZip.TryExtract( appLocation, new ZipExtractOptions { OverwriteExisting = true } ) )
					throw new ApplicationException( "Could not extract Chromium Embedded Framework." );
			}
		}
	}
}