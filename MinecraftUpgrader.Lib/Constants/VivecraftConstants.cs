namespace MinecraftUpgrader.Constants
{
	public class VivecraftConstants
	{
		public const string OptifineVersion          = "1.12.2_HD_U_E3";
		public const string OptifineBaseUri          = "https://optifine.net/";
		public const string OptifineDownloadFilename = "OptiFine_" + OptifineVersion + ".jar";
		public const string OptifineLibraryFilename  = "OptiFine-" + OptifineVersion + ".jar";
		public const string OptifineMirrorUri        = OptifineBaseUri + "adloadx?f=" + OptifineDownloadFilename;

		public const string VivecraftVersionVr     = "1.12.2-jrbudda-9";
		public const string VivecraftRevisionVr    = "r6";
		public const string VivecraftVersionNonVr  = "1.12.2-jrbudda-NONVR-9";
		public const string VivecraftRevisionNonVr = "r1";

		public const string VivecraftJvmArgs = "-XX:+UseParallelGC -XX:ParallelGCThreads=3 -XX:MaxGCPauseMillis=3 -Xmn256M -Dfml.ignoreInvalidMinecraftCertificates=true -Dfml.ignorePatchDiscrepancies=true";

		public static string GetVivecraftInstallerUri( bool vrEnabled )
		{
			var vivecraftVersion  = vrEnabled ? VivecraftVersionVr : VivecraftVersionNonVr;
			var vivecraftRevision = vrEnabled ? VivecraftRevisionVr : VivecraftRevisionNonVr;

			return $"https://github.com/jrbudda/Vivecraft_112/releases/download/{VivecraftVersionVr}/vivecraft-{vivecraftVersion}-{vivecraftRevision}-installer.exe";
		}
	}
}