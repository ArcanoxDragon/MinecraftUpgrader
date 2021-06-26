using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Version;
using Humanizer;
using MinecraftLauncher.Config;
using MinecraftLauncher.Modpack;
using NickStrupat;

namespace MinecraftLauncher.Utility
{
	public class McLauncher
	{
		public event EventHandler Launched;

		private readonly PackMetadata     packMetadata;
		private readonly InstanceMetadata instanceMetadata;
		private readonly bool             isVrEnabled;

		public McLauncher(PackMetadata packMetadata, InstanceMetadata instanceMetadata, bool isVrEnabled = false)
		{
			this.packMetadata     = packMetadata;
			this.instanceMetadata = instanceMetadata;
			this.isVrEnabled      = isVrEnabled;
		}

		public Process CurrentProcess { get; private set; }

		public async Task<bool> StartMinecraftAsync(IWin32Window fromWindow)
		{
			if (this.instanceMetadata == null)
				return false;

			var session = await SessionUtil.GetSessionAsync(fromWindow);

			if (session == null)
				return false;

			var launchOptions = CreateLaunchOptions(session);
			var launchVersion = this.instanceMetadata.CurrentLaunchVersion;

			if (this.packMetadata.SupportsVr)
			{
				if (this.isVrEnabled)
					launchVersion = this.instanceMetadata.VrLaunchVersion;
				else if (this.packMetadata.EnableVivecraftForNonVrPlayers != false)
					launchVersion = this.instanceMetadata.NonVrLaunchVersion;
			}

			launchOptions.StartVersion = await GetVersionMetadataAsync(launchVersion);

			var launch = new MLaunch(launchOptions);
			var process = launch.GetProcess();

			process.StartInfo.UseShellExecute        =  false;
			process.StartInfo.RedirectStandardError  =  true;
			process.StartInfo.RedirectStandardOutput =  true;
			process.EnableRaisingEvents              =  true;
			process.Exited                           += (_, _) => CurrentProcess = null;

			process.Start();

			CurrentProcess = process;
			Launched?.Invoke(this, EventArgs.Empty);

			return true;
		}

		private MLaunchOption CreateLaunchOptions(MSession session)
		{
			var computerInfo = new ComputerInfo();
			var config = AppConfig.Get();
			var ramSize = ( (long) computerInfo.TotalPhysicalMemory ).Bytes();
			// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB but at least 5
			var maxRamGb = (int) Math.Max(Math.Min(ramSize.Gigabytes - 4, 12), 5);
			var maxRamMb = maxRamGb.Gigabytes().Megabytes;

			if (config.MaxRamMb is not null)
			{
				if (config.MaxRamMb <= 2048)
					throw new InvalidOperationException("Max RAM must be greater than 2048 megabytes");

				maxRamMb = Math.Min(maxRamMb, config.MaxRamMb.Value);
			}

			return new MLaunchOption {
				Path             = new MinecraftPath(PackBuilder.ProfilePath),
				JavaPath         = config.JavaPath,
				MinimumRamMb     = (int) 1.Gigabytes().Megabytes,
				MaximumRamMb     = (int) maxRamMb,
				GameLauncherName = this.packMetadata.DisplayName,
				Session          = session,
			};
		}

		private async Task<MVersion> GetVersionMetadataAsync(string versionString)
		{
			var versionLoader = new MVersionLoader(new MinecraftPath(PackBuilder.ProfilePath));
			var versions = await versionLoader.GetVersionMetadatasAsync();

			return versions.GetVersion(versionString);
		}
	}
}