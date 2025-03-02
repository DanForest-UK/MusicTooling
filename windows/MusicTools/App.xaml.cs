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
            // The firewall exception is no longer needed since we're not using a localhost server
            // You can remove this if you're no longer using port 8888 for anything else
            // AddFirewallException();

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
        /// This handles protocol activation for Spotify callbacks.
        /// </summary>
        protected override void OnActivated(IActivatedEventArgs e)
        {
            var preActivationContent = Window.Current.Content;
            base.OnActivated(e);

            // Handle protocol activation (for Spotify auth callback)
            if (e.Kind == ActivationKind.Protocol)
            {
                var protocolArgs = e as ProtocolActivatedEventArgs;
                if (protocolArgs != null)
                {
                    // This URI will contain the authorization code from Spotify
                    var uri = protocolArgs.Uri;

                    if (preActivationContent == null && Window.Current != null)
                    {
                        // Display the initial content
                        var frame = (Frame)Window.Current.Content;
                        // Pass the URI to the MainPage
                        frame.Navigate(typeof(MainPage), uri.ToString());
                    }
                    else if (Window.Current.Content is Frame frame &&
                             frame.Content is MainPage page)
                    {
                        // If we already have the MainPage, just pass the URI to it
                        page.HandleProtocolActivation(uri);
                    }
                }
            }
            else
            {
                // Handle other activation types as before
                if (preActivationContent == null && Window.Current != null)
                {
                    // Display the initial content
                    var frame = (Frame)Window.Current.Content;
                    frame.Navigate(typeof(MainPage), null);
                }
            }

            // Ensure the window is activated
            Window.Current.Activate();
        }
    }
}