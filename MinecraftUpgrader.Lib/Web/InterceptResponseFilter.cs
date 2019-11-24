using System;
using System.Diagnostics;
using System.IO;
using CefSharp;

namespace MinecraftUpgrader.Web
{
	public class InterceptResponseFilter : IResponseFilter
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

		public bool InitFilter() => true;

		public FilterStatus Filter( Stream dataIn, out long dataInRead, Stream dataOut, out long dataOutWritten )
		{
			if ( dataIn == null )
			{
				dataInRead     = 0;
				dataOutWritten = 0;

				return FilterStatus.Done;
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
				return FilterStatus.NeedMoreData;

			return FilterStatus.Done;
		}
	}
}