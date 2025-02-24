import React, { useState } from "react";
import { View, Text, Button, FlatList } from "react-native";
import { NativeModules } from "react-native";

const { FileScannerModule } = NativeModules;

type FileInfo = {
    Id: string;
    Name: string;
    Artist: string;
    Path: string;
};

const App = () => {
    const [files, setFiles] = useState<FileInfo[]>([]);

    const scanFiles = async () => {
        try {
            const result: FileInfo[] = await FileScannerModule.ScanFiles();
            setFiles(result);
        } catch (error) {
            console.error("Error scanning files:", error);
        }
    };

    return (
        <View style={{ padding: 20 }}>
            <Button title="Scan files" onPress={scanFiles} />

            <FlatList
                data={files}
                keyExtractor={(item) => item.Id}
                renderItem={({ item }) => (
                    <View style={{ marginVertical: 10, padding: 10, backgroundColor: "#ddd" }}>
                        <Text>Artist: {item.Artist}</Text>
                        <Text>Title: {item.Name}</Text>
                        <Text>Path: {item.Path}</Text>
                    </View>
                )}
            />
        </View>
    );
};

export default App;
