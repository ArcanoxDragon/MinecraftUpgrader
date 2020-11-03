namespace MinecraftLauncher.Async
{
	public class ProgressReporter
	{
		public delegate void ReportProgressEvent( double? progress, string status );

		public event ReportProgressEvent OnProgress;

		public double Progress { get; set; }
		public string Status { get; set; }

		public void ReportProgress( double progress, string status )
		{
			this.Progress = progress;
			this.Status = status;
			this.OnProgress?.Invoke( progress, status );
		}

		public void ReportProgress( double progress )
		{
			this.Progress = progress;
			this.OnProgress?.Invoke( progress, null );
		}

		public void ReportProgress( string status )
		{
			this.Status = status;
			this.OnProgress?.Invoke( null, status );
		}
	}
}