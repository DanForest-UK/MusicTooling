import React, { createContext, useState, useContext, useEffect } from 'react';
import { NativeModules, DeviceEventEmitter } from 'react-native';

const { StatusModule } = NativeModules;

// Status message types to match C# enum
export enum StatusLevel {
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3
}

// Status message interface
export interface StatusMessage {
    Text: string;
    Level: StatusLevel;
    Id: string;
    Timestamp: string;
}

// Default/empty status message
const defaultStatus: StatusMessage = {
    Text: '',
    Level: StatusLevel.Info,
    Id: '',
    Timestamp: new Date().toISOString()
};

// Context interface
interface StatusContextType {
    status: StatusMessage;
    clearStatus: () => void;
}

// Create the context
const StatusContext = createContext<StatusContextType>({
    status: defaultStatus,
    clearStatus: () => { }
});

// Status event name used by native module
const STATUS_UPDATE_EVENT = 'statusUpdate';

// Provider component that manages status state
export const StatusProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [status, setStatus] = useState<StatusMessage>(defaultStatus);

    useEffect(() => {
        const initializeStatus = async () => {
            try {
                // Start the status update service - now handles as promise
                await StatusModule.StartStatusUpdates();

                // Register a listener id - now handles as promise
                const listenerId = `listener_${Date.now()}`;
                await StatusModule.AddListener(listenerId);

                console.log("Status listener initialized");
            } catch (error) {
                console.error("Error initializing status listener:", error);
            }
        };

        // Subscribe to status updates using DeviceEventEmitter
        const subscription = DeviceEventEmitter.addListener(
            STATUS_UPDATE_EVENT,
            (statusJson: string) => {
                try {
                    console.log("Received status update:", statusJson);
                    // Parse the JSON
                    const statusData = JSON.parse(statusJson);

                    // Set status using PascalCase properties
                    setStatus({
                        Text: statusData.Text,
                        Level: statusData.Level,
                        Id: statusData.Id,
                        Timestamp: statusData.Timestamp
                    });
                } catch (error) {
                    console.error('Error parsing status update:', error);
                }
            }
        );

        // Get the current status
        const fetchInitialStatus = async () => {
            try {
                const statusJson = await StatusModule.GetCurrentStatus();
                if (statusJson) {
                    console.log("Initial status:", statusJson);
                    const statusData = JSON.parse(statusJson);
                    if (statusData.Text) {
                        setStatus({
                            Text: statusData.Text,
                            Level: statusData.Level,
                            Id: statusData.Id,
                            Timestamp: statusData.Timestamp
                        });
                    }
                }
            } catch (error) {
                console.error('Error fetching initial status:', error);
            }
        };

        initializeStatus();
        fetchInitialStatus();

        // Cleanup
        return () => {
            subscription.remove();
        };
    }, []);

    // Function to clear the status
    const clearStatus = () => {
        setStatus(defaultStatus);
    };

    return (
        <StatusContext.Provider value={{ status, clearStatus }}>
            {children}
        </StatusContext.Provider>
    );
};

// Custom hook to use the status context
export const useStatus = (): StatusContextType => {
    return useContext(StatusContext);
};