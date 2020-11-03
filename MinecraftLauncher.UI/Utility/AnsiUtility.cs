using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace MinecraftLauncher.Utility
{
	public static class AnsiUtility
	{
		private static readonly Regex AnsiCodeRegex  = new Regex( @"\u001b\[(?<N1>\d+)(?:;(?<N2>\d+))?m" );
		private static readonly Regex AnsiStripRegex = new Regex( @"\u001b\[[^\u0040-\u007e]*[\u0040-\u007e]" );

		private static readonly Dictionary<int, Color> FgCodeTable = new Dictionary<int, Color> {
			// Normal FG
			{ 30, Color.FromArgb( 0, 0, 0 ) },
			{ 31, Color.FromArgb( 128, 0, 0 ) },
			{ 32, Color.FromArgb( 0, 128, 0 ) },
			{ 33, Color.FromArgb( 128, 128, 0 ) },
			{ 34, Color.FromArgb( 0, 0, 128 ) },
			{ 35, Color.FromArgb( 128, 0, 128 ) },
			{ 36, Color.FromArgb( 0, 128, 128 ) },
			{ 37, Color.FromArgb( 192, 192, 192 ) },

			// Bright FG
			{ 90, Color.FromArgb( 128, 128, 128 ) },
			{ 91, Color.FromArgb( 255, 0, 0 ) },
			{ 92, Color.FromArgb( 0, 255, 0 ) },
			{ 93, Color.FromArgb( 255, 255, 0 ) },
			{ 94, Color.FromArgb( 0, 0, 255 ) },
			{ 95, Color.FromArgb( 255, 0, 255 ) },
			{ 96, Color.FromArgb( 0, 255, 255 ) },
			{ 97, Color.FromArgb( 255, 255, 255 ) },
		};

		private static readonly Dictionary<int, Color> BgCodeTable = new Dictionary<int, Color> {
			// Normal BG
			{ 40, Color.FromArgb( 0, 0, 0 ) },
			{ 41, Color.FromArgb( 128, 0, 0 ) },
			{ 42, Color.FromArgb( 0, 128, 0 ) },
			{ 43, Color.FromArgb( 128, 128, 0 ) },
			{ 44, Color.FromArgb( 0, 0, 128 ) },
			{ 45, Color.FromArgb( 128, 0, 128 ) },
			{ 46, Color.FromArgb( 0, 128, 128 ) },
			{ 47, Color.FromArgb( 192, 192, 192 ) },

			// Bright BG
			{ 100, Color.FromArgb( 128, 128, 128 ) },
			{ 101, Color.FromArgb( 255, 0, 0 ) },
			{ 102, Color.FromArgb( 0, 255, 0 ) },
			{ 103, Color.FromArgb( 255, 255, 0 ) },
			{ 104, Color.FromArgb( 0, 0, 255 ) },
			{ 105, Color.FromArgb( 255, 0, 255 ) },
			{ 106, Color.FromArgb( 0, 255, 255 ) },
			{ 107, Color.FromArgb( 255, 255, 255 ) },
		};

		public static (string stripped, Color? fg, Color? bg) ParseFirstFgAnsiColor( string text )
		{
			var match      = AnsiCodeRegex.Match( text );
			var foreground = default(Color?);
			var background = default(Color?);

			if ( match.Success )
			{
				var num1 = int.Parse( match.Groups[ "N1" ].Value );

				if ( FgCodeTable.TryGetValue( num1, out var color ) )
					foreground = color;
				else if ( BgCodeTable.TryGetValue( num1, out color ) )
					background = color;

				if ( match.Groups[ "N2" ].Success )
				{
					var num2 = int.Parse( match.Groups[ "N2" ].Value );

					if ( FgCodeTable.TryGetValue( num2, out color ) )
						foreground = color;
					else if ( BgCodeTable.TryGetValue( num2, out color ) )
						background = color;
				}
			}

			var stripped = AnsiStripRegex.Replace( text, "" );

			return ( stripped, foreground, background );
		}
	}
}