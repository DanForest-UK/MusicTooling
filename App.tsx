import React, { useState } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator, Alert } from 'react-native';
import { Picker } from '@react-native-picker/picker';
import { NativeModules } from 'react-native';
import { styles } from './styles';

const { FileScannerModule } = NativeModules;

const App = () => {
    const [files, setFiles] = useState<SongInfo[]>([]);
    const [loading, setLoading] = useState(false);
    const [hasScanned, setHasScanned] = useState(false);
    const [minRating, setMinRating] = useState('0');

    const scanFiles = async () => {
        if (loading) { return; }
        setLoading(true);

        try {
            setFiles([]);
            const result: SongInfo[] = await FileScannerModule.ScanFiles(minRating);
            setFiles(result);
        } catch (error: any) {
            Alert.alert(
                'Scan Error',
                error?.message || 'An unknown error occurred while scanning files.'
            );
        } finally {
            setLoading(false);
            setHasScanned(true);
        }
    };

    return (
        <View style={styles.container}>
            <View style={styles.controlsContainer}>
                <View style={styles.buttonWrapper}>
                    <Button
                        title="Scan files"
                        onPress={() => scanFiles(minRating)}
                        disabled={loading}
                        color={loading ? '#A9A9A9' : '#007AFF'}
                    />
                </View>
                <Text style={styles.pickerLabel}>Minimum rating:</Text>
                <Picker
                    selectedValue={minRating}
                    style={styles.picker}
                    onValueChange={(itemValue) => setMinRating(itemValue)}
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

            {hasScanned && files.length === 0 && !loading && (
                <Text style={styles.emptyText}>No files found.</Text>
            )}

            <FlatList
                data={files}
                contentContainerStyle={styles.listContainer}
                keyExtractor={(item) => item.Id}
                renderItem={({ item }) => (
                    <View style={styles.fileItem}>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>
                                Artist: {item.Artist?.length ? item.Artist.join(', ') : '[No artist]'}
                            </Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>Title: {item.Name}</Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>Album: {item.Album}</Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>Rating: {item.Rating}</Text>
                        </View>
                        <View style={styles.fileTextContainer}>
                            <Text style={styles.fileText}>Path: {item.Path}</Text>
                        </View>
                    </View>
                )}
            />
        </View>
    );
};

export default App;
