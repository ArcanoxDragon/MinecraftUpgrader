using System.Text;

namespace MinecraftLauncher.Utility
{
	public class EncodingUtil
	{
		static EncodingUtil()
		{
			UTF8NoBom = new UTF8Encoding( false );
		}

		public static Encoding UTF8NoBom { get; }
	}
}