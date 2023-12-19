using System;
using System.Buffers;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftUpgrader.Utility;

public record DownloadProgressEventArgs(string Uri, long BytesReceived, long TotalBytesToReceive, int ProgressPercentage);

public class FileDownloader(HttpClient httpClient)
{
	public const int DefaultBufferSize = 4096;

	public event EventHandler<DownloadProgressEventArgs> ProgressChanged;

	public int BufferSize { get; init; } = DefaultBufferSize;

	public async Task DownloadFileAsync(string uri, string destinationPath, CancellationToken cancellationToken = default)
	{
		await using var fileStream = File.Open(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

		await DownloadToStreamAsync(uri, fileStream, cancellationToken);
		await fileStream.FlushAsync(cancellationToken);
	}

	public async Task<Stream> DownloadToMemoryAsync(string uri, CancellationToken cancellationToken = default)
	{
		var destinationStream = new MemoryStream();

		await DownloadToStreamAsync(uri, destinationStream, cancellationToken);

		return destinationStream;
	}

	private async Task DownloadToStreamAsync(string uri, Stream destinationStream, CancellationToken cancellationToken = default)
	{
		using var response = await httpClient.GetAsync(uri, cancellationToken);
		await using var downloadStream = await response.Content.ReadAsStreamAsync(cancellationToken);

		var totalBytes = response.Content.Headers.ContentLength ?? -1;
		var bytesRemaining = totalBytes;
		var totalBytesRead = 0;

		ProgressChanged?.Invoke(this, new DownloadProgressEventArgs(uri, 0L, bytesRemaining, 0));

		using var bufferOwner = MemoryPool<byte>.Shared.Rent(BufferSize);
		var buffer = bufferOwner.Memory;

		while (bytesRemaining > 0)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var bytesRead = await downloadStream.ReadAsync(buffer, cancellationToken);

			if (bytesRead == 0)
				break;

			bytesRemaining -= bytesRead;
			totalBytesRead += bytesRead;

			await destinationStream.WriteAsync(buffer[..bytesRead], cancellationToken);

			ProgressChanged?.Invoke(this, new DownloadProgressEventArgs(uri, totalBytesRead, totalBytes, (int) ( 100 * (double) totalBytesRead / totalBytes )));
		}
	}
}