import React, { useState } from 'react';
import { View, Button, Text } from 'react-native';
import { NativeModules } from 'react-native';

console.log("Native Modules: ", NativeModules);

const { FileScannerModule } = NativeModules;

const App = () => {
    const [fileList, setFileList] = useState<string[]>([]);

    const handleScanFiles = async () => {
        try {
            const result: string[] = await FileScannerModule.ScanFiles();
            setFileList(result); // Update state with the returned list
        } catch (error) {
            console.error('Error scanning files:', error);
        }
    };

    return (
        <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
            <Button title="Scan files" onPress={handleScanFiles} />

            {/* Display the file list */}
            {fileList.length > 0 && (
                <View style={{ marginTop: 20 }}>
                    {fileList.map((file, index) => (
                        <Text key={index} style={{ marginBottom: 5 }}>{file}</Text>
                    ))}
                </View>
            )}
        </View>
    );
};

export default App;
