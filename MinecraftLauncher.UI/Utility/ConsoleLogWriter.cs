using System;
using System.IO;
using System.Text;

namespace MinecraftLauncher.Utility
{
	public class ConsoleLogWriter : TextWriter
	{
		private readonly string       logFilePath;
		private readonly TextWriter   inner;
		private readonly Stream       fileStream;
		private readonly StreamWriter fileStreamWriter;

		private bool disposed;

		public ConsoleLogWriter( string logFilePath, TextWriter inner )
		{
			this.logFilePath      = logFilePath ?? throw new ArgumentNullException( nameof(logFilePath) );
			this.inner            = inner ?? throw new ArgumentNullException( nameof(inner) );

			Directory.CreateDirectory( Path.GetDirectoryName( this.logFilePath ) );

			this.fileStream       = File.Open( this.logFilePath, FileMode.Append, FileAccess.Write, FileShare.Read );
			this.fileStreamWriter = new StreamWriter( this.fileStream );
		}

		public override Encoding Encoding => this.inner.Encoding;

		public override void Write( char value )
		{
			this.inner.Write( value );
			this.fileStreamWriter.Write( value );
		}

		public override void Flush()
		{
			this.inner.Flush();
			this.fileStreamWriter.Flush();
		}

		#region IDisposable

		protected override void Dispose( bool disposing )
		{
			if ( this.disposed )
				return;

			base.Dispose( disposing );

			if ( disposing )
			{
				this.fileStreamWriter.Dispose();
				this.fileStream.Dispose();
				this.inner.Dispose();
			}

			this.disposed = true;
		}

		#endregion
	}
}