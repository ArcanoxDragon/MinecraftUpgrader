using System;
using static MinecraftUpgrader.Utility.Constants.Vivecraft;

namespace MinecraftUpgrader.Prism;

public class PrismPatchDefinitions
{
	public static readonly PrismPatchesFile VivecraftPatchVr = new() {
		Uid = "vivecraft",
		Name = "Vivecraft",
		ReleaseTime = DateTime.Now,
		Time = DateTime.Now,
		MainClass = "net.minecraft.launchwrapper.Launch",
		Version = "4",
		Libraries = [
			new PrismLibrary {
				Name = $"com.mtbs3d:minecrift:{VivecraftVersionVr}-{VivecraftRevisionVr}",
				MmcHint = "local",
			},

			new PrismLibrary {
				Name = "org.json:json:20140107",
				Url = "http://vivecraft.org/jar/",
			},

			new PrismLibrary {
				Name = "com.sun:jna:4.2.1",
				Url = "http://vivecraft.org/jar/",
			},

			new PrismLibrary {
				Name = "org.ow2.asm:asm-all:5.2",
				Url = "http://files.minecraftforge.net/maven/",
			},

			new PrismLibrary {
				Name = "net.minecraft:launchwrapper:1.12",
			},

			new PrismLibrary {
				Name = $"optifine:OptiFine:{OptifineVersion}",
				MmcHint = "local",
			},
		],
		Tweakers = [
			"org.vivecraft.tweaker.MinecriftForgeTweaker",
			"net.minecraftforge.fml.common.launcher.FMLTweaker",
			"optifine.OptiFineForgeTweaker",
		],
	};

	public static readonly PrismPatchesFile VivecraftPatchNonVr = new() {
		Uid = "vivecraft",
		Name = "Vivecraft",
		ReleaseTime = DateTime.Now,
		Time = DateTime.Now,
		MainClass = "net.minecraft.launchwrapper.Launch",
		Version = "4",
		Libraries = [
			new PrismLibrary {
				Name = $"com.mtbs3d:minecrift:{VivecraftVersionNonVr}-{VivecraftRevisionNonVr}",
				MmcHint = "local",
			},

			new PrismLibrary {
				Name = "org.json:json:20140107",
				Url = "http://vivecraft.org/jar/",
			},

			new PrismLibrary {
				Name = "org.ow2.asm:asm-all:5.2",
				Url = "http://files.minecraftforge.net/maven/",
			},

			new PrismLibrary {
				Name = "net.minecraft:launchwrapper:1.12",
			},

			new PrismLibrary {
				Name = $"optifine:OptiFine:{OptifineVersion}",
				MmcHint = "local",
			},

			new PrismLibrary {
				Name = "de.fruitfly.ovr:JRift:0.8.0.0.1",
				Url = "http://vivecraft.org/jar/",
			},

		],
		Tweakers = [
			"org.vivecraft.tweaker.MinecriftForgeTweaker",
			"net.minecraftforge.fml.common.launcher.FMLTweaker",
			"optifine.OptiFineForgeTweaker",
		],
	};
}