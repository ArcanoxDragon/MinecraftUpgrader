namespace MinecraftUpgrader.Extensions;

internal static class ExceptionExtensions
{
	public static string GetFriendlyMessage(this Exception exception)
		=> exception switch {
			OperationCanceledException { InnerException: TimeoutException }
				=> "The connection timed out while trying to reach the server",

			_ => exception.Message,
		};
}