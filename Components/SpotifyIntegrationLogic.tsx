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

    // Function to handle the auth code exchange - defined with useCallback
    const completeAuthentication = useCallback(async (code: string) => {
        try {
            setIsAuthenticating(true);
            const result = await SpotifyModule.ExchangeCodeForToken(code);
            const response = JSON.parse(result);

            setIsAuthenticating(false);

            if (response.success) {
                setIsAuthenticated(true);
                Alert.alert('Success', 'Spotify authentication successful!');
            } else {
                Alert.alert('Error', `Token exchange failed: ${response.error?.message || 'Unknown error'}`);
            }
        } catch (error) {
            console.error('Token exchange error:', error);
            setIsAuthenticating(false);
            Alert.alert('Error', `Token exchange failed: ${error}`);
        }
    }, []);

    // Handle authentication error - defined with useCallback
    const handleAuthError = useCallback((error: string) => {
        setIsAuthenticating(false);
        Alert.alert('Authentication Error', `Spotify returned an error: ${error}`);
    }, []);

    // Function to handle the auth callback URL - defined with useCallback
    const handleAuthCallbackUrl = useCallback((url: string) => {
        console.log('Auth callback URL received:', url);

        if (url.startsWith('musictools://auth/callback')) {
            try {
                // Try to extract the code parameter using regex
                const codeMatch = url.match(/code=([^&]+)/);
                if (codeMatch && codeMatch[1]) {
                    const code = codeMatch[1];
                    completeAuthentication(code);
                } else {
                    // Try to extract error parameter using regex
                    const errorMatch = url.match(/error=([^&]+)/);
                    if (errorMatch && errorMatch[1]) {
                        const error = errorMatch[1];
                        handleAuthError(error);
                    } else {
                        console.log('No code or error found in redirect URL');
                    }
                }
            } catch (e) {
                console.error('Error parsing callback URL:', e);
            }
        }
    }, [completeAuthentication, handleAuthError]);

    // Check for stored auth URI (fallback mechanism for if eg the app is in the background)
    const checkStoredUri = useCallback(async () => {
        try {
            const storedUri = await SpotifyModule.GetStoredAuthUri();
            if (storedUri) {
                console.log('Found stored auth URI:', storedUri);
                handleAuthCallbackUrl(storedUri);
                SpotifyModule.ClearStoredAuthUri();
            }
        } catch (error) {
            console.log('Error checking stored auth URI:', error);
        }
    }, [handleAuthCallbackUrl]);

    // Handle URL events (from deep linking)
    useEffect(() => {
        // Event listener for URL events
        const handleUrl = (event: { url: string }) => {
            handleAuthCallbackUrl(event.url);
        };

        // Variable to hold the subscription
        let subscription: EmitterSubscription | null = null;

        // Add event listener, using a try-catch to handle different API versions
        try {
            // Try with addListener first, alternatives attempted below the catch block
                subscription = Linking.addListener('url', handleUrl);
        } catch (e) {
            console.log('Error setting up link listener, trying alternative:', e);
        }

        // Check if app was opened via URL
        Linking.getInitialURL().then(initialUrl => {
            if (initialUrl) {
                handleAuthCallbackUrl(initialUrl);
            }
        });

        // Check for stored URI immediately and after a delay
        checkStoredUri();
        const timeoutId = setTimeout(checkStoredUri, 2000);

        // Initial auth status check
        const checkAuth = async () => {
            try {
                const result: boolean = await SpotifyModule.CheckAuthStatus();
                if (result) {
                    setIsAuthenticated(true);
                }
            } catch (error) {
                console.log('Initial auth check failed:', error);
            }
        };

        checkAuth();

        // Cleanup
        return () => {
            // Clean up subscription if it exists
            if (subscription) {
                subscription.remove();
            }

            clearTimeout(timeoutId);
        };
    }, [handleAuthCallbackUrl, checkStoredUri]);

    const handleAuthenticate = async () => {
        try {
            setIsAuthenticating(true);
            setErrors([]);

            // Get the auth URL
            const authUrl = await SpotifyModule.GetAuthUrl();
            console.log('Got auth URL:', authUrl);

            // Open the auth URL in browser
            const supported = await Linking.canOpenURL(authUrl);
            if (!supported) {
                Alert.alert('Error', 'Cannot open Spotify authentication URL');
                setIsAuthenticating(false);
                return;
            }

            // Open the URL - the callback will be handled by our URL event listener
            await Linking.openURL(authUrl);

            // We'll set a timeout to clear the authenticating state if no callback is received
            setTimeout(() => {
                setIsAuthenticating(prevState => {
                    if (prevState) {
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
            // Filter the songs array to only include chosen songs
            const chosenSongObjects = appState.songs.filter(song =>
                appState.chosenSongs.includes(song.id)
            );

            const result = await SpotifyModule.LikeSongs(JSON.stringify(chosenSongObjects));
            const response = JSON.parse(result);

            if (response.success) {
                Alert.alert('Success', 'Songs have been liked on Spotify!');
            } else if (response.partialSuccess) {
                setErrors(response.errors || []);
                Alert.alert('Partial Success', 'Some songs were liked, but there were errors. See details below.');
            } else {
                setErrors(response.errors || []);
                Alert.alert('Error', 'Failed to like songs on Spotify. See details below.');
            }
        } catch (error) {
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
            // Filter the songs array to only include chosen songs
            const chosenSongObjects = appState.songs.filter(song =>
                appState.chosenSongs.includes(song.id)
            );

            const result = await SpotifyModule.FollowArtists(JSON.stringify(chosenSongObjects));
            const response = JSON.parse(result);

            if (response.success) {
                Alert.alert('Success', 'Artists have been followed on Spotify!');
            } else if (response.partialSuccess) {
                setErrors(response.errors || []);
                Alert.alert('Partial Success', 'Some artists were followed, but there were errors. See details below.');
            } else {
                setErrors(response.errors || []);
                Alert.alert('Error', 'Failed to follow artists on Spotify. See details below.');
            }
        } catch (error) {
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