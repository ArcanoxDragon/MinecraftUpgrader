namespace MinecraftUpgrader.Async;

public class ProgressReporter
{
	public delegate void ReportProgressEvent(double? progress, string status);

	public event ReportProgressEvent OnProgress;

	public double Progress { get; set; }
	public string Status   { get; set; }

	public void ReportProgress(double progress, string status)
	{
		Progress = progress;
		Status = status;
		OnProgress?.Invoke(progress, status);
	}

	public void ReportProgress(double progress)
	{
		Progress = progress;
		OnProgress?.Invoke(progress, null);
	}

	public void ReportProgress(string status)
	{
		Status = status;
		OnProgress?.Invoke(null, status);
	}
}