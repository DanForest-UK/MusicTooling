import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator, Alert, TouchableOpacity, SafeAreaView } from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { NativeModules } from 'react-native';
import { styles } from './styles';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
// Import the interfaces with updated SpotifyStatus enum
import { AppModel, SpotifyStatus, SongInfo } from './types';
// Import the SongItem component
import SongItem from './Components/SongItem';
// Import the SpotifyIntegration component 
import SpotifyIntegration from './Components/SpotifyIntegration';

// Native modules are imported at the top
const { FileScannerModule, StateModule } = NativeModules;

// The polling interval in milliseconds
const POLLING_INTERVAL = 500; // Poll every 500ms

const App = () => {
    // Local UI state
    const [loading, setLoading] = useState(false);
    const [hasScanned, setHasScanned] = useState(false);
    const [showSpotify, setShowSpotify] = useState(false);
    const [showSpotifyStatus, setShowSpotifyStatus] = useState(false);
    const [lastValidFilteredSongs, setLastValidFilteredSongs] = useState<SongInfo[]>([]);

    // App state from C# backend
    const [appState, setAppState] = useState<AppModel>({
        songs: {},
        chosenSongs: [],
        minimumRating: 0,
    });

    // Reference to track the interval for cleanup
    const intervalRef = useRef<NodeJS.Timeout | null>(null);

    // Store the last valid filtered songs in a separate effect
    // Only update when we have a valid object, even if it results in 0 filtered songs
    useEffect(() => {
        if (appState.songs && typeof appState.songs === 'object') {
            const filtered = Object.values(appState.songs).filter(song =>
                song.rating >= appState.minimumRating
            );
            // We update lastValidFilteredSongs even if filtered.length is 0,
            // as long as appState.songs is a valid object
            setLastValidFilteredSongs(filtered);
        }
    }, [appState.songs, appState.minimumRating]);

    // Calculate filtered songs in the frontend - convert dictionary to array and filter by rating
    // Now with fallback to lastValidFilteredSongs if appState.songs is undefined
    const filteredSongs = useMemo(() => {
        // Only fall back if appState.songs is undefined or not an object
        // This ensures we show empty results when the filter genuinely produces no results
        if (!appState.songs || typeof appState.songs !== 'object') {
            console.log('Using fallback filtered songs due to invalid appState.songs');
            return lastValidFilteredSongs;
        }

        // Normal case - this could legitimately return an empty array if no songs match the filter
        return Object.values(appState.songs).filter(song =>
            song.rating >= appState.minimumRating
        );
    }, [appState.songs, appState.minimumRating, lastValidFilteredSongs]);

    // Fetch current state from native module
    const fetchCurrentState = useCallback(async () => {
        try {
            const stateJson = await StateModule.GetCurrentState();
            const newState = JSON.parse(stateJson) as AppModel;

            // Update state without referencing the current appState in the callback
            setAppState(prevState => {
                // Only update if something changed
                return JSON.stringify(newState) !== JSON.stringify(prevState)
                    ? newState
                    : prevState;
            });
        } catch (error) {
            console.error('Error fetching state:', error);
        }
    }, []); // Remove appState dependency

    // Set up polling for state changes
    useEffect(() => {
        // Get initial state
        fetchCurrentState();

        // Set up polling interval
        intervalRef.current = setInterval(fetchCurrentState, POLLING_INTERVAL);

        // Cleanup on unmount
        return () => {
            if (intervalRef.current) { clearInterval(intervalRef.current); }
        };
    }, [fetchCurrentState]);

    const scanFiles = async () => {
        if (loading) { return; }
        setLoading(true);

        try {
            await FileScannerModule.ScanFiles();
        } catch (error: any) {
            Alert.alert(
                'Scan Error',
                error?.message || 'An unknown error occurred while scanning files.'
            );
        } finally {
            setLoading(false);
            setHasScanned(true);
            fetchCurrentState();
        }
    };

    const handleRatingChange = (rating: string) =>
        StateModule.SetMinimumRating(parseInt(rating, 10));

    const toggleSongSelection = async (songId: string) => {
        await StateModule.ToggleSongSelection(songId);
        // Immediately fetch the updated state to reflect the change
        fetchCurrentState();
    };

    const toggleSpotifyPanel = () => {
        setShowSpotify(!showSpotify);
    };

    // Handler for when songs are liked or artists are followed
    const handleSpotifyAction = () => {
        setShowSpotifyStatus(true);
    };

    // Determine if we should show Spotify statuses by checking if any songs or artists have been processed
    const hasProcessedSpotifyItems = useMemo(() => {
        if (!appState.songs || typeof appState.songs !== 'object') {
            return false;
        }
        return Object.values(appState.songs).some(song =>
            song.songStatus !== SpotifyStatus.NotSearched ||
            song.artistStatus !== SpotifyStatus.NotSearched
        );
    }, [appState.songs]);

    // Automatically show status if items have been processed
    useEffect(() => {
        if (hasProcessedSpotifyItems) {
            setShowSpotifyStatus(true);
        }
    }, [hasProcessedSpotifyItems]);

    return (
        <SafeAreaView style={{ flex: 1 }}>
            <View style={styles.container}>
                <View style={styles.controlsContainer}>
                    <View style={styles.buttonWrapper}>
                        <Button
                            title="Scan files"
                            onPress={scanFiles}
                            disabled={loading}
                            color={loading ? '#A9A9A9' : '#007AFF'}
                        />
                    </View>
                    <Text style={styles.pickerLabel}>Minimum rating:</Text>
                    <Picker
                        selectedValue={String(appState.minimumRating)}
                        style={styles.picker}
                        onValueChange={handleRatingChange}
                    >
                        <Picker.Item label="Any" value="0" />
                        <Picker.Item label="1 star" value="1" />
                        <Picker.Item label="2 stars" value="2" />
                        <Picker.Item label="3 stars" value="3" />
                        <Picker.Item label="4 stars" value="4" />
                        <Picker.Item label="5 stars" value="5" />
                    </Picker>
                </View>

                <View style={styles.statsContainer}>
                    <Text style={styles.statsText}>
                        Showing {filteredSongs.length} of {appState.songs ? Object.keys(appState.songs).length : 0} songs
                    </Text>
                    <Text style={styles.statsText}>
                        {appState.chosenSongs.length} songs selected
                    </Text>
                </View>

                {loading && (
                    <View style={styles.loadingOverlay}>
                        <ActivityIndicator style={styles.activityIndicator} size={100} color="#0000ff" />
                        <Text style={styles.loadingText}>Scanning files...</Text>
                    </View>
                )}

                {hasScanned && filteredSongs.length === 0 && !loading && (
                    <Text style={styles.emptyText}>No files found matching your criteria.</Text>
                )}

                <FlatList
                    data={filteredSongs}
                    contentContainerStyle={styles.listContainer}
                    keyExtractor={item => item.id}
                    renderItem={({ item }) => (
                        <SongItem
                            item={item}
                            isSelected={appState.chosenSongs.includes(item.id)}
                            onToggle={toggleSongSelection}
                            showSpotifyStatus={showSpotifyStatus}
                        />
                    )}
                />

                {/* Spotify Toggle - Only show after scanning files with results */}
                {hasScanned && filteredSongs.length > 0 && (
                    <View style={styles.spotifyToggleContainer}>
                        <TouchableOpacity
                            style={styles.spotifyOptionsButton}
                            onPress={toggleSpotifyPanel}
                        >
                            <Text style={styles.spotifyOptionsButtonText}>
                                {showSpotify ? "Hide Spotify" : "Spotify Options"}
                            </Text>
                        </TouchableOpacity>
                    </View>
                )}

                {/* Spotify Integration Panel */}
                {showSpotify && hasScanned && filteredSongs.length > 0 && (
                    <View style={styles.spotifyContainer}>
                        <View style={styles.spotifyHeader}>
                            <Text style={styles.spotifyTitle}>Spotify Integration</Text>
                            <TouchableOpacity
                                style={styles.spotifyCloseButton}
                                onPress={toggleSpotifyPanel}
                            >
                                <FontAwesomeIcon name="times" style={styles.spotifyCloseIcon} />
                            </TouchableOpacity>
                        </View>
                        <SpotifyIntegration
                            appState={appState}
                            onClose={toggleSpotifyPanel}
                            onSpotifyAction={handleSpotifyAction}
                        />
                    </View>
                )}
            </View>
        </SafeAreaView>
    );
};

export default App;