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

		public McLauncher( PackMetadata packMetadata, InstanceMetadata instanceMetadata )
		{
			this.packMetadata     = packMetadata;
			this.instanceMetadata = instanceMetadata;
		}

		public Process CurrentProcess { get; private set; }

		public async Task<bool> StartMinecraftAsync( IWin32Window fromWindow )
		{
			if ( this.instanceMetadata == null )
				return false;

			var session = await SessionUtil.GetSessionAsync( fromWindow );

			if ( session == null )
				return false;

			var launchOptions = this.CreateLaunchOptions( session );

			launchOptions.StartVersion = await this.GetVersionMetadataAsync( this.instanceMetadata.CurrentLaunchVersion );

			var launch  = new MLaunch( launchOptions );
			var process = launch.GetProcess();

			process.StartInfo.UseShellExecute        =  false;
			process.StartInfo.RedirectStandardError  =  true;
			process.StartInfo.RedirectStandardOutput =  true;
			process.EnableRaisingEvents              =  true;
			process.Exited                           += ( sender, args ) => this.CurrentProcess = null;

			process.Start();

			this.CurrentProcess = process;
			this.Launched?.Invoke( this, EventArgs.Empty );

			return true;
		}

		private MLaunchOption CreateLaunchOptions( MSession session )
		{
			var computerInfo = new ComputerInfo();
			var ramSize      = ( (long) computerInfo.TotalPhysicalMemory ).Bytes();
			// Set JVM max memory to 2 GB less than the user's total RAM, at most 12 GB but at least 5
			var maxRamGb = (int) Math.Max( Math.Min( ramSize.Gigabytes - 4, 12 ), 5 );

			return new MLaunchOption {
				Path             = new MinecraftPath( PackBuilder.ProfilePath ),
				JavaPath         = AppConfig.Get().JavaPath,
				MinimumRamMb     = (int) 1.Gigabytes().Megabytes,
				MaximumRamMb     = (int) maxRamGb.Gigabytes().Megabytes,
				GameLauncherName = this.packMetadata.DisplayName,
				Session          = session,
			};
		}

		private async Task<MVersion> GetVersionMetadataAsync( string versionString )
		{
			var versionLoader = new MVersionLoader( new MinecraftPath( PackBuilder.ProfilePath ) );
			var versions      = await versionLoader.GetVersionMetadatasAsync();

			return versions.GetVersion( versionString );
		}
	}
}