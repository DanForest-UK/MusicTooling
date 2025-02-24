import React, { useState } from 'react';
import { View, Text, Button, FlatList } from 'react-native';
import { NativeModules } from 'react-native';
import { styles } from './styles'; // Import styles from external file

const { FileScannerModule } = NativeModules;

const App = () => {
    const [files, setFiles] = useState<SongInfo[]>([]);

    const scanFiles = async () => {
        try {
            const result: SongInfo[] = await FileScannerModule.ScanFiles();
            setFiles(result);
        } catch (error) {
            console.error('Error scanning files:', error);
        }
    };

    return (
        <View style={styles.container}>
            <Button title="Scan files" onPress={scanFiles} />

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
