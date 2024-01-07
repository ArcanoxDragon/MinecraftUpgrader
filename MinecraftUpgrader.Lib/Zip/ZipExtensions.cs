using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using MinecraftUpgrader.Async;

namespace MinecraftUpgrader.Zip;

public class ZipExtractOptions(
	string directoryName = null,
	bool overwriteExisting = false,
	CancellationToken? cancellationToken = null,
	ProgressReporter progressReporter = null,
	string filenamePattern = null)
{
	/// <summary>
	/// The name of the directory in the ZIP file to extract (optional)
	/// </summary>
	public string DirectoryName { get; set; } = directoryName;

	public bool               RecreateFolderStructure { get; set; } = true;
	public bool               OverwriteExisting       { get; set; } = overwriteExisting;
	public CancellationToken? CancellationToken       { get; set; } = cancellationToken;
	public ProgressReporter   ProgressReporter        { get; set; } = progressReporter;
	public string             FilenamePattern         { get; set; } = filenamePattern;
}

public static class ZipExtensions
{
	private const int BufferSize = 4 * 1024 * 1024; // 4 MB

	public static async Task ExtractAsync(this ZipFile zip, string destination, ZipExtractOptions options)
	{
		var directoryName = options.DirectoryName;
		var recreateFolder = options.RecreateFolderStructure;
		var overwriteExisting = options.OverwriteExisting;
		var cancellationToken = options.CancellationToken ?? CancellationToken.None;
		var progress = options.ProgressReporter;
		var iEntry = 0;
		var entries = directoryName == null
			? zip.OfType<ZipEntry>().ToList()
			: ( from ZipEntry e in zip
				where e.IsFile
				where e.Name.StartsWith($"{directoryName}/")
				select e ).ToList();

		if (directoryName != null && entries.Count == 0)
			throw new DirectoryNotFoundException($"The directory \"{directoryName}\" was not found in the archive");

		if (!string.IsNullOrEmpty(options.FilenamePattern))
			entries = entries.Where(e => Regex.IsMatch(e.Name, options.FilenamePattern, RegexOptions.IgnoreCase)).ToList();

		foreach (var entry in entries)
		{
			if (cancellationToken.IsCancellationRequested)
				return;

			var dirName = Path.GetDirectoryName(entry.Name) ?? "";

			dirName = dirName.Replace('\\', '/');

			if (directoryName != null)
			{
				if (recreateFolder)
				{
					if (directoryName.Contains("/"))
						dirName = dirName.Substring(directoryName.LastIndexOf("/", StringComparison.Ordinal)) // get relative path
							.Trim('/', '\\');                                                                 // trim leading/trailing slashes that are left over
				}
				else
				{
					dirName = dirName.Substring(directoryName.Length)
						.TrimStart('/', '\\');
				}
			}

			var fileName = Path.GetFileName(entry.Name);
			var destinationPath = Path.Combine(destination, dirName);
			var destinationName = Path.Combine(destinationPath, fileName);

			Directory.CreateDirectory(destinationPath);

			// Entry name can be a folder name; in this case, fileName will be an empty string and we should skip it
			if (!string.IsNullOrEmpty(fileName) && ( overwriteExisting || !File.Exists(destinationName) ))
			{
				await using var fs = File.Open(destinationName, FileMode.Create, FileAccess.Write, FileShare.None);
				await using var es = zip.GetInputStream(entry);

				await es.CopyToAsync(fs, BufferSize, cancellationToken);
			}

			progress?.ReportProgress(++iEntry / (double) entries.Count);
		}
	}

	public static async Task<bool> TryExtractAsync(this ZipFile zip, string destination, ZipExtractOptions options)
	{
		try
		{
			await zip.ExtractAsync(destination, options);
			return true;
		}
		catch
		{
			return false;
		}
	}
}