namespace MinecraftUpgrader.Web
{
	public class DownloadProgressChangedEventArgs
	{
		public long BytesReceived       { get; set; }
		public long TotalBytesToReceive { get; set; }
	}
}