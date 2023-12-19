using System.Runtime.CompilerServices;

namespace MinecraftUpgrader.Extensions;

public static class WinFormsExtensions
{
	#region Invoke Extensions

	public static void Invoke(this Control control, Action action)
		=> control.Invoke(action);

	public static void BeginInvoke(this Control control, Action action)
		=> control.BeginInvoke(action);

	public static void TryInvoke(this Control control, Action action)
	{
		try
		{
			control.Invoke(action);
		}
		catch
		{
			// Ignored
		}
	}

	public static void TryBeginInvoke(this Control control, Action action)
	{
		try
		{
			control.BeginInvoke(action);
		}
		catch
		{
			// Ignored
		}
	}

	#endregion

	#region Disable While

	private sealed record DisableWhileState
	{
		public int  DisableCount    { get; set; }
		public bool OriginalEnabled { get; set; }
	}

	private static readonly ConditionalWeakTable<Control, DisableWhileState> DisableWhileStates = new();

	public static void DisableWhile(this Control control, Action action)
	{
		var state = DisableWhileStates.GetOrCreateValue(control)!;

		if (state.DisableCount++ == 0)
			state.OriginalEnabled = control.Enabled;

		control.Enabled = false;

		try
		{
			action();
		}
		finally
		{
			if (--state.DisableCount <= 0)
			{
				state.DisableCount = 0;
				control.Enabled = state.OriginalEnabled;
			}
		}
	}

	public static async Task DisableWhile(this Control control, Func<Task> action)
	{
		var state = DisableWhileStates.GetOrCreateValue(control)!;

		if (state.DisableCount++ == 0)
			state.OriginalEnabled = control.Enabled;

		control.Enabled = false;

		try
		{
			await action();
		}
		finally
		{
			if (--state.DisableCount <= 0)
			{
				state.DisableCount = 0;
				control.Enabled = state.OriginalEnabled;
			}
		}
	}

	#endregion

	public static void AppendText(this RichTextBox textBox, string text, Color? foreground = default, Color? background = default)
	{
		textBox.Select(textBox.TextLength, 0);

		if (foreground != default)
			textBox.SelectionColor = foreground.Value;

		if (background != default)
			textBox.SelectionBackColor = background.Value;

		textBox.AppendText(text);
		textBox.SelectionColor = textBox.ForeColor;
		textBox.SelectionBackColor = textBox.BackColor;
	}
}