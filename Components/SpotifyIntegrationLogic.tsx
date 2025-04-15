import { useState, useEffect, useCallback, useRef } from 'react';
import { Alert, Linking, EmitterSubscription, DeviceEventEmitter } from 'react-native';
import { NativeModules } from 'react-native';
import { AppModel } from '../types';
import {
    SpotifyError,
    SpotifyResponse,
    isAlreadyAuthenticatedError,
} from '../types';

const { SpotifyModule } = NativeModules;

// Event constants to match C# side
const SPOTIFY_OPERATION_PROGRESS = 'spotifyOperationProgress';
const SPOTIFY_OPERATION_COMPLETE = 'spotifyOperationComplete';
const SPOTIFY_OPERATION_ERROR = 'spotifyOperationError';

// Progress state interface
interface ProgressState {
    phase: 'idle' | 'initializing' | 'searching' | 'searchComplete' | 'liking' | 'complete';
    totalSongs: number;
    processed: number;
    message: string;
    isComplete: boolean;
    isError: boolean;
    isCancelled: boolean;
}

const defaultProgressState: ProgressState = {
    phase: 'idle',
    totalSongs: 0,
    processed: 0,
    message: '',
    isComplete: false,
    isError: false,
    isCancelled: false,
};

export interface SpotifyIntegrationProps {
    appState: AppModel;
    onClose: () => void;
    onSpotifyAction?: () => void;
}

