import React from 'react';
import { Button, NativeModules, View, Text } from 'react-native';

const { FileScanner } = NativeModules; // Import the native module

const App = () => {
    const scanFiles = async () => {
        try {
            const result = await FileScanner.ScanFiles(); // Call the C# function
            console.log("Scan Result:", result);
        } catch (error) {
            console.error("Error scanning files:", error);
        }
    };

    return (
        <View style={{ flex: 1, justifyContent: 'center', alignItems: 'center' }}>
            <Text style={{ fontSize: 20, marginBottom: 10 }}>React Native Windows</Text>
            <Button title="Scan Files" onPress={scanFiles} />
        </View>
    );
};

export default App;