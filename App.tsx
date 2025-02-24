import React, { useState } from 'react';
import { View, Text, Button, FlatList, ActivityIndicator } from 'react-native';
import { NativeModules } from 'react-native';
import { styles } from './styles';

const { FileScannerModule } = NativeModules;

const App = () => {
    const [files, setFiles] = useState<SongInfo[]>([]);
    const [loading, setLoading] = useState(false);

    const scanFiles = async () => {
        setLoading(true);

        try {
            const result: SongInfo[] = await FileScannerModule.ScanFiles();
            setFiles(result);
        } catch (error) {
            console.error('Error scanning files:', error);
        } finally {
            setLoading(false);
        }
    };

    return (
        <View style={styles.container}>
            <Button title='Scan files' onPress={scanFiles} disabled={loading} />
            {loading && (
                <View style={styles.loadingOverlay}>
                    <ActivityIndicator size='large' color='#0000ff' />
                    <Text style={styles.loadingText}>Scanning files...</Text>
                </View>
            )}

            <FlatList
                data={files}
                contentContainerStyle={styles.listContainer}
                keyExtractor={(item) => item.Id}
                renderItem={({ item }) => (
                    <View style={styles.fileItem}>
                        <Text style={styles.fileText}>
                            Artist: {item.Artist.length > 0 ? item.Artist.join(', ') : '[No artist]'}
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
