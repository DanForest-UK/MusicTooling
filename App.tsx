import React, { useState, useEffect, useRef } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator, Alert, TouchableOpacity, SafeAreaView, DeviceEventEmitter } from 'react-native';
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
// Import the StatusProvider and StatusBar
import { StatusProvider } from './StatusContext';
import StatusBar from './Components/StatusBar';
// Import the SessionDialog component
import SessionDialog from './Components/Dialog';

// Native modules are imported at the top
const { FileScannerModule, StateModule } = NativeModules;

// Event name for state updates
const APP_STATE_UPDATED_EVENT = 'appStateUpdated';
const SAVED_STATE_EVENT = 'savedStateAvailable';

// Table header component defined outside of App component
const TableHeader: React.FC<{ showSpotifyStatus: boolean }> = ({ showSpotifyStatus }) => (
    <View style={styles.tableHeader}>
        <View style={styles.tableCheckboxCell}>
            <Text style={styles.tableHeaderText}></Text>
        </View>
        <View style={styles.tableArtistCell}>
            <Text style={styles.tableHeaderText}>Artist</Text>
        </View>
        <View style={styles.tableAlbumCell}>
            <Text style={styles.tableHeaderText}>Album</Text>
        </View>
        <View style={styles.tableTitleCell}>
            <Text style={styles.tableHeaderText}>Title</Text>
        </View>
        <View style={styles.tableRatingCell}>
            <Text style={styles.tableHeaderText}>Rating</Text>
        </View>
        {showSpotifyStatus && (
            <>
                <View style={styles.tableStatusCell}>
                    <Text style={styles.tableHeaderText}>Song</Text>
                </View>
                <View style={styles.tableStatusCell}>
                    <Text style={styles.tableHeaderText}>Artist</Text>
                </View>
            </>
        )}
    </View>
);

