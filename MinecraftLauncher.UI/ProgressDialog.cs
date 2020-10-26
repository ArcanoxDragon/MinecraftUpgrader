using System;
using System.Windows.Forms;
using MinecraftLauncher.Async;
using MinecraftLauncher.Extensions;

namespace MinecraftLauncher
{
	public sealed partial class ProgressDialog : Form
	{
		public event EventHandler Cancel;

		public ProgressDialog( string taskName )
		{
			this.Reporter            =  new ProgressReporter();
			this.Reporter.OnProgress += this.ReporterOnProgress;

			this.InitializeComponent();

			this.Text = taskName;
		}

		public bool AllowCancel
		{
			get => this.btnCancel.Enabled;
			set => this.btnCancel.Enabled = value;
		}

		public bool ConfirmCancel { get; set; }

		private void ReporterOnProgress( double? progress, string status )
		{
			this.BeginInvoke( () => {
				if ( progress != null )
				{
					this.pbProgress.Style     = progress >= 0 ? ProgressBarStyle.Continuous : ProgressBarStyle.Marquee;
					this.pbProgress.Value     = Math.Max( Math.Min( (int) ( progress * 100.0 ), this.pbProgress.Maximum ), this.pbProgress.Minimum );
					this.lbPercentage.Visible = progress >= 0;
					this.lbPercentage.Text    = $"{this.pbProgress.Value}%";
				}

				if ( !string.IsNullOrEmpty( status ) )
					this.lbStatus.Text = status;
			} );
		}

		public ProgressReporter Reporter { get; }

		private void OnBtnCancelClick( object sender, EventArgs e )
		{
			var doCancel = true;

			if ( this.ConfirmCancel )
			{
				var result = MessageBox.Show( "Are you sure you want to cancel?",
											  "Cancel Operation",
											  MessageBoxButtons.YesNo,
											  MessageBoxIcon.Question );

				doCancel = result == DialogResult.Yes;
			}

			if ( doCancel )
			{
				this.Cancel?.Invoke( this, new EventArgs() );
				this.btnCancel.Enabled = false;
			}
		}
	}
}