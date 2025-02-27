import React, { useState, useEffect } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator, Alert } from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { NativeModules } from 'react-native';
import FontAwesomeIcon from 'react-native-vector-icons/FontAwesome';
import { styles } from './styles';
// Import the interfaces
import { AppModel, SongInfo } from './types';

const { FileScannerModule, StateModule } = NativeModules;

// The polling interval in milliseconds
const POLLING_INTERVAL = 1000; // 1 second

const App = () => {
    // Local UI state
    const [loading, setLoading] = useState(false);
    const [hasScanned, setHasScanned] = useState(false);

    // App state from C# backend - now with explicit type annotation
    const [appState, setAppState] = useState<AppModel>({ songs: [], minimumRating: 0 });

    // Initialize and poll for state changes
    useEffect(() => {
        // Get initial state
        fetchCurrentState();

        // Set up polling for state changes
        const intervalId = setInterval(fetchCurrentState, POLLING_INTERVAL);

        // Cleanup interval on unmount
        return () => {
            clearInterval(intervalId);
        };
    }, []);

    // Function to fetch current state from native module with improved debugging
    const fetchCurrentState = async () => {
        try {
            const stateJson = await StateModule.GetCurrentState();
             setAppState(JSON.parse(stateJson) as AppModel);
        } catch (error) {
            console.error('Error fetching state:', error);
        }
    };

    const scanFiles = async () => {
        if (loading) return;
        setLoading(true);

        try {
            // No need to pass minimumRating - it's already in the state
            await FileScannerModule.ScanFiles();
        } catch (error: any) {
            Alert.alert(
                'Scan Error',
                error?.message || 'An unknown error occurred while scanning files.'
            );
        } finally {
            setLoading(false);
            setHasScanned(true);
            // Fetch latest state after scan
            fetchCurrentState();
        }
    };

    const handleRatingChange = (rating: string) => {
        // Update rating in state when picker changes
        StateModule.SetMinimumRating(parseInt(rating, 10));
        // Fetch latest state after rating change
        fetchCurrentState();
    };

    const renderStars = (rating: number) => {
        return (
            <View style={styles.starsContainer}>
                {[...Array(rating)].map((_, index) => (
                    <FontAwesomeIcon key={index} name="star" style={styles.starIcon} />
                ))}
            </View>
        );
    };

    // This explicitly uses the SongInfo interface to fix the linting warning
    const renderSongItem = ({ item }: { item: SongInfo }) => (
        <View style={styles.fileItem}>
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
        </View>
    );

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

            {loading && (
                <View style={styles.loadingOverlay}>
                    <ActivityIndicator style={styles.activityIndicator} size={100} color="#0000ff" />
                    <Text style={styles.loadingText}>Scanning files...</Text>
                </View>
            )}

            {hasScanned && appState.songs?.length === 0 && !loading && (
                <Text style={styles.emptyText}>No files found.</Text>
            )}

            <FlatList
                data={appState.songs}
                contentContainerStyle={styles.listContainer}
                keyExtractor={(item) => item.id}
                renderItem={renderSongItem}
            />
        </View>
    );
};

export default App;