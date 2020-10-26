using System;
using System.Drawing;
using System.Windows.Forms;

namespace MinecraftLauncher.Extensions
{
	public static class WinFormsExtensions
	{
		public static void Invoke( this Control control, Action action )
			=> control.Invoke( new Action( action ) );

		public static void BeginInvoke( this Control control, Action action )
			=> control.BeginInvoke( new Action( action ) );

		public static void TryInvoke( this Control control, Action action )
		{
			try
			{
				control.Invoke( new Action( action ) );
			}
			catch
			{
				// Ignored
			}
		}

		public static void TryBeginInvoke( this Control control, Action action )
		{
			try
			{
				control.BeginInvoke( new Action( action ) );
			}
			catch
			{
				// Ignored
			}
		}

		public static void AppendText( this RichTextBox textBox, string text, Color? foreground = default, Color? background = default )
		{
			textBox.Select( textBox.TextLength, 0 );

			if ( foreground != default )
				textBox.SelectionColor = foreground.Value;

			if ( background != default )
				textBox.SelectionBackColor = background.Value;

			textBox.AppendText( text );
			textBox.SelectionColor     = textBox.ForeColor;
			textBox.SelectionBackColor = textBox.BackColor;
		}
	}
}