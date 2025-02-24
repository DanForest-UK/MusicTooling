import React, { useState } from 'react';
import { View, Text, Button, FlatList } from 'react-native';
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
        <View style={{ padding: 20 }}>
            <Button title='Scan files' onPress={scanFiles} />

            <FlatList
                data={files}
                contentContainerStyle={{ padding: 3 }} 
                keyExtractor={(item) => item.Id}
                renderItem={({ item }) => (
                    <View style={{ marginVertical: 10, padding: 10, backgroundColor: '#8B4513', borderRadius: 5 }}>
                        <Text style={{ color: 'white' }}>Artist: {item.Artist}</Text>
                        <Text style={{ color: 'white' }}>Title: {item.Name}</Text>
                        <Text style={{ color: 'white' }}>Album: {item.Album}</Text>
                        <Text style={{ color: 'white' }}>Path: {item.Path}</Text>
                    </View>
                )}
            />
        </View>
    );
};

export default App;
