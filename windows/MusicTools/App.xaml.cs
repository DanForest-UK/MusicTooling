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
            // Dependency injection

            Runtime.Status = StatusModule.AddStatus;

            // Wire up the other status event handlers to use the same mechanism
            Runtime.Info = message => StatusHelper.Info(message);
            Runtime.Warning = message => StatusHelper.Warning(message);
            Runtime.Error = (message, ex) => StatusHelper.Error(message, ex);

            // Initialize file system access methods
            Runtime.GetFilesWithExtensionAsync = React.GetFilesWithExtensionAsync;
            Runtime.WithStream = React.WithStream;
            Runtime.ReadSongInfo = ReadTag.ReadSongInfo;

            // Initialize Spotify API
            Runtime.GetSpotifyAPI = (clientId, clientSecret, redirectUri) =>
                new SpotifyApi(clientId, clientSecret, redirectUri);

            PersistedStateService.Initialize();

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
                // Handle other activation types
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