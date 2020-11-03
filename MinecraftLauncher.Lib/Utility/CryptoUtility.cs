using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MinecraftLauncher.Utility
{
	public static class CryptoUtility
	{
		public static string CalculateFileMd5( string filePath )
		{
			var       md5       = MD5.Create();
			using var fs        = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
			var       localHash = md5.ComputeHash( fs );

			return string.Join( "", localHash.Select( b => b.ToString( "x2" ) ) );
		}
	}
}