import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator, Alert } from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { NativeModules } from 'react-native';
import { styles } from './styles';
// Import the interfaces
import { AppModel } from './types';
// Import the SongItem component
import SongItem from './SongItem';

const { FileScannerModule, StateModule } = NativeModules;

// The polling interval in milliseconds
const POLLING_INTERVAL = 500; // Poll every 500ms

const App = () => {
    // Local UI state
    const [loading, setLoading] = useState(false);
    const [hasScanned, setHasScanned] = useState(false);

    // App state from C# backend
    const [appState, setAppState] = useState<AppModel>({
        songs: [],
        chosenSongs: [],
        minimumRating: 0,
    });

    // Reference to track the interval for cleanup
    const intervalRef = useRef<NodeJS.Timeout | null>(null);

    // Calculate filtered songs in the frontend - only filter by rating
    const filteredSongs = useMemo(() => {
        return appState.songs.filter(song =>
            song.rating >= appState.minimumRating
        );
    }, [appState.songs, appState.minimumRating]);

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

    return (
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
                    Showing {filteredSongs.length} of {appState.songs.length} songs
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
                    />
                )}
            />
        </View>
    );
};

export default App;
