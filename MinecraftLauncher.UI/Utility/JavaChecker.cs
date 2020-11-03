using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CmlLib.Core.Downloader;
using Humanizer;
using MinecraftLauncher.Async;
using MinecraftLauncher.Config;
using NickStrupat;

namespace MinecraftLauncher.Utility
{
	public static class JavaChecker
	{
		public static async Task<bool> CheckJavaAsync()
		{
			if ( !Environment.Is64BitOperatingSystem )
			{
				MessageBox.Show( "Your computer is running a 32-bit operating system.\n\n" +
								 "You must be using a 64-bit operating system to run Minecraft.",
								 "Wrong Architecture",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );

				return false;
			}

			var ci      = new ComputerInfo();
			var ramSize = ( (long) ci.TotalPhysicalMemory ).Bytes();

			if ( ramSize.Gigabytes < Constants.Minecraft.RecommendedMemoryGb )
			{
				MessageBox.Show( $"You have less than {Constants.Minecraft.RecommendedMemoryGb} GB of memory installed in your computer.\n\n" +
								 "Modded Minecraft requires a considerable amount of memory, and there " +
								 "may not be enough available memory in your system to run it.\n\n" +
								 "If the game crashes when starting up, you may need to install more " +
								 "memory in your system to play modded Minecraft.",
								 "Insufficient Memory",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Warning );
			}

			var cancellationTokenSource = new CancellationTokenSource();
			var progressDialog = new ProgressDialog( "Checking Java Compatibility" ) {
				AllowCancel   = true,
				ConfirmCancel = false,
			};

			progressDialog.Cancel += ( sender, args ) => cancellationTokenSource.Cancel();

			try
			{
				progressDialog.Show();
				await CheckJavaVersionAsync( cancellationTokenSource.Token, progressDialog.Reporter );

				return true;
			}
			catch ( Exception ex )
			{
				if ( !( ex is TaskCanceledException || ex is OperationCanceledException ) )
				{
					MessageBox.Show( $"An error occurred while checking Java:\n\n{ex.Message}",
									 "Unexpected Error",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );
				}

				return false;
			}
			finally
			{
				progressDialog.Close();
			}
		}

		private static async Task CheckJavaVersionAsync( CancellationToken cancellationToken, ProgressReporter progressDialogReporter )
		{
			var java = new MJava();

			java.ProgressChanged += ( sender, args ) => {
				progressDialogReporter?.ReportProgress( args.ProgressPercentage / 100.0, "Installing Java..." );
			};

			var javaPath = await java.CheckJavaAsync(cancellationToken);

			cancellationToken.ThrowIfCancellationRequested();

			AppConfig.Update( appConfig => appConfig.JavaPath = javaPath );
		}
	}
}