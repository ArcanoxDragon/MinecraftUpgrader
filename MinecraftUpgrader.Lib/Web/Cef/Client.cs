using Xilium.CefGlue;

namespace MinecraftUpgrader.Web.Cef
{
	public class Client : CefClient
	{
		public CefRequestHandler RequestHandler { get; set; }

		protected override CefRequestHandler GetRequestHandler() => this.RequestHandler;
	}
}