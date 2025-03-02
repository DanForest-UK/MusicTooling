using Microsoft.ReactNative;
using MusicTools;
using MusicTools.Logic;
using MusicTools.NativeModules;
using System.Diagnostics;
using System;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MusicTools
{
    sealed partial class App : ReactApplication
    {
        public App()
        {
            AddFirewallException();

            // Dependency injection
            Runtime.GetFilesWithExtensionAsync = React.GetFilesWithExtensionAsync;
            Runtime.ReadSongInfo = ReadTag.ReadSongInfo;
            Runtime.WithStream = React.WithStream;

#if BUNDLE
            JavaScriptBundleFile = "index.windows";
            InstanceSettings.UseFastRefresh = false;
#else
            JavaScriptBundleFile = "index";
            InstanceSettings.UseFastRefresh = true;
#endif

#if DEBUG
            InstanceSettings.UseDirectDebugger = true;
            InstanceSettings.UseDeveloperSupport = true;
#else
            InstanceSettings.UseDirectDebugger = false;
            InstanceSettings.UseDeveloperSupport = false;
#endif

            Microsoft.ReactNative.Managed.AutolinkedNativeModules.RegisterAutolinkedNativeModulePackages(PackageProviders); // Includes any autolinked modules

            PackageProviders.Add(new ReactPackageProvider());

            InitializeComponent();
        }

 

            private void AddFirewallException()
            {
                try
                {
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "advfirewall firewall add rule name=\"Spotify Auth Callback\" dir=in action=allow protocol=TCP localport=8888",
                        Verb = "runas",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    Debug.WriteLine("Added firewall rule for port 8888");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to add firewall rule: {ex.Message}");
                    // Continue anyway, as the rule might already exist or we don't have sufficient privileges
                }
            }

     
        /// <summary>
        /// Entry point
        /// </summary>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            base.OnLaunched(e);
            var frame = (Frame)Window.Current.Content;
            frame.Navigate(typeof(MainPage), e.Arguments);
        }

        /// <summary>
        /// Invoked when the application is activated by some means other than normal launching.
        /// </summary>
        protected override void OnActivated(Windows.ApplicationModel.Activation.IActivatedEventArgs e)
        {
            var preActivationContent = Window.Current.Content;
            base.OnActivated(e);
            if (preActivationContent == null && Window.Current != null)
            {
                // Display the initial content
                var frame = (Frame)Window.Current.Content;
                frame.Navigate(typeof(MainPage), null);
            }
        }
    }
}
