using System;

namespace MinecraftUpgrader.Config
{
	public class ConfigPropertyAttribute(string propertyName = null) : Attribute
	{
		public string PropertyName { get; } = propertyName;
	}
}