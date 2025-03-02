import { useState, useEffect } from 'react';
import { Alert, Linking } from 'react-native';
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

    // Initial check on component mount
    useEffect(() => {
        const checkAuth = async () => {
            try {
                const result = await SpotifyModule.CheckAuthStatus();
                const response = JSON.parse(result);
                if (response.isAuthenticated) {
                    setIsAuthenticated(true);
                }
            } catch (error: unknown) {
                // Silent fail on initial check
                console.log('Initial auth check failed:', error);
            }
        };

        checkAuth();

        // Cleanup on unmount
        return () => {
            try {
                SpotifyModule.StopAuthListener().catch((error: Error) => {
                    console.error('Error stopping auth listener:', error);
                });
            } catch (error: unknown) {
                console.error('Error stopping auth listener:', error);
            }
        };
    }, []);

    const handleAuthenticate = async () => {
        try {
            setIsAuthenticating(true);
            setErrors([]);

            // Step 1: Get the auth URL
            try {
                const authUrl = await SpotifyModule.GetAuthUrl();
                console.log('Got auth URL:', authUrl);

                // Step 2: Start the auth listener
                try {
                    const startResult = await SpotifyModule.StartAuthListener();
                    console.log('StartAuthListener result:', startResult);

                    // Step 3: Open the auth URL in the browser
                    const supported = await Linking.canOpenURL(authUrl);
                    if (!supported) {
                        Alert.alert('Error', 'Cannot open Spotify authentication URL');
                        setIsAuthenticating(false);
                        return;
                    }

                    // Open the URL
                    await Linking.openURL(authUrl);

                    // Step 4: Wait for authentication
                    try {
                        const result = await SpotifyModule.WaitForAuthentication();
                        const response = JSON.parse(result);

                        setIsAuthenticating(false);

                        if (response.success) {
                            setIsAuthenticated(true);
                            Alert.alert('Success', 'Spotify authentication successful!');
                        } else {
                            Alert.alert('Error', `Authentication failed: ${response.error?.message || 'Unknown error'}`);
                        }
                    } catch (error: unknown) {
                        console.error('Wait for auth error:', error);
                        setIsAuthenticating(false);
                        Alert.alert('Error', `Authentication failed: ${error}`);
                    }
                } catch (error: unknown) {
                    console.error('Start auth listener error:', error);
                    Alert.alert('Error', `Failed to start authentication listener: ${error}`);
                    setIsAuthenticating(false);
                }
            } catch (error: unknown) {
                console.error('Get auth URL error:', error);
                Alert.alert('Error', `Failed to get authentication URL: ${error}`);
                setIsAuthenticating(false);
            }
        } catch (error: unknown) {
            console.error('General auth error:', error);
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
        } catch (error: unknown) {
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
        } catch (error: unknown) {
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