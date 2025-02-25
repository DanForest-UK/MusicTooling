import React, { useState } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator, Alert } from 'react-native';
import { NativeModules } from 'react-native';
import { styles } from './styles';

const { FileScannerModule } = NativeModules;

const App = () => {
    const [files, setFiles] = useState<SongInfo[]>([]);
    const [loading, setLoading] = useState(false);
    const [hasScanned, setHasScanned] = useState(false); // Start as false

    const scanFiles = async () => {
        if (loading) { return; } // Prevent duplicate scans
        setLoading(true);

        try {
            setFiles([]); // Clear previous files
            const result: SongInfo[] = await FileScannerModule.ScanFiles();
            setFiles(result);
        } catch (error: any) {
            Alert.alert(
                'Scan Error',
                error?.message || 'An unknown error occurred while scanning files.'
            );
        } finally {
            setLoading(false);
            setHasScanned(true); // Now it updates whether scan succeeds or fails
        }
    };

    return (
        <View style={styles.container}>
            <Button
                title="Scan files"
                onPress={scanFiles}
                disabled={loading}
                color={loading ? '#A9A9A9' : '#007AFF'} // Greyed out when disabled
            />
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
                        <Text style={styles.fileText}>
                            Artist: {item.Artist?.length ? item.Artist.join(', ') : '[No artist]'}
                        </Text>
                        <Text style={styles.fileText}>Title: {item.Name}</Text>
                        <Text style={styles.fileText}>Album: {item.Album}</Text>
                        <Text style={styles.fileText}>Path: {item.Path}</Text>
                    </View>
                )}
            />
        </View>
    );
};

export default App;
