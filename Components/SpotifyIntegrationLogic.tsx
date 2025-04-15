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

// New event constants to match C# side
const SPOTIFY_OPERATION_PROGRESS = 'spotifyOperationProgress';
const SPOTIFY_OPERATION_COMPLETE = 'spotifyOperationComplete';
const SPOTIFY_OPERATION_ERROR = 'spotifyOperationError';

// Simplified Progress state interface
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
        // More precise checking for valid appState structure
        if (appState &&
            appState.Songs &&
            typeof appState.Songs === 'object' &&
            Array.isArray(appState.ChosenSongs)) {
            console.log('Storing valid app state as fallback');
            lastValidAppState.current = appState;
        }
    }, [appState]);

    // Helper to clean up subscriptions
    const cleanupSubscriptions = useCallback(() => {
        console.log('Cleaning up event subscriptions');
        if (eventSubscriptions.current.length > 0) {
            eventSubscriptions.current.forEach(sub => sub.remove());
            eventSubscriptions.current = [];
        }
    }, []);

    // Function to process complete event data
    const processCompleteEvent = useCallback((completeData: any) => {
        console.log('Test banana'); // Debug log to see if this function runs at all
        console.log('Processing complete event data:', completeData);

        try {
            const data = typeof completeData === 'string' ? JSON.parse(completeData) : completeData;

            // Add these debug logs to check the data structure and properties
            console.log('Operation complete data:', JSON.stringify(data, null, 2));
            console.log('Property exists check:',
                'noSongsToProcess' in data,
                'NoSongsToProcess' in data,
                'noArtistsToProcess' in data,
                'NoArtistsToProcess' in data);
            console.log('Property value check:',
                data.noSongsToProcess,
                data.NoSongsToProcess,
                data.noArtistsToProcess,
                data.NoArtistsToProcess);

            // More permissive check for no items to process
            if (data.noSongsToProcess ||
                (data.hasOwnProperty('NoSongsToProcess') && data.NoSongsToProcess) ||
                data.noArtistsToProcess ||
                (data.hasOwnProperty('NoArtistsToProcess') && data.NoArtistsToProcess) ||
                (data.message && typeof data.message === 'string' &&
                    data.message.includes("No songs available to like"))) {

                console.log('No items to process detected from backend');

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
            console.error('Error processing complete event:', error);

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

        console.log('Registering fresh event handlers');

        // Register fresh event listeners
        const progressSubscription = DeviceEventEmitter.addListener(
            SPOTIFY_OPERATION_PROGRESS,
            (progressData) => {
                console.log('PROGRESS EVENT RECEIVED:', progressData);
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

        // Try both with constant and direct string
        const completeSubscription = DeviceEventEmitter.addListener(
            SPOTIFY_OPERATION_COMPLETE,
            (completeData) => {
                console.log('COMPLETE EVENT TRIGGERED via constant:', completeData);

                // Handle the complete event
                processCompleteEvent(completeData);
            }
        );

        const errorSubscription = DeviceEventEmitter.addListener(
            SPOTIFY_OPERATION_ERROR,
            (errorData) => {
                console.log('ERROR EVENT RECEIVED:', errorData);
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

        console.log('Event handlers registered successfully');
    }, [cleanupSubscriptions, processCompleteEvent]);

    // Function to handle the auth code exchange
    const completeAuthentication = useCallback(async (code: string) => {
        try {
            // Prevent duplicate code usage
            if (code === lastUsedCode.current) {
                console.log('Skipping duplicate code usage');
                return;
            }

            // Prevent concurrent code exchange attempts
            if (codeExchangeInProgress.current) {
                console.log('Code exchange already in progress');
                return;
            }

            if (!code || code.length < 5) {
                console.error('Invalid authorization code received');
                setIsAuthenticating(false);
                Alert.alert('Error', 'Invalid authorization code received');
                return;
            }

            console.log(`Auth code received (first 4 chars): ${code.substring(0, 4)}..., length: ${code.length}`);

            // Set authenticating state and mark exchange as in progress
            setIsAuthenticating(true);
            codeExchangeInProgress.current = true;
            lastUsedCode.current = code;

            // Exchange code for token IMMEDIATELY - pass the raw code without any modification
            console.log('Exchanging code for token...');
            const result = await SpotifyModule.ExchangeCodeForToken(code);
            console.log('Code exchange completed, parsing result');

            // Clear in-progress flag
            codeExchangeInProgress.current = false;

            // Clear the stored URI immediately to prevent reuse
            await SpotifyModule.ClearStoredAuthUri();

            // Parse response
            const response = JSON.parse(result) as SpotifyResponse;
            console.log('Parsed response:', response);
            setIsAuthenticating(false);

            if (response.success) {
                console.log('Authentication successful!');
                setIsAuthenticated(true);
            } else {

                if (response.error) {
                    if (isAlreadyAuthenticatedError(response.error)) {
                        // User is already authenticated, shouldn't happen but not a real issue
                        // so no need to alert the user
                        setIsAuthenticated(true);
                    } else {
                        console.error('Token exchange failed:', response.error.Message);
                        Alert.alert('Error', `Token exchange failed: ${response.error.Message}`);
                    }
                } else {
                    console.error('Token exchange failed: Unknown error');
                    Alert.alert('Error', 'Token exchange failed: Unknown error');
                }
            }
        } catch (error) {
            console.error('Token exchange error:', error);
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
        console.error('Auth error received:', error);
        setIsAuthenticating(false);
        Alert.alert('Authentication Error', `Spotify returned an error: ${error}`);
    }, []);

    // Function to handle the auth callback URL - improved parsing
    const handleAuthCallbackUrl = useCallback((url: string) => {
        console.log('Auth callback URL received:', url);

        if (url.startsWith('musictools://auth/callback')) {
            try {
                // Extract the code parameter using regex for more reliable parsing
                const codeMatch = url.match(/[?&]code=([^&]+)/);
                if (codeMatch && codeMatch[1]) {
                    // Get the code - IMPORTANT: Do not decode it here
                    // The authorization code from Spotify should be used as-is
                    const code = codeMatch[1];
                    console.log(`Extracted code (length: ${code.length})`);

                    // Call authenticate immediately with the raw code
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
            // Don't check for stored URI if authentication is already in progress
            if (codeExchangeInProgress.current) {
                console.log('Skipping stored URI check - code exchange in progress');
                return;
            }

            console.log('Checking for stored auth URI...');
            const storedUri = await SpotifyModule.GetStoredAuthUri();
            if (storedUri) {
                console.log('Found stored auth URI, processing...');
                handleAuthCallbackUrl(storedUri);
            } else {
                console.log('No stored auth URI found');
            }
        } catch (error) {
            console.log('Error checking stored auth URI:', error);
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
            console.log('Component unmounting, cleaning up resources');
            // Mark component as unmounted
            isMountedRef.current = false;

            // Clean up event subscriptions
            cleanupSubscriptions();
        };
    }, [registerEventHandlers, cleanupSubscriptions]);

    // Handle URL events (from deep linking)
    useEffect(() => {
        console.log('Setting up URL handlers for Spotify auth...');

        // Event listener for URL events
        const handleUrl = (event: { url: string }) => {
            console.log('URL event received:', event.url);
            handleAuthCallbackUrl(event.url);
        };

        // Variable to hold the subscription
        let subscription: EmitterSubscription | null = null;

        // Add event listener
        try {
            console.log('Adding URL event listener');
            subscription = Linking.addListener('url', handleUrl);
        } catch (e) {
            console.log('Error setting up link listener:', e);
        }

        // Check if app was opened via URL
        console.log('Checking for initial URL');
        Linking.getInitialURL().then(initialUrl => {
            if (initialUrl) {
                console.log('App was opened via URL:', initialUrl);
                handleAuthCallbackUrl(initialUrl);
            } else {
                console.log('No initial URL found');
            }
        });

        // Check for stored URI only once at startup - no timeout check needed
        checkStoredUri();

        // Initial auth status check
        const checkAuth = async () => {
            try {
                console.log('Checking initial auth status');
                const result: boolean = await SpotifyModule.CheckAuthStatus();
                if (result) {
                    console.log('User is already authenticated');
                    setIsAuthenticated(true);
                } else {
                    console.log('User is not authenticated');
                }
            } catch (error) {
                console.log('Initial auth check failed:', error);
            }
        };

        checkAuth();

        // Cleanup
        return () => {
            console.log('Cleaning up URL handlers');
            if (subscription) {
                subscription.remove();
            }
        };
    }, [handleAuthCallbackUrl, checkStoredUri]);

    const handleAuthenticate = async () => {
        try {
            console.log('Starting Spotify authentication process');
            setIsAuthenticating(true);
            setErrors([]);

            // Reset last used code when starting a new auth flow
            lastUsedCode.current = null;

            // Get the auth URL from SpotifyModule
            console.log('Requesting auth URL from native module');
            const authUrl = await SpotifyModule.GetAuthUrl();
            console.log('Got auth URL, preparing to open browser');

            // Open the auth URL in browser
            const supported = await Linking.canOpenURL(authUrl);
            if (!supported) {
                console.error('Cannot open Spotify authentication URL');
                Alert.alert('Error', 'Cannot open Spotify authentication URL');
                setIsAuthenticating(false);
                return;
            }

            // Open the URL - the callback will be handled by our URL event listener
            console.log('Opening auth URL in browser');
            await Linking.openURL(authUrl);

            // Set a timeout to clear the authenticating state if no callback is received
            console.log('Setting authentication timeout');
            setTimeout(() => {
                setIsAuthenticating(prevState => {
                    if (prevState) {
                        console.log('Authentication timeout occurred');
                        Alert.alert('Authentication Timeout',
                            'Did not receive a response from Spotify. Please try again.');
                        codeExchangeInProgress.current = false;
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

    // Updated version of handleLikeSongs - now explicitly registers handlers
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

            console.log('Starting like songs operation');

            // Call the method but don't await - will get updates via events
            SpotifyModule.LikeSongs();

        } catch (error) {
            console.error('Error starting like songs operation:', error);
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
                console.log('Cancelling Spotify operation');
                SpotifyModule.CancelSpotifyOperation();
            } catch (error) {
                console.error('Error cancelling operation:', error);
            }
        }
    };

    // Updated handleFollowArtists to rely on backend checks and fix type issues
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
            console.log('Following artists from selected songs');

            const result = await SpotifyModule.FollowArtists();
            console.log('Follow artists request completed, parsing response');
            const response = JSON.parse(result) as SpotifyResponse;
            console.log('Parsed follow artists response:', response);

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
                console.log('All artists followed successfully');
                Alert.alert('Success', 'Artists have been followed on Spotify!');
            } else if (response.partialSuccess) {
                const errorsArray = response.errors || [];
                console.log(`Partial success: ${errorsArray.length} errors`);
                setErrors(errorsArray);
                Alert.alert('Partial Success', 'Some artists were followed, but there were errors. See details below.');
            } else {
                const errorsArray = response.errors || [];
                console.error('Failed to follow artists:', errorsArray);
                setErrors(errorsArray);
                Alert.alert('Error', 'Failed to follow artists on Spotify. See details below.');
            }
        } catch (error) {
            console.error('Error following artists:', error);
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