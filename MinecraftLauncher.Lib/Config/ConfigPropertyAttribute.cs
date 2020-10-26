using System;

namespace MinecraftLauncher.Config
{
	public class ConfigPropertyAttribute : Attribute
	{
		public ConfigPropertyAttribute( string propertyName = null )
		{
			this.PropertyName = propertyName;
		}

		public string PropertyName { get; }
	}
}