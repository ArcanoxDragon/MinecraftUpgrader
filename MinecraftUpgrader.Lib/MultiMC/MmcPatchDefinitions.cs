using System;
using System.Collections.Generic;
using static MinecraftUpgrader.Utility.Constants.Vivecraft;

namespace MinecraftUpgrader.MultiMC
{
	public class MmcPatchDefinitions
	{
		public static readonly MmcPatchesFile VivecraftPatchVr = new() {
			Uid = "vivecraft",
			Name = "Vivecraft",
			ReleaseTime = DateTime.Now,
			Time = DateTime.Now,
			MainClass = "net.minecraft.launchwrapper.Launch",
			Version = "4",
			Libraries = new List<MmcLibrary> {
				new() {
					Name = $"com.mtbs3d:minecrift:{VivecraftVersionVr}-{VivecraftRevisionVr}",
					MmcHint = "local",
				},
				new() {
					Name = "org.json:json:20140107",
					Url = "http://vivecraft.org/jar/",
				},
				new() {
					Name = "com.sun:jna:4.2.1",
					Url = "http://vivecraft.org/jar/",
				},
				new() {
					Name = "org.ow2.asm:asm-all:5.2",
					Url = "http://files.minecraftforge.net/maven/",
				},
				new() {
					Name = "net.minecraft:launchwrapper:1.12",
				},
				new() {
					Name = $"optifine:OptiFine:{OptifineVersion}",
					MmcHint = "local",
				},
			},
			Tweakers = new List<string> {
				"org.vivecraft.tweaker.MinecriftForgeTweaker",
				"net.minecraftforge.fml.common.launcher.FMLTweaker",
				"optifine.OptiFineForgeTweaker",
			},
		};

		public static readonly MmcPatchesFile VivecraftPatchNonVr = new() {
			Uid = "vivecraft",
			Name = "Vivecraft",
			ReleaseTime = DateTime.Now,
			Time = DateTime.Now,
			MainClass = "net.minecraft.launchwrapper.Launch",
			Version = "4",
			Libraries = new List<MmcLibrary> {
				new() {
					Name = $"com.mtbs3d:minecrift:{VivecraftVersionNonVr}-{VivecraftRevisionNonVr}",
					MmcHint = "local",
				},
				new() {
					Name = "org.json:json:20140107",
					Url = "http://vivecraft.org/jar/",
				},
				new() {
					Name = "org.ow2.asm:asm-all:5.2",
					Url = "http://files.minecraftforge.net/maven/",
				},
				new() {
					Name = "net.minecraft:launchwrapper:1.12",
				},
				new() {
					Name = $"optifine:OptiFine:{OptifineVersion}",
					MmcHint = "local",
				},
				new() {
					Name = "de.fruitfly.ovr:JRift:0.8.0.0.1",
					Url = "http://vivecraft.org/jar/",
				},
			},
			Tweakers = new List<string> {
				"org.vivecraft.tweaker.MinecriftForgeTweaker",
				"net.minecraftforge.fml.common.launcher.FMLTweaker",
				"optifine.OptiFineForgeTweaker",
			},
		};
	}
}