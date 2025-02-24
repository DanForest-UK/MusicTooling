import React, { useState } from 'react';
import { View, Text, Button, FlatList, StyleSheet } from 'react-native';
import { NativeModules } from 'react-native';

const { FileScannerModule } = NativeModules;

const App = () => {
    const [files, setFiles] = useState<FileInfo[]>([]);

    const scanFiles = async () => {
        try {
            const result: FileInfo[] = await FileScannerModule.ScanFiles();
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
                        <Text style={styles.fileText}>Artist: {item.Artist}</Text>
                        <Text style={styles.fileText}>Title: {item.Name}</Text>
                        <Text style={styles.fileText}>Album: {item.Album}</Text>
                        <Text style={styles.fileText}>Path: {item.Path}</Text>
                    </View>
                )}
            />
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        padding: 20,
    },
    listContainer: {
        padding: 3,
    },
    fileItem: {
        marginVertical: 10,
        padding: 10,
        backgroundColor: '#8B4513',
        borderRadius: 5,
    },
    fileText: {
        color: 'white',
    },
});

export default App;
