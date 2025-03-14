import React from 'react';
import { View, Text, Button, TouchableOpacity, ActivityIndicator, ScrollView } from 'react-native';
import { styles } from '../styles';
import { SpotifyError } from '../types';

// Simplified progress state interface
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
    isCancelled: false
};

interface SpotifyIntegrationUIProps {
    isAuthenticated: boolean;
    isAuthenticating: boolean;
    isProcessing: boolean;
    progress: ProgressState;
    operationRunning: boolean;
    errors: SpotifyError[];
    selectedSongsCount: number;
    onAuthenticate: () => void;
    onLikeSongs: () => void;
    onFollowArtists: () => void;
    onCancelOperation: () => void;
    onClose: () => void;
}

const SpotifyIntegrationUI: React.FC<SpotifyIntegrationUIProps> = ({
    isAuthenticated,
    isAuthenticating,
    isProcessing,
    progress = defaultProgressState,
    operationRunning,
    errors,
    selectedSongsCount,
    onAuthenticate,
    onLikeSongs,
    onFollowArtists,
    onCancelOperation,
    onClose
}) => {
    // Track mounting/unmounting for debugging
    React.useEffect(() => {
        return () => {
            // Critical: If unmounting with operation running, DO NOT cancel automatically
            // The absence of the cancel call here is the fix
        };
    }, []);

    const renderErrors = () => {
        if (errors.length === 0) return null;

        return (
            <View style={styles.spotifyErrorContainer}>
                <Text style={styles.spotifyErrorTitle}>Error Details:</Text>
                <ScrollView style={{ maxHeight: 120 }}>
                    {errors.map((error, index) => (
                        <View key={index} style={styles.spotifyErrorItem}>
                            <Text style={styles.spotifyErrorCode}>{error.ErrorCode}</Text>
                            <Text style={styles.spotifyErrorMessage}>{error.Message}</Text>
                        </View>
                    ))}
                </ScrollView>
            </View>
        );
    };

    // Simplified component to render progress information
    const renderProgress = () => {
        if (progress.phase === 'idle') return null;

        return (
            <View style={styles.spotifyProgressContainer}>
                <View style={styles.spotifyProgressHeader}>
                    <Text style={styles.spotifyProgressTitle}>
                        {progress.message || 'Processing songs...'}
                    </Text>

                    {operationRunning && (
                        <TouchableOpacity
                            style={styles.spotifyCancelButtonCompact}
                            onPress={onCancelOperation}
                        >
                            <Text style={styles.spotifyCancelButtonText}>Cancel Operation</Text>
                        </TouchableOpacity>
                    )}
                </View>
            </View>
        );
    };

    return (
        <View>
            {isAuthenticating ? (
                <View style={{ alignItems: 'center', marginVertical: 15 }}>
                    <ActivityIndicator size="large" color="#1DB954" />
                    <Text style={{ color: 'white', marginTop: 10 }}>Authenticating with Spotify...</Text>
                </View>
            ) : (
                !isAuthenticated && (
                    <Button
                        title="Connect to Spotify"
                        onPress={onAuthenticate}
                        color="#1DB954"
                    />
                )
            )}

            {isAuthenticated && (
                <View>
                    <Text style={styles.spotifyConnectText}>
                        Connected to Spotify
                    </Text>

                    <View style={{ backgroundColor: '#3E3E3E', padding: 10, borderRadius: 4, marginBottom: 10 }}>
                        <Text style={{ color: 'white', fontSize: 12 }}>
                            Selected: {selectedSongsCount} songs
                        </Text>
                    </View>

                    {/* Render progress component */}
                    {renderProgress()}

                    {/* Only show action buttons if no operation is running */}
                    {!operationRunning && (
                        <View style={styles.spotifyButtonRow}>
                            <TouchableOpacity
                                style={[styles.spotifyButton, isProcessing && { opacity: 0.6 }]}
                                onPress={onLikeSongs}
                                disabled={isProcessing || operationRunning}
                            >
                                <Text style={styles.spotifyButtonText}>Like Songs</Text>
                            </TouchableOpacity>

                            <TouchableOpacity
                                style={[styles.spotifyButton, isProcessing && { opacity: 0.6 }]}
                                onPress={onFollowArtists}
                                disabled={isProcessing || operationRunning}
                            >
                                <Text style={styles.spotifyButtonText}>Follow Artists</Text>
                            </TouchableOpacity>
                        </View>
                    )}

                    {isProcessing && !operationRunning && (
                        <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'center', marginTop: 10 }}>
                            <ActivityIndicator size="small" color="#1DB954" />
                            <Text style={{ color: 'white', marginLeft: 10 }}>Processing...</Text>
                        </View>
                    )}
                </View>
            )}

            {renderErrors()}
        </View>
    );
};

export default React.memo(SpotifyIntegrationUI);