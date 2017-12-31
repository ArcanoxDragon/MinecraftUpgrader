using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Async;

namespace MinecraftUpgrader.Zip
{
	public class ZipExtractOptions
	{
		public ZipExtractOptions( string directoryName = null, bool overwriteExisting = false, CancellationToken? cancellationToken = null, ProgressReporter progressReporter = null )
		{
			this.DirectoryName     = directoryName;
			this.OverwriteExisting = overwriteExisting;
			this.CancellationToken = cancellationToken;
			this.ProgressReporter  = progressReporter;
		}

		/// <summary>
		/// The name of the directory in the ZIP file to extract (optional)
		/// </summary>
		public string DirectoryName { get; set; }

		public bool RecreateFolderStructure { get; set; } = true;
		public bool OverwriteExisting { get; set; }
		public CancellationToken? CancellationToken { get; set; }
		public ProgressReporter ProgressReporter { get; set; }
	}

	public static class ZipExtensions
	{
		private const int BufferSize = 4 * 1024 * 1024; // 4 MB

		public static async Task ExtractAsync( this ZipFile zip, string destination, ZipExtractOptions options )
		{
			var directoryName     = options.DirectoryName;
			var recreateFolder    = options.RecreateFolderStructure;
			var overwriteExisting = options.OverwriteExisting;
			var token             = options.CancellationToken;
			var progress          = options.ProgressReporter;

			var t       = token ?? CancellationToken.None;
			var iEntry  = 0;
			var entries = directoryName == null
							  ? zip.OfType<ZipEntry>().ToList()
							  : ( from ZipEntry e in zip
								  where e.IsFile
								  where e.Name.StartsWith( $"{directoryName}/" )
								  select e ).ToList();

			if ( directoryName != null && entries.Count == 0 )
				throw new DirectoryNotFoundException( $"The directory \"{directoryName}\" was not found in the archive" );

			foreach ( var entry in entries )
			{
				if ( t.IsCancellationRequested )
					return;

				var dirName = Path.GetDirectoryName( entry.Name ) ?? "";

				dirName = dirName.Replace( '\\', '/' );

				if ( directoryName != null )
				{
					if ( recreateFolder )
					{
						if ( directoryName.Contains( "/" ) )
							dirName = dirName.Substring( directoryName.LastIndexOf( "/", StringComparison.Ordinal ) ); // get relative path
					}
					else
					{
						dirName = dirName.Substring( directoryName.Length );

						if ( dirName.StartsWith( "/" ) )
							dirName = dirName.Substring( 1 );
					}
				}

				var fileName        = Path.GetFileName( entry.Name );
				var destinationPath = Path.Combine( destination,     dirName );
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