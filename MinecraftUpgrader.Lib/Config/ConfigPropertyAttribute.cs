using System;

namespace MinecraftUpgrader.Config
{
	public class ConfigPropertyAttribute : Attribute
	{
		public ConfigPropertyAttribute(string propertyName = null)
		{
			PropertyName = propertyName;
		}

		public string PropertyName { get; }
	}
}