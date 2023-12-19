using MinecraftUpgrader.Async;

namespace MinecraftUpgrader;

public sealed partial class ProgressDialog : Form
{
	public event EventHandler Cancel;

	public ProgressDialog(string taskName)
	{
		Reporter = new ProgressReporter();
		Reporter.OnProgress += ReporterOnProgress;
		Text = taskName;

		InitializeComponent();
	}

	private void ReporterOnProgress(double? progress, string status)
	{
		Invoke(() => {
			if (progress != null)
			{
				this.pbProgress.Style = progress >= 0 ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
				this.pbProgress.Value = Math.Max(Math.Min((int) ( progress * 100.0 ), this.pbProgress.Maximum), this.pbProgress.Minimum);
				this.lbPercentage.Visible = progress >= 0;
				this.lbPercentage.Text = $"{this.pbProgress.Value}%";
			}

			if (!string.IsNullOrEmpty(status))
				this.lbStatus.Text = status;
		});
	}

	public ProgressReporter Reporter { get; }

	private void OnBtnCancelClick(object sender, EventArgs e)
	{
		var result = MessageBox.Show("Are you sure you want to cancel?",
									 "Cancel Operation",
									 MessageBoxButtons.YesNo,
									 MessageBoxIcon.Question);

		if (result == DialogResult.Yes)
		{
			Cancel?.Invoke(this, EventArgs.Empty);
			this.btnCancel.Enabled = false;
		}
	}
}