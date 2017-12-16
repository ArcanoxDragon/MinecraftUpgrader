using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MinecraftUpgrader.Config
{
	public class ConfigReader
	{
		private static async Task<Dictionary<string, string>> ReadConfigValues( TextReader reader )
		{
			string line;
			var    values = new Dictionary<string, string>();

			while ( ( line = await reader.ReadLineAsync() ) != null )
			{
				var match = Regex.Match( line, @"([\w\d_]+)=([^\r\n]+)" );

				if ( match.Success )
				{
					var propName  = match.Groups[ 1 ].Value;
					var propValue = match.Groups[ 2 ].Value;

					values[ propName ] = propValue;
				}
			}

			return values;
		}

		private static async Task WriteConfigValues( Dictionary<string, string> values, TextWriter writer )
		{
			foreach ( var key in values.Keys.OrderBy( k => k ) )
				await writer.WriteLineAsync( $"{key}={values[ key ] ?? ""}" );
		}

		private static void SetProperties<T>( T source, Dictionary<string, string> targetDictionary )
		{
			var objProps    = typeof( T ).GetProperties( BindingFlags.Public | BindingFlags.Instance );
			var configProps = from p in objProps
							  let att = p.GetCustomAttribute<ConfigPropertyAttribute>()
							  where att != null
							  select (p, att);

			foreach ( var (prop, att) in configProps )
				// ReSharper disable PossibleNullReferenceException (this is an incorrect warning by ReSharper; will literally never happen)
				targetDictionary[ att.PropertyName ?? prop.Name ] = prop.GetValue( source )?.ToString();
			// ReSharper restore PossibleNullReferenceException
		}

		public static async Task<T> ReadConfig<T>( TextReader reader ) where T : new()
		{
			var propValues  = await ReadConfigValues( reader );
			var objProps    = typeof( T ).GetProperties( BindingFlags.Public | BindingFlags.Instance );
			var configProps = from p in objProps
							  let att = p.GetCustomAttribute<ConfigPropertyAttribute>()
							  where att != null
							  select (p, att);

			var obj = new T();

			foreach ( var (prop, att) in configProps )
			{
				var    propName = att.PropertyName ?? prop.Name;
				string propVal  = null;

				if ( propValues.TryGetValue( propName, out var val ) )
					propVal = val;

				if ( prop.PropertyType == typeof( string ) )
					prop.SetValue( obj, propVal );
				else if ( prop.PropertyType == typeof( int ) && int.TryParse( propVal, out var i ) )
					prop.SetValue( obj, i );
				else if ( prop.PropertyType == typeof( double ) && double.TryParse( propVal, out var d ) )
					prop.SetValue( obj, d );
				else if ( prop.PropertyType == typeof( bool ) )
				{
					if ( propVal?.ToLower() == "true" || propVal == "1" )
						prop.SetValue( obj, true );
					else
						prop.SetValue( obj, false );
				}
			}

			return obj;
		}

		public static async Task WriteConfig<T>( T config, TextWriter writer )
		{
			var propValues = new Dictionary<string, string>();

			SetProperties( config, propValues );
			await WriteConfigValues( propValues, writer );
		}

		public static async Task UpdateConfig<T>( T updatedConfig, Stream configStream )
		{
			using ( var sr = new StreamReader( configStream ) )
			using ( var sw = new StreamWriter( configStream ) )
			{
				// Seek to the beginning
				configStream.Seek( 0, SeekOrigin.Begin );

				var propValues = await ReadConfigValues( sr );

				SetProperties( updatedConfig, propValues );

				// Seek to the beginning and clear the stream
				configStream.Seek( 0, SeekOrigin.Begin );
				configStream.SetLength( 0 );

				await WriteConfigValues( propValues, sw );
			}
		}
	}
}