using System;
using System.Diagnostics;
using System.IO;
using Xilium.CefGlue;

namespace MinecraftUpgrader.Web.Cef
{
	public class InterceptResponseFilter : CefResponseFilter, IDisposable
	{
		public event Action<long> DataReceived;

		private Stream stream;

		public InterceptResponseFilter( Stream stream )
		{
			this.stream = stream;
		}

		public void Dispose()
		{
			this.stream = null;
		}

		protected override bool InitFilter() => true;

		protected override CefResponseFilterStatus Filter( UnmanagedMemoryStream dataIn, long dataInSize, out long dataInRead, UnmanagedMemoryStream dataOut, long dataOutSize, out long dataOutWritten )
		{
			if ( dataIn == null )
			{
				dataInRead     = 0;
				dataOutWritten = 0;

				return CefResponseFilterStatus.Done;
			}

			dataInRead     = Math.Min( dataIn.Length, dataOut.Length );
			dataOutWritten = dataInRead;

			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} Begin read {dataInRead} bytes" );

			var buffer = new byte[ dataInRead ];

			dataIn.Read( buffer, 0, buffer.Length );
			dataOut.Write( buffer, 0, buffer.Length );
			this.stream.Write( buffer, 0, buffer.Length );

			Debug.WriteLine( $"{DateTime.Now:yyyy-MMMM-dd hh:mm:ss tt} End write {dataOutWritten} bytes" );

			this.DataReceived?.Invoke( dataOutWritten );

			if ( dataInRead < dataIn.Length )
				return CefResponseFilterStatus.NeedMoreData;

			return CefResponseFilterStatus.Done;
		}
	}
}