const App = () => {
    // Local UI state
    const [loading, setLoading] = useState(false);
    const [hasScanned, setHasScanned] = useState(false);
    const [showSpotify, setShowSpotify] = useState(false);
    const [showSpotifyStatus, setShowSpotifyStatus] = useState(false);
    const [lastValidFilteredSongs, setLastValidFilteredSongs] = useState<SongInfo[]>([]);
    const [showSessionDialog, setShowSessionDialog] = useState(false);
    const [isLoadingSession, setIsLoadingSession] = useState(false);
    const [isScanningFiles, setIsScanningFiles] = useState(false);

    // Track if we've had songs at least once - this prevents the panel from disappearing during operations
    const hasHadSongsRef = useRef(false);

    // App state from C# backend
    const [appState, setAppState] = useState<AppModel>({
        Songs: {},
        ChosenSongs: [],
        MinimumRating: 0,
    });

    // Store the last valid filtered songs in a separate effect
    // Only update when we have a valid object, even if it results in 0 filtered songs
    useEffect(() => {
        if (appState?.Songs && typeof appState.Songs === 'object') {
            const filtered = Object.values(appState.Songs).filter(song =>
                song.Rating >= (appState.MinimumRating || 0)
            );
            // We update lastValidFilteredSongs even if filtered.length is 0,
            // as long as appState.Songs is a valid object
            setLastValidFilteredSongs(filtered);

            // If we ever have songs, mark that we've had them
            if (filtered.length > 0) {
                hasHadSongsRef.current = true;
            }
        }
    }, [appState.Songs, appState.MinimumRating]);

    // Calculate filtered songs in the frontend - convert dictionary to array and filter by rating
    // Now with fallback to lastValidFilteredSongs if appState.Songs is undefined
    const filteredSongs = React.useMemo(() => {
        // Only fall back if appState.Songs is undefined or not an object
        // This ensures we show empty results when the filter genuinely produces no results
        if (!appState?.Songs || typeof appState.Songs !== 'object') {
            return lastValidFilteredSongs;
        }

        // Normal case - this could legitimately return an empty array if no songs match the filter
        return Object.values(appState.Songs).filter(song =>
            song.Rating >= (appState.MinimumRating || 0)
        );
    }, [appState?.Songs, appState?.MinimumRating, lastValidFilteredSongs]);

    // Listen for saved state event
    useEffect(() => {
        const subscription = DeviceEventEmitter.addListener(
            SAVED_STATE_EVENT,
            (eventData) => {
                // Check if saved state is available
                if (eventData?.available) {
                    console.log('Saved state detected, showing session dialog');
                    setShowSessionDialog(true);
                }
            }
        );

        return () => {
            subscription.remove();
        };
    }, []);

    // Check for saved state explicitly
    useEffect(() => {
        const checkSavedState = async () => {
            try {
                const hasSavedState = await StateModule.CheckForSavedState();
                if (hasSavedState) {
                    console.log('Saved state available, showing dialog');
                    setShowSessionDialog(true);
                }
            } catch (error) {
                console.error('Error checking for saved state:', error);
            }
        };

        checkSavedState();
    }, []);

    // Set up listener for state changes from C#
    useEffect(() => {
        // Initialize the state listener
        const initStateListener = async () => {
            try {
                // Register for state updates - this will also get the initial state
                await StateModule.RegisterStateListener();
                console.log('Registered for state updates');
            } catch (error) {
                console.error('Error registering for state updates:', error);
            }
        };

        // Add event listener for state updates
        const subscription = DeviceEventEmitter.addListener(
            APP_STATE_UPDATED_EVENT,
            (stateJson) => {
                try {
                    console.log('Received state update:', typeof stateJson, stateJson);
                    const newState = typeof stateJson === 'string' ? JSON.parse(stateJson) : stateJson;
                    console.log('Parsed new state:', newState);

                    // Check if Songs exist in the new state
                    console.log('Songs in new state:', newState.Songs ? Object.keys(newState.Songs).length : 'none');

                    setAppState(prevState => {
                        console.log('Previous state Songs:', prevState.Songs ? Object.keys(prevState.Songs).length : 'none');
                        // Only update if something changed
                        const shouldUpdate = JSON.stringify(newState) !== JSON.stringify(prevState);
                        console.log('Should update state?', shouldUpdate);
                        return shouldUpdate ? newState : prevState;
                    });
                } catch (error) {
                    console.error('Error processing state update:', error);
                }
            }
        );

        // Initialize the state listener
        initStateListener();

        // Cleanup on unmount
        return () => {
            subscription.remove();
        };
    }, []);

    const handleLoadSavedState = async () => {
        try {
            setLoading(true);
            setIsLoadingSession(true); // Set the flag to indicate we're loading a session
            setShowSessionDialog(false);

            console.log('Loading saved state...');
            const success = await StateModule.LoadSavedState();

            if (success) {
                console.log('Saved state loaded successfully');
                setHasScanned(true);
            } else {
                console.error('Failed to load saved state');
                Alert.alert('Error', 'Failed to load previous session');
            }
        } catch (error) {
            console.error('Error loading saved state:', error);
            Alert.alert('Error', 'An error occurred while loading the previous session');
        } finally {
            setIsLoadingSession(false); // Reset the flag
            setLoading(false);
        }
    };

    const handleDeleteSavedState = async () => {
        try {
            setShowSessionDialog(false);

            console.log('Deleting saved state...');
            await StateModule.DeleteSavedState();

            console.log('Saved state deleted');
        } catch (error) {
            console.error('Error deleting saved state:', error);
            Alert.alert('Error', 'An error occurred while deleting the previous session');
        }
    };

    const scanFiles = async () => {
        if (loading) return;

        setLoading(true);
        setIsScanningFiles(true);

        try {
            await FileScannerModule.ScanFiles();
        } catch (error: any) {
            Alert.alert(
                'Scan Error',
                error?.message || 'An unknown error occurred while scanning files.'
            );
        } finally {
            setIsScanningFiles(false);
            setLoading(false);
            setHasScanned(true);
        }
    };

    const cancelScan = async () => {
        try {
            await FileScannerModule.CancelScan();
        } catch (error) {
            console.error('Error cancelling scan:', error);
        }
    };

    const handleRatingChange = (rating: string) =>
        StateModule.SetMinimumRating(parseInt(rating, 10));

    const toggleSongSelection = async (songId: string) => {
        await StateModule.ToggleSongSelection(songId);
    };

    const toggleSpotifyPanel = () => {
        setShowSpotify(!showSpotify);
    };

    // Handler for when songs are liked or artists are followed
    const handleSpotifyAction = () => {
        setShowSpotifyStatus(true);
    };

    // Determine if we should show Spotify statuses by checking if any songs or artists have been processed
    const hasProcessedSpotifyItems = React.useMemo(() => {
        if (!appState?.Songs || typeof appState.Songs !== 'object') {
            return false;
        }
        return Object.values(appState.Songs).some(song =>
            song.SongStatus !== SpotifyStatus.NotSearched ||
            song.ArtistStatus !== SpotifyStatus.NotSearched
        );
    }, [appState?.Songs]);

    // Automatically show status if items have been processed
    useEffect(() => {
        if (hasProcessedSpotifyItems) {
            setShowSpotifyStatus(true);
        }
    }, [hasProcessedSpotifyItems]);

    // Determines whether we should show the Spotify toggle button
    // Shows if we have songs now OR we've had songs at some point (prevent toggle disappearing)
    const shouldShowSpotifyToggle = hasScanned && (filteredSongs.length > 0 || hasHadSongsRef.current);

    return (
        <StatusProvider>
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
                            selectedValue={String(appState?.MinimumRating || 0)}
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
                            Showing {filteredSongs.length} of {appState?.Songs ? Object.keys(appState.Songs).length : 0} songs
                        </Text>
                        <Text style={styles.statsText}>
                            {appState?.ChosenSongs?.length || 0} songs selected
                        </Text>
                    </View>

                    {loading && (
                        <View style={styles.loadingOverlay}>
                            <ActivityIndicator style={styles.activityIndicator} size={100} color="#0000ff" />
                            <Text style={styles.loadingText}>
                                {isLoadingSession ? 'Loading previous session...' : 'Scanning files...'}
                            </Text>
                            {isScanningFiles && (
                                <TouchableOpacity
                                    style={styles.cancelScanButton}
                                    onPress={cancelScan}
                                >
                                    <Text style={styles.cancelScanButtonText}>Cancel Scan</Text>
                                </TouchableOpacity>
                            )}
                        </View>
                    )}

                    {hasScanned && filteredSongs.length === 0 && !loading && (
                        <Text style={styles.emptyText}>No files found matching your criteria.</Text>
                    )}

                    {hasScanned && filteredSongs.length > 0 && (
                        <TableHeader showSpotifyStatus={showSpotifyStatus} />
                    )}

                    <FlatList
                        data={filteredSongs}
                        contentContainerStyle={[
                            styles.listContainer,
                            // Add dynamic bottom padding based on whether Spotify panel is visible
                            showSpotify ? { paddingBottom: 270 } : { paddingBottom: 80 }
                        ]}
                        keyExtractor={item => item.Id}
                        renderItem={({ item }) => (
                            <SongItem
                                item={item}
                                isSelected={Array.isArray(appState?.ChosenSongs) && appState.ChosenSongs.includes(item.Id)}
                                onToggle={toggleSongSelection}
                                showSpotifyStatus={showSpotifyStatus}
                            />
                        )}
                    />

                    {/* Spotify Toggle - Only show after scanning files with results */}
                    {shouldShowSpotifyToggle && (
                        <View style={styles.spotifyToggleContainer}>
                            <TouchableOpacity
                                style={styles.spotifyOptionsButton}
                                onPress={toggleSpotifyPanel}
                            >
                                <Text style={styles.spotifyOptionsButtonText}>
                                    {showSpotify ? 'Hide Spotify' : 'Spotify Options'}
                                </Text>
                            </TouchableOpacity>
                        </View>
                    )}

                    {/* Spotify Integration Panel */}
                    {showSpotify && hasScanned && hasHadSongsRef.current && (
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

                    {/* Session Dialog using custom modal implementation */}
                    <SessionDialog
                        visible={showSessionDialog}
                        onLoadSession={handleLoadSavedState}
                        onDeleteSession={handleDeleteSavedState}
                    />

                    <StatusBar />
                </View>
            </SafeAreaView>
        </StatusProvider>
    );
};

export default App;