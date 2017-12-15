using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Async;

namespace MinecraftUpgrader.Zip
{
	public static class ZipExtensions
	{
		private const int BufferSize = 4 * 1024 * 1024; // 4 MB

		public static async Task ExtractDirectory( this ZipFile zip, string directoryName, string destination, bool overwriteExisting = false, CancellationToken? token = null, ProgressReporter progress = null )
		{
			var t = token ?? CancellationToken.None;
			var iEntry = 0;
			var entries = ( from ZipEntry e in zip
							where e.IsFile
							where e.Name.StartsWith( $"{directoryName}/" )
							select e ).ToList();

			if ( entries.Count == 0 )
				throw new DirectoryNotFoundException( $"The directory \"{directoryName}\" was not found in the archive" );

			foreach ( var entry in entries )
			{
				if ( t.IsCancellationRequested )
					return;

				var dirName = Path.GetDirectoryName( entry.Name ) ?? "";
				if ( directoryName.Contains( "/" ) )
					dirName = dirName.Substring( directoryName.LastIndexOf( "/", StringComparison.Ordinal ) ); // get relative path
				var fileName = Path.GetFileName( entry.Name );
				var destinationPath = Path.Combine( destination, dirName );
				var destinationName = Path.Combine( destinationPath, fileName );

				Directory.CreateDirectory( destinationPath );

				if ( overwriteExisting || !File.Exists( destinationName ) )
				{
					using ( var fs = File.Open( destinationName,
												FileMode.Create,
												FileAccess.Write,
												FileShare.None ) )
					using ( var es = zip.GetInputStream( entry ) )
					{
						await es.CopyToAsync( fs, BufferSize, t );
					}
				}

				progress?.ReportProgress( ++iEntry / (double) entries.Count );
			}
		}
	}
}