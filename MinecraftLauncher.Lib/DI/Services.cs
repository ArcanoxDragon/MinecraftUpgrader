using System;
using Microsoft.Extensions.DependencyInjection;
using MinecraftLauncher.Modpack;
using MinecraftLauncher.Options;

namespace MinecraftLauncher.DI
{
	public static class Services
	{
		private static IServiceProvider instance;

		public static IServiceProvider Instance => instance ?? throw new InvalidOperationException( "Services have not been configured yet" );

		public static void Configure( string modPackUrl )
		{
			var services = new ServiceCollection();

			services.Configure<PackBuilderOptions>( opts => opts.ModPackUrl = modPackUrl );
			services.AddTransient<PackBuilder>();

			instance = services.BuildServiceProvider();
		}

		public static object GetService( Type serviceType ) => Instance.GetService( serviceType );
		public static object GetRequiredService( Type serviceType ) => Instance.GetRequiredService( serviceType );
		public static TService GetService<TService>() => Instance.GetService<TService>();
		public static TService GetRequiredService<TService>() => Instance.GetRequiredService<TService>();

		public static T CreateInstance<T>( params object[] additionalConstructorParams )
			=> ActivatorUtilities.CreateInstance<T>( Instance, additionalConstructorParams );
	}
}