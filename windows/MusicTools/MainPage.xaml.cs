using MusicTools.NativeModules;
using System;
using System.Diagnostics;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using static LanguageExt.Prelude;
using MusicTools.Domain;

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
            if (e.Parameter is string uriString && uriString.HasValue())
                StoreAuthUri(uriString);            
        }

        /// <summary>
        /// Handles protocol activation from App.xaml.cs
        /// </summary>
        public void HandleProtocolActivation(Uri uri) 
        {
            if (uri != null)
                StoreAuthUri(uri.ToString());
        }

        /// <summary>
        /// Store the auth URI for React Native to access later
        /// </summary>
        private void StoreAuthUri(string uriString) =>
            Try(() => ApplicationData.Current.LocalSettings.Values[SpotifyModule.spotifyAuthUriKey] = uriString)
                .IfFail(ex => Debug.WriteLine($"Error storing URI: {ex.Message}"));      
    }
}