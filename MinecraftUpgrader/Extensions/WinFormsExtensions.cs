using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MinecraftUpgrader.Extensions
{
	public static class WinFormsExtensions
	{
		#region Invoke Extensions

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

		#endregion

		public static void DisableWhile( this Control control, Action action )
		{
			var wasEnabled = control.Enabled;

			control.Enabled = false;

			try
			{
				action();
			}
			finally
			{
				control.Enabled = wasEnabled;
			}
		}

		public static async Task DisableWhile( this Control control, Func<Task> action )
		{
			var wasEnabled = control.Enabled;

			control.Enabled = false;

			try
			{
				await action();
			}
			finally
			{
				control.Enabled = wasEnabled;
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