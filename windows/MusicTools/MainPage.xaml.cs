using Microsoft.ReactNative;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MusicTools
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            var app = Application.Current as App;
            reactRootView.ReactNativeHost = app.Host;
        }

        /// <summary>
        /// Handle URI navigation parameter when page is first navigated to
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // If we have a URI string from protocol activation, store it
            if (e.Parameter is string uriString && !string.IsNullOrEmpty(uriString))
            {
                StoreAuthUri(uriString);
            }
        }

        /// <summary>
        /// Handles protocol activation from App.xaml.cs
        /// </summary>
        public void HandleProtocolActivation(Uri uri)
        {
            if (uri != null)
            {
                StoreAuthUri(uri.ToString());
            }
        }

        /// <summary>
        /// Store the auth URI for React Native to access later
        /// </summary>
        private void StoreAuthUri(string uriString)
        {
            try
            {
                // For debugging
                System.Diagnostics.Debug.WriteLine($"Received URI: {uriString}");

                // Store in application settings
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["spotifyAuthUri"] = uriString;

                System.Diagnostics.Debug.WriteLine("URI stored in application settings");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error storing URI: {ex.Message}");
            }
        }
    }
}