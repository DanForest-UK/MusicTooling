import React from 'react';
import { View, Text, Button, TouchableOpacity, ActivityIndicator, ScrollView, ProgressBarAndroid } from 'react-native';
import { styles } from '../styles';
import { SpotifyError } from '../types';

// Progress state interface matching the hook
interface ProgressState {
    phase: 'idle' | 'initializing' | 'searching' | 'searchComplete' | 'liking' | 'complete';
    totalSongs: number;
    processed: number;
    found: number;
    liked: number;
    message: string;
    isComplete: boolean;
    isError: boolean;
    isCancelled: boolean;
}

const defaultProgressState: ProgressState = {
    phase: 'idle',
    totalSongs: 0,
    processed: 0,
    found: 0,
    liked: 0,
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

    // Component to render progress information
    const renderProgress = () => {
        if (progress.phase === 'idle') return null;

        // Calculate progress percentage for the progress bar
        let progressPercentage = 0;
        if (progress.totalSongs > 0) {
            if (progress.phase === 'searching' || progress.phase === 'searchComplete') {
                progressPercentage = progress.processed / progress.totalSongs;
            } else if (progress.phase === 'liking') {
                progressPercentage = 0.5 + (progress.liked / (progress.found || 1) * 0.5); // 50-100% for liking phase
            } else if (progress.phase === 'complete') {
                progressPercentage = 1; // 100% complete
            }
        }

        return (
            <View style={styles.spotifyProgressContainer}>
                <Text style={styles.spotifyProgressTitle}>
                    {progress.message || 'Processing songs...'}
                </Text>

                <View style={styles.spotifyProgressBarContainer}>
                    <ProgressBarAndroid
                        styleAttr="Horizontal"
                        indeterminate={progress.phase === 'initializing'}
                        progress={progressPercentage}
                        color="#1DB954"
                        style={styles.spotifyProgressBar}
                    />
                </View>

                <View style={styles.spotifyProgressStats}>
                    {progress.phase !== 'idle' && progress.phase !== 'initializing' && (
                        <>
                            <Text style={styles.spotifyProgressStatText}>
                                Processed: {progress.processed}/{progress.totalSongs}
                            </Text>
                            <Text style={styles.spotifyProgressStatText}>
                                Found: {progress.found}
                            </Text>
                            {progress.phase === 'liking' || progress.phase === 'complete' ? (
                                <Text style={styles.spotifyProgressStatText}>
                                    Liked: {progress.liked}
                                </Text>
                            ) : null}
                        </>
                    )}
                </View>

                {operationRunning && (
                    <TouchableOpacity
                        style={styles.spotifyCancelButton}
                        onPress={onCancelOperation}
                    >
                        <Text style={styles.spotifyCancelButtonText}>Cancel Operation</Text>
                    </TouchableOpacity>
                )}
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