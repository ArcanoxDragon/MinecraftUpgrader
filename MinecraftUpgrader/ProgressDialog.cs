using System;
using System.Windows.Forms;
using MinecraftUpgrader.Async;

namespace MinecraftUpgrader
{
	public sealed partial class ProgressDialog : Form
	{
		public event EventHandler Cancel;

		public ProgressDialog( string taskName )
		{
			this.Reporter = new ProgressReporter();
			this.Reporter.OnProgress += this.ReporterOnProgress;
			this.Text = taskName;

			this.InitializeComponent();
		}

		private void ReporterOnProgress( double? progress, string status )
		{
			this.Invoke( new Action( () => {
				if ( progress != null )
				{
					this.pbProgress.Style = progress >= 0 ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
					this.pbProgress.Value = Math.Max( Math.Min( (int) ( progress * 100.0 ), this.pbProgress.Maximum ), this.pbProgress.Minimum );
					this.lbPercentage.Visible = progress >= 0;
					this.lbPercentage.Text = $"{this.pbProgress.Value}%";
				}

				if ( !string.IsNullOrEmpty( status ) )
					this.lbStatus.Text = status;
			} ) );
		}

		public ProgressReporter Reporter { get; }

		private void OnBtnCancelClick( object sender, EventArgs e )
		{
			var result = MessageBox.Show( "Are you sure you want to cancel?",
										  "Cancel Operation",
										  MessageBoxButtons.YesNo,
										  MessageBoxIcon.Question );

			if ( result == DialogResult.Yes )
			{
				this.Cancel?.Invoke( this, new EventArgs() );
				this.btnCancel.Enabled = false;
			}
		}
	}
}