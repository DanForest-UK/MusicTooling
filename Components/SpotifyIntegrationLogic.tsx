import { useState, useEffect, useCallback } from 'react';
import { Alert, Linking, EmitterSubscription } from 'react-native';
import { NativeModules } from 'react-native';
import { AppModel } from '../types';

const { SpotifyModule } = NativeModules;

export interface SpotifyError {
    errorCode: string;
    message: string;
    resourceId: string;
}

export interface SpotifyIntegrationProps {
    appState: AppModel;
    onClose: () => void;
}

export const useSpotifyIntegration = (appState: AppModel) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isAuthenticating, setIsAuthenticating] = useState(false);
    const [isProcessing, setIsProcessing] = useState(false);
    const [errors, setErrors] = useState<SpotifyError[]>([]);

    // Function to handle the auth code exchange - with improved error handling
    const completeAuthentication = useCallback(async (code: string) => {
        try {
            if (!code || code.length < 5) {
                console.error("Invalid authorization code received");
                setIsAuthenticating(false);
                Alert.alert('Error', 'Invalid authorization code received');
                return;
            }

            console.log(`Auth code received (first 4 chars): ${code.substring(0, 4)}..., length: ${code.length}`);

            // Set authenticating state
            setIsAuthenticating(true);

            // Exchange code for token IMMEDIATELY
            console.log("Exchanging code for token...");
            const result = await SpotifyModule.ExchangeCodeForToken(code);
            console.log("Code exchange completed, parsing result");

            // Parse response
            const response = JSON.parse(result);
            setIsAuthenticating(false);

            if (response.success) {
                console.log("Authentication successful!");
                setIsAuthenticated(true);
                Alert.alert('Success', 'Spotify authentication successful!');
            } else {
                console.error("Token exchange failed:", response.error?.message || 'Unknown error');
                Alert.alert('Error', `Token exchange failed: ${response.error?.message || 'Unknown error'}`);
            }
        } catch (error) {
            console.error('Token exchange error:', error);
            setIsAuthenticating(false);
            Alert.alert('Error', `Token exchange failed: ${error}`);
        }
    }, []);

    // Handle authentication error
    const handleAuthError = useCallback((error: string) => {
        console.error("Auth error received:", error);
        setIsAuthenticating(false);
        Alert.alert('Authentication Error', `Spotify returned an error: ${error}`);
    }, []);

    // Function to handle the auth callback URL - improved parsing
    const handleAuthCallbackUrl = useCallback((url: string) => {
        console.log('Auth callback URL received:', url);

        if (url.startsWith('musictools://auth/callback')) {
            try {
                // Extract the code parameter using regex
                const codeMatch = url.match(/[?&]code=([^&]+)/);
                if (codeMatch && codeMatch[1]) {
                    // Get the code and make sure it's properly decoded
                    const code = decodeURIComponent(codeMatch[1]);
                    console.log(`Extracted code (length: ${code.length})`);

                    // Call authenticate immediately
                    completeAuthentication(code);
                } else {
                    // Extract error parameter using regex
                    const errorMatch = url.match(/[?&]error=([^&]+)/);
                    if (errorMatch && errorMatch[1]) {
                        const error = decodeURIComponent(errorMatch[1]);
                        console.log(`Extracted error: ${error}`);
                        handleAuthError(error);
                    } else {
                        console.log('No code or error found in redirect URL');
                        setIsAuthenticating(false);
                        Alert.alert('Error', 'No authorization code found in the callback URL');
                    }
                }
            } catch (e) {
                console.error('Error parsing callback URL:', e);
                setIsAuthenticating(false);
                Alert.alert('Error', `Failed to parse the callback URL: ${e}`);
            }
        } else {
            console.log('Received URL does not match expected callback URL format');
        }
    }, [completeAuthentication, handleAuthError]);

    // Check for stored auth URI (fallback mechanism)
    const checkStoredUri = useCallback(async () => {
        try {
            console.log("Checking for stored auth URI...");
            const storedUri = await SpotifyModule.GetStoredAuthUri();
            if (storedUri) {
                console.log('Found stored auth URI, processing...');
                handleAuthCallbackUrl(storedUri);
                // Clear the stored URI to prevent reuse
                console.log("Clearing stored auth URI");
                SpotifyModule.ClearStoredAuthUri();
            } else {
                console.log("No stored auth URI found");
            }
        } catch (error) {
            console.log('Error checking stored auth URI:', error);
        }
    }, [handleAuthCallbackUrl]);

    // Handle URL events (from deep linking)
    useEffect(() => {
        console.log("Setting up URL handlers for Spotify auth...");

        // Event listener for URL events
        const handleUrl = (event: { url: string }) => {
            console.log("URL event received:", event.url);
            handleAuthCallbackUrl(event.url);
        };

        // Variable to hold the subscription
        let subscription: EmitterSubscription | null = null;

        // Add event listener
        try {
            console.log("Adding URL event listener");
            subscription = Linking.addListener('url', handleUrl);
        } catch (e) {
            console.log('Error setting up link listener:', e);
        }

        // Check if app was opened via URL
        console.log("Checking for initial URL");
        Linking.getInitialURL().then(initialUrl => {
            if (initialUrl) {
                console.log("App was opened via URL:", initialUrl);
                handleAuthCallbackUrl(initialUrl);
            } else {
                console.log("No initial URL found");
            }
        });

        // Check for stored URI immediately and after a delay
        checkStoredUri();
        console.log("Setting timeout for second stored URI check");
        const timeoutId = setTimeout(checkStoredUri, 2000);

        // Initial auth status check
        const checkAuth = async () => {
            try {
                console.log("Checking initial auth status");
                const result: boolean = await SpotifyModule.CheckAuthStatus();
                if (result) {
                    console.log("User is already authenticated");
                    setIsAuthenticated(true);
                } else {
                    console.log("User is not authenticated");
                }
            } catch (error) {
                console.log('Initial auth check failed:', error);
            }
        };

        checkAuth();

        // Cleanup
        return () => {
            console.log("Cleaning up URL handlers");
            if (subscription) {
                subscription.remove();
            }
            clearTimeout(timeoutId);
        };
    }, [handleAuthCallbackUrl, checkStoredUri]);

    const handleAuthenticate = async () => {
        try {
            console.log("Starting Spotify authentication process");
            setIsAuthenticating(true);
            setErrors([]);

            // Get the auth URL from SpotifyModule
            console.log("Requesting auth URL from native module");
            const authUrl = await SpotifyModule.GetAuthUrl();
            console.log('Got auth URL, preparing to open browser');

            // Open the auth URL in browser
            const supported = await Linking.canOpenURL(authUrl);
            if (!supported) {
                console.error("Cannot open Spotify authentication URL");
                Alert.alert('Error', 'Cannot open Spotify authentication URL');
                setIsAuthenticating(false);
                return;
            }

            // Open the URL - the callback will be handled by our URL event listener
            console.log("Opening auth URL in browser");
            await Linking.openURL(authUrl);

            // Set a timeout to clear the authenticating state if no callback is received
            console.log("Setting authentication timeout");
            setTimeout(() => {
                setIsAuthenticating(prevState => {
                    if (prevState) {
                        console.log("Authentication timeout occurred");
                        Alert.alert('Authentication Timeout',
                            'Did not receive a response from Spotify. Please try again.');
                        return false;
                    }
                    return prevState;
                });
            }, 60000); // 1 minute timeout

        } catch (error) {
            console.error('Authentication error:', error);
            setIsAuthenticating(false);
            Alert.alert('Error', `Failed to start authentication: ${error}`);
        }
    };

    const handleLikeSongs = async () => {
        if (!isAuthenticated) {
            Alert.alert('Error', 'Please authenticate with Spotify first');
            return;
        }

        if (appState.chosenSongs.length === 0) {
            Alert.alert('No Songs Selected', 'Please select songs to like on Spotify');
            return;
        }

        setIsProcessing(true);
        setErrors([]);

        try {
            console.log(`Liking ${appState.chosenSongs.length} songs on Spotify`);

            // Filter the songs array to only include chosen songs
            const chosenSongObjects = appState.songs.filter(song =>
                appState.chosenSongs.includes(song.id)
            );

            const result = await SpotifyModule.LikeSongs(JSON.stringify(chosenSongObjects));
            console.log("Like songs request completed, parsing response");
            const response = JSON.parse(result);

            if (response.success) {
                console.log("All songs liked successfully");
                Alert.alert('Success', 'Songs have been liked on Spotify!');
            } else if (response.partialSuccess) {
                console.log(`Partial success: ${response.errors?.length || 0} errors`);
                setErrors(response.errors || []);
                Alert.alert('Partial Success', 'Some songs were liked, but there were errors. See details below.');
            } else {
                console.error("Failed to like songs:", response.errors);
                setErrors(response.errors || []);
                Alert.alert('Error', 'Failed to like songs on Spotify. See details below.');
            }
        } catch (error) {
            console.error("Error liking songs:", error);
            Alert.alert('Error', `An unexpected error occurred: ${error}`);
        } finally {
            setIsProcessing(false);
        }
    };

    const handleFollowArtists = async () => {
        if (!isAuthenticated) {
            Alert.alert('Error', 'Please authenticate with Spotify first');
            return;
        }

        if (appState.chosenSongs.length === 0) {
            Alert.alert('No Songs Selected', 'Please select songs whose artists you want to follow');
            return;
        }

        setIsProcessing(true);
        setErrors([]);

        try {
            console.log(`Following artists from ${appState.chosenSongs.length} songs`);

            // Filter the songs array to only include chosen songs
            const chosenSongObjects = appState.songs.filter(song =>
                appState.chosenSongs.includes(song.id)
            );

            const result = await SpotifyModule.FollowArtists(JSON.stringify(chosenSongObjects));
            console.log("Follow artists request completed, parsing response");
            const response = JSON.parse(result);

            if (response.success) {
                console.log("All artists followed successfully");
                Alert.alert('Success', 'Artists have been followed on Spotify!');
            } else if (response.partialSuccess) {
                console.log(`Partial success: ${response.errors?.length || 0} errors`);
                setErrors(response.errors || []);
                Alert.alert('Partial Success', 'Some artists were followed, but there were errors. See details below.');
            } else {
                console.error("Failed to follow artists:", response.errors);
                setErrors(response.errors || []);
                Alert.alert('Error', 'Failed to follow artists on Spotify. See details below.');
            }
        } catch (error) {
            console.error("Error following artists:", error);
            Alert.alert('Error', `An unexpected error occurred: ${error}`);
        } finally {
            setIsProcessing(false);
        }
    };

    return {
        isAuthenticated,
        isAuthenticating,
        isProcessing,
        errors,
        handleAuthenticate,
        handleLikeSongs,
        handleFollowArtists
    };
};