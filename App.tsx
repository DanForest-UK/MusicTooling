import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator, Alert, TouchableOpacity } from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { NativeModules } from 'react-native';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
import { styles } from './styles';
// Import the interfaces
import { AppModel, SongInfo } from './types';

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

            // Only update state if something changed (to avoid unnecessary renders)
            if (JSON.stringify(newState) !== JSON.stringify(appState)) {
                setAppState(newState);
            }
        } catch (error) {
            console.error('Error fetching state:', error);
        }
    }, [appState]);

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

    const renderStars = (rating: number) => (
        <View style={styles.starsContainer}>
            {[...Array(rating)].map((_, index) => (
                <FontAwesomeIcon key={index} name="star" style={styles.starIcon} />
            ))}
        </View>
    );

    const renderSongItem = ({ item }: { item: SongInfo }) => {
        const isSelected = appState.chosenSongs.includes(item.id);

        return (
            <View style={styles.fileItem}>
                <View style={styles.fileHeaderContainer}>
                    <TouchableOpacity
                        style={styles.checkboxContainer}
                        onPress={() => toggleSongSelection(item.id)}
                    >
                        <FontAwesomeIcon
                            name={isSelected ? 'check-square-o' : 'square-o'}
                            style={[styles.checkboxIcon, isSelected ? styles.checkboxIconChecked : null]}
                        />
                        <Text style={styles.fileHeaderText}>
                            {isSelected ? 'Selected' : 'Not Selected'}
                        </Text>
                    </TouchableOpacity>
                </View>
                <TouchableOpacity onPress={() => toggleSongSelection(item.id)}>
                    <View style={styles.fileTextContainer}>
                        <Text style={styles.fileText}>
                            Artist: {item.artist?.length ? item.artist.join(', ') : '[No artist]'}
                        </Text>
                    </View>
                    <View style={styles.fileTextContainer}>
                        <Text style={styles.fileText}>Title: {item.name}</Text>
                    </View>
                    <View style={styles.fileTextContainer}>
                        <Text style={styles.fileText}>Album: {item.album}</Text>
                    </View>
                    <View style={styles.fileTextContainer}>
                        <View style={styles.ratingRow}>
                            <Text style={styles.fileText}>Rating:</Text>
                            {renderStars(item.rating)}
                        </View>
                    </View>
                    <View style={styles.fileTextContainer}>
                        <Text style={styles.fileText}>Path: {item.path}</Text>
                    </View>
                </TouchableOpacity>
            </View>
        );
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
                renderItem={renderSongItem}
            />
        </View>
    );
};

export default App;