export const useSpotifyIntegration = (appState: AppModel, onSpotifyAction?: () => void) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isAuthenticating, setIsAuthenticating] = useState(false);
    const [isProcessing, setIsProcessing] = useState(false);
    const [progress, setProgress] = useState<ProgressState>(defaultProgressState);
    const [errors, setErrors] = useState<SpotifyError[]>([]);
    const [operationRunning, setOperationRunning] = useState(false);

    // Ref to track if component is mounted
    const isMountedRef = useRef(true);

    // Keep a local copy of the last valid state
    const lastValidAppState = useRef<AppModel | null>(null);

    // Store event subscriptions
    const eventSubscriptions = useRef<EmitterSubscription[]>([]);

    // Update the ref whenever we get a valid appState
    useEffect(() => {
        if (appState &&
            appState.Songs &&
            typeof appState.Songs === 'object' &&
            Array.isArray(appState.ChosenSongs)) {
            lastValidAppState.current = appState;
        }
    }, [appState]);

    // Helper to clean up subscriptions
    const cleanupSubscriptions = useCallback(() => {
        if (eventSubscriptions.current.length > 0) {
            eventSubscriptions.current.forEach(sub => sub.remove());
            eventSubscriptions.current = [];
        }
    }, []);

    // Function to process complete event data
    const processCompleteEvent = useCallback((completeData: any) => {
        try {
            const data = typeof completeData === 'string' ? JSON.parse(completeData) : completeData;

            // Check for no items to process
            if (data.noSongsToProcess ||
                (data.hasOwnProperty('NoSongsToProcess') && data.NoSongsToProcess) ||
                data.noArtistsToProcess ||
                (data.hasOwnProperty('NoArtistsToProcess') && data.NoArtistsToProcess) ||
                (data.message && typeof data.message === 'string' &&
                    data.message.includes("No songs available to like"))) {

                // Clean up any event subscriptions
                cleanupSubscriptions();

                // Reset UI state immediately
                if (isMountedRef.current) {
                    setIsProcessing(false);
                    setOperationRunning(false);
                    Alert.alert('Nothing to Process', data.message || 'All items have already been processed.');
                }

                return;
            }

            // Clean up event subscriptions to prevent further events
            cleanupSubscriptions();

            // Only update state if component is still mounted
            if (isMountedRef.current) {
                setProgress(prev => ({
                    ...prev,
                    phase: 'complete',
                    message: data.message || 'Operation complete',
                    isComplete: true,
                    isError: false,
                    isCancelled: data.cancelled || false,
                }));

                // If there are errors, update the errors state
                if (data.errors && Array.isArray(data.errors)) {
                    setErrors(data.errors);
                }

                // Add a small delay before changing component state
                setTimeout(() => {
                    if (isMountedRef.current) {
                        setIsProcessing(false);
                        setOperationRunning(false);
                    }
                }, 100);

                // Call onSpotifyAction to update parent component
                if (onSpotifyAction) {
                    onSpotifyAction();
                }

                // Show appropriate message based on result
                if (data.cancelled) {
                    Alert.alert('Cancelled', 'Operation was cancelled');
                } else if (data.success) {
                    if (data.partialSuccess) {
                        Alert.alert('Partial Success',
                            'Operation completed with some errors.');
                    } else {
                        Alert.alert('Success', data.message || 'Operation completed successfully');
                    }
                } else {
                    Alert.alert('Operation Failed', data.message || 'Operation failed to complete');
                }
            }
        } catch (error) {
            // Ensure UI is reset even if there's an error
            if (isMountedRef.current) {
                setIsProcessing(false);
                setOperationRunning(false);
                Alert.alert('Error', 'An error occurred while processing the operation result');
            }
        }
    }, [cleanupSubscriptions, onSpotifyAction]);

    // Function to register event handlers
    const registerEventHandlers = useCallback(() => {
        // Remove any existing event listeners first
        cleanupSubscriptions();

        // Register fresh event listeners
        const progressSubscription = DeviceEventEmitter.addListener(
            SPOTIFY_OPERATION_PROGRESS,
            (progressData) => {
                const data = typeof progressData === 'string' ? JSON.parse(progressData) : progressData;

                // Only update state if component is still mounted
                if (isMountedRef.current) {
                    setProgress(prev => ({
                        ...prev,
                        phase: data.phase || prev.phase,
                        totalSongs: data.totalSongs || prev.totalSongs,
                        processed: data.processed || prev.processed,
                        message: data.message || prev.message,
                        isComplete: false,
                        isError: false,
                        isCancelled: false,
                    }));
                }
            }
        );

        // Only use one event listener for the complete event
        const completeSubscription = DeviceEventEmitter.addListener(
            SPOTIFY_OPERATION_COMPLETE,
            (completeData) => {
                processCompleteEvent(completeData);
            }
        );

        const errorSubscription = DeviceEventEmitter.addListener(
            SPOTIFY_OPERATION_ERROR,
            (errorData) => {
                const data = typeof errorData === 'string' ? JSON.parse(errorData) : errorData;

                // Clean up event subscriptions immediately
                cleanupSubscriptions();

                // Only update state if component is still mounted
                if (isMountedRef.current) {
                    setProgress(prev => ({
                        ...prev,
                        phase: 'complete',
                        message: data.error || 'Operation failed',
                        isComplete: true,
                        isError: true,
                        isCancelled: false,
                    }));

                    // Add a small delay before changing component state
                    setTimeout(() => {
                        if (isMountedRef.current) {
                            setIsProcessing(false);
                            setOperationRunning(false);
                        }
                    }, 100);

                    Alert.alert('Error', data.error || 'An unknown error occurred');
                }
            }
        );

        // Store the subscriptions for cleanup
        eventSubscriptions.current = [
            progressSubscription,
            completeSubscription,
            errorSubscription,
        ];
    }, [cleanupSubscriptions, processCompleteEvent]);

    // Function to handle the auth code exchange
    const completeAuthentication = useCallback(async (code: string) => {
        try {
            // Prevent duplicate code usage
            if (code === lastUsedCode.current) {
                return;
            }

            // Prevent concurrent code exchange attempts
            if (codeExchangeInProgress.current) {
                return;
            }

            if (!code || code.length < 5) {
                setIsAuthenticating(false);
                Alert.alert('Error', 'Invalid authorization code received');
                return;
            }

            // Set authenticating state and mark exchange as in progress
            setIsAuthenticating(true);
            codeExchangeInProgress.current = true;
            lastUsedCode.current = code;

            // Exchange code for token
            const result = await SpotifyModule.ExchangeCodeForToken(code);

            // Clear in-progress flag
            codeExchangeInProgress.current = false;

            // Clear the stored URI immediately to prevent reuse
            await SpotifyModule.ClearStoredAuthUri();

            // Parse response
            const response = JSON.parse(result) as SpotifyResponse;
            setIsAuthenticating(false);

            if (response.success) {
                setIsAuthenticated(true);
            } else {
                if (response.error) {
                    if (isAlreadyAuthenticatedError(response.error)) {
                        // User is already authenticated
                        setIsAuthenticated(true);
                    } else {
                        Alert.alert('Error', `Token exchange failed: ${response.error.Message}`);
                    }
                } else {
                    Alert.alert('Error', 'Token exchange failed: Unknown error');
                }
            }
        } catch (error) {
            setIsAuthenticating(false);
            codeExchangeInProgress.current = false;
            Alert.alert('Error', `Token exchange failed: ${error}`);
        }
    }, []);

    // Add a ref to track if code exchange is in progress
    const codeExchangeInProgress = useRef(false);
    // Add a ref to store the last used code to prevent duplicate usage
    const lastUsedCode = useRef<string | null>(null);

    // Handle authentication error
    const handleAuthError = useCallback((error: string) => {
        setIsAuthenticating(false);
        Alert.alert('Authentication Error', `Spotify returned an error: ${error}`);
    }, []);

    // Function to handle the auth callback URL
    const handleAuthCallbackUrl = useCallback((url: string) => {
        if (url.startsWith('musictools://auth/callback')) {
            try {
                // Extract the code parameter using regex for more reliable parsing
                const codeMatch = url.match(/[?&]code=([^&]+)/);
                if (codeMatch && codeMatch[1]) {
                    // Get the code - IMPORTANT: Do not decode it here
                    const code = codeMatch[1];

                    // Call authenticate immediately with the raw code
                    completeAuthentication(code);
                } else {
                    // Extract error parameter using regex
                    const errorMatch = url.match(/[?&]error=([^&]+)/);
                    if (errorMatch && errorMatch[1]) {
                        const error = decodeURIComponent(errorMatch[1]);
                        handleAuthError(error);
                    } else {
                        setIsAuthenticating(false);
                        Alert.alert('Error', 'No authorization code found in the callback URL');
                    }
                }
            } catch (e) {
                setIsAuthenticating(false);
                Alert.alert('Error', `Failed to parse the callback URL: ${e}`);
            }
        }
    }, [completeAuthentication, handleAuthError]);

    // Check for stored auth URI (fallback mechanism)
    const checkStoredUri = useCallback(async () => {
        try {
            // Don't check for stored URI if authentication is already in progress
            if (codeExchangeInProgress.current) {
                return;
            }

            const storedUri = await SpotifyModule.GetStoredAuthUri();
            if (storedUri) {
                handleAuthCallbackUrl(storedUri);
            }
        } catch (error) {
            // Silently fail
        }
    }, [handleAuthCallbackUrl]);

    // Clean up when component unmounts
    useEffect(() => {
        // Set mounted ref to true on initialization
        isMountedRef.current = true;

        // Register initial event handlers
        registerEventHandlers();

        // Cleanup function
        return () => {
            // Mark component as unmounted
            isMountedRef.current = false;

            // Clean up event subscriptions
            cleanupSubscriptions();
        };
    }, [registerEventHandlers, cleanupSubscriptions]);

    // Handle URL events (from deep linking)
    useEffect(() => {
        // Event listener for URL events
        const handleUrl = (event: { url: string }) => {
            handleAuthCallbackUrl(event.url);
        };

        // Variable to hold the subscription
        let subscription: EmitterSubscription | null = null;

        // Add event listener
        try {
            subscription = Linking.addListener('url', handleUrl);
        } catch (e) {
            // Silently fail
        }

        // Check if app was opened via URL
        Linking.getInitialURL().then(initialUrl => {
            if (initialUrl) {
                handleAuthCallbackUrl(initialUrl);
            }
        });

        // Check for stored URI
        checkStoredUri();

        // Initial auth status check
        const checkAuth = async () => {
            try {
                const result: boolean = await SpotifyModule.CheckAuthStatus();
                if (result) {
                    setIsAuthenticated(true);
                }
            } catch (error) {
                // Silently fail
            }
        };

        checkAuth();

        // Cleanup
        return () => {
            if (subscription) {
                subscription.remove();
            }
        };
    }, [handleAuthCallbackUrl, checkStoredUri]);

    const handleAuthenticate = async () => {
        try {
            setIsAuthenticating(true);
            setErrors([]);

            // Reset last used code when starting a new auth flow
            lastUsedCode.current = null;

            // Get the auth URL from SpotifyModule
            const authUrl = await SpotifyModule.GetAuthUrl();

            // Open the auth URL in browser
            const supported = await Linking.canOpenURL(authUrl);
            if (!supported) {
                Alert.alert('Error', 'Cannot open Spotify authentication URL');
                setIsAuthenticating(false);
                return;
            }

            // Open the URL - the callback will be handled by our URL event listener
            await Linking.openURL(authUrl);

            // Set a timeout to clear the authenticating state if no callback is received
            setTimeout(() => {
                setIsAuthenticating(prevState => {
                    if (prevState) {
                        Alert.alert('Authentication Timeout',
                            'Did not receive a response from Spotify. Please try again.');
                        codeExchangeInProgress.current = false;
                        return false;
                    }
                    return prevState;
                });
            }, 60000); // 1 minute timeout

        } catch (error) {
            setIsAuthenticating(false);
            Alert.alert('Error', `Failed to start authentication: ${error}`);
        }
    };

    // Handle liking songs
    const handleLikeSongs = () => {
        if (!isAuthenticated) {
            Alert.alert('Error', 'Please authenticate with Spotify first');
            return;
        }

        // Don't start a new operation if one is already running
        if (operationRunning) {
            Alert.alert('Operation in Progress', 'A Spotify operation is already running.');
            return;
        }

        try {
            // Register event handlers first to ensure they're ready
            registerEventHandlers();

            // Reset progress state
            setProgress(defaultProgressState);
            setErrors([]);
            setIsProcessing(true);
            setOperationRunning(true);

            // Call the method but don't await - will get updates via events
            SpotifyModule.LikeSongs();
        } catch (error) {
            if (isMountedRef.current) {
                setIsProcessing(false);
                setOperationRunning(false);
                Alert.alert('Error', `Failed to start operation: ${error}`);
            }
        }
    };

    // Cancel the current operation
    const cancelOperation = () => {
        if (operationRunning) {
            try {
                SpotifyModule.CancelSpotifyOperation();
            } catch (error) {
                // Silently fail
            }
        }
    };

    // Handle following artists
    const handleFollowArtists = async () => {
        if (!isAuthenticated) {
            Alert.alert('Error', 'Please authenticate with Spotify first');
            return;
        }

        // Register event handlers first
        registerEventHandlers();

        setIsProcessing(true);
        setErrors([]);

        try {
            const result = await SpotifyModule.FollowArtists();
            const response = JSON.parse(result) as SpotifyResponse;

            // Check for the special "no artists to process" flag with safe property access
            if (response.noArtistsToProcess ||
                (response.hasOwnProperty('NoArtistsToProcess') && (response as any).NoArtistsToProcess)) {
                // Access the message correctly
                Alert.alert(
                    'Nothing to Process',
                    response.message || 'All artists have already been processed.'
                );
                setIsProcessing(false);
                return;
            }

            // Notify parent component to show Spotify status
            if (onSpotifyAction) {
                onSpotifyAction();
            }

            if (response.success) {
                Alert.alert('Success', 'Artists have been followed on Spotify!');
            } else if (response.partialSuccess) {
                const errorsArray = response.errors || [];
                setErrors(errorsArray);
                Alert.alert('Partial Success', 'Some artists were followed, but there were errors. See details below.');
            } else {
                const errorsArray = response.errors || [];
                setErrors(errorsArray);
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
        progress,
        errors,
        operationRunning,
        handleAuthenticate,
        handleLikeSongs,
        handleFollowArtists,
        cancelOperation,
    };
};