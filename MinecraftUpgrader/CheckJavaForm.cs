﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;

namespace MinecraftUpgrader
{
	public partial class CheckJavaForm : Form
	{
		private readonly CancellationTokenSource cancel;

		public CheckJavaForm()
		{
			this.cancel = new CancellationTokenSource();

			this.InitializeComponent();
		}

		private void CheckJavaForm_Load( object sender, EventArgs e )
		{
			if ( !Environment.Is64BitOperatingSystem )
			{
				MessageBox.Show( "Your computer is running a 32-bit operating system.\n\n" +
								 "You must be using a 64-bit operating system to run Minecraft.",
								 "Wrong Architecture",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Error );
				Application.Exit();
			}

			var ci = new ComputerInfo();
			var ramSize = new FileSize( (long) ci.TotalPhysicalMemory );

			if ( ramSize.GigaBytes < 7.5 )
			{
				MessageBox.Show( "You have less than 8 GB of memory installed in your computer.\n\n" +
								 "Modded Minecraft requires a considerable amount of memory, and there " +
								 "may not be enough available memory in your system to run it.\n\n" +
								 "If the game crashes when starting up, you may need to install more " +
								 "memory in your system to play modded Minecraft.",
								 "Insufficient Memory",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Warning );
			}

			Task.Run( this.CheckJavaVersions ).ContinueWith( this.OnCheckFinished );
		}

		private void OnCheckFinished( Task task )
		{
			if ( task.IsCanceled )
			{
				Application.Exit();
				return;
			}

			if ( task.IsFaulted )
			{
				if ( task.Exception == null || task.Exception.InnerExceptions.Count == 0 )
				{
					MessageBox.Show( "An unknown error occurred. The application will now exit.",
									 "Unexpected Error",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );
					Application.Exit();
				}
				else if ( task.Exception.InnerExceptions.Count == 1 )
				{
					MessageBox.Show( "An error occurred while checking Java:\n\n" +
									 task.Exception.InnerExceptions.First().Message,
									 "Unexpected Error",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );
					Application.Exit();
				}
				else
				{
					MessageBox.Show( "The following errors occurred while checking Java:\n\n" +
									 task.Exception.InnerExceptions.Select( e => e.Message + "\n" ),
									 "Unexpected Error",
									 MessageBoxButtons.OK,
									 MessageBoxIcon.Error );
					Application.Exit();
				}

				return;
			}

			this.Invoke( new Action( () => {
				MessageBox.Show( "If you have not configured Java in MultiMC yet, make sure to choose " +
								 "a version starting with \"1.8\" and with an architecture of \"64\".\n\n" +
								 "If you do not do this, you will get an error from MultiMC when starting " +
								 "the instance saying that it cannot create the Java Virtual Machine.",
								 "Java Setup",
								 MessageBoxButtons.OK,
								 MessageBoxIcon.Information );

				this.Hide();
				new MainForm().ShowDialog();
				this.Close();
			} ) );
		}

		private async Task CheckJavaVersions()
		{
			var javaVersions = this.FindJavaPaths();
			var results = new List<bool>();

			foreach ( var version in javaVersions )
				results.Add( await this.CheckJava( version ) );

			if ( !results.Any( r => r ) )
			{
				this.Invoke( new Action( () => {
					var result = MessageBox.Show( "You do not have any compatible Java versions installed; " +
												  "modded Minecraft requires at least Java 8 64-bit.\n\n" +
												  "You can download Java from the official website. Would you " +
												  "like to go there now?",
												  "Java Not Found",
												  MessageBoxButtons.YesNo,
												  MessageBoxIcon.Error );

					if ( result == DialogResult.Yes )
					{
						MessageBox.Show( "When you click OK, the Java website will open in a browser window.\n\n" +
										 "On the website, click the link for \"Windows Offline (64-bit)\" and " +
										 "follow the instructions to install 64-bit Java.",
										 "Installing Java",
										 MessageBoxButtons.OK,
										 MessageBoxIcon.Information );
						Process.Start( "https://www.java.com/en/download/manual.jsp" );
					}
				} ) );

				Application.Exit();
			}
		}

		private Task<bool> CheckJava( string javaExe )
		{
			var task = new TaskCompletionSource<bool>();
			var process = new Process {
				EnableRaisingEvents = true,
				StartInfo = {
					CreateNoWindow = true,
					FileName = javaExe,
					Arguments = "-version",
					UseShellExecute = false,
					ErrorDialog = true,
					RedirectStandardError = true
				}
			};

			this.cancel.Token.Register( () => task.TrySetCanceled() );

			process.Exited += ( o, e ) => {
				var output = process.StandardError.ReadToEnd(); // version information is printed to stderr, because Oracle
				var versionStrMatch = Regex.Match( output, @"java version ""([^""]+)""", RegexOptions.IgnoreCase );

				if ( !versionStrMatch.Success )
				{
					task.TrySetException( new Exception( $"Invalid Java process output from Java runtime: {javaExe}" ) );
					return;
				}

				var version = versionStrMatch.Groups[ 1 ].Value;
				var versionMatch = Regex.Match( version, @"(\d+)\.(\d+)" );
				var majorStr = versionMatch.Groups[ 2 ].Value;

				if ( !versionStrMatch.Success || !int.TryParse( majorStr, out int major ) )
				{
					task.TrySetException( new Exception( $"Invalid Java version: {version} ({javaExe})" ) );
					return;
				}

				task.TrySetResult( major >= 8 );
			};

			process.Start();

			return task.Task;
		}

		private IEnumerable<string> FindJavaPaths()
		{
			var javaRoot = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles ), "Java" );
			var javaFiles = Directory.EnumerateFiles( javaRoot, "*.exe", SearchOption.AllDirectories );
			var javaExes = from file in javaFiles
						   where Path.GetFileName( file )?.ToLower() == "java.exe"
						   select file;

			return javaExes;
		}

		private void OnBtnCancelClick( object sender, EventArgs e )
		{
			this.btnCancel.Enabled = false;
			this.cancel.Cancel();
		}
	}
}