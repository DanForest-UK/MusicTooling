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
    Timestamp: new Date().toISOString(),
};

// Context interface
interface StatusContextType {
    status: StatusMessage;
    clearStatus: () => void;
}

// Create the context
const StatusContext = createContext<StatusContextType>({
    status: defaultStatus,
    clearStatus: () => { },
});

// Provider component that manages status state
export const StatusProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [status, setStatus] = useState<StatusMessage>(defaultStatus);

    // Function to update status with protection against empty status
    const updateStatus = (newStatus: StatusMessage) => {
        // Skip setting empty status unless we're explicitly clearing
        if (!newStatus.Text && status.Text) {
            return;
        }

        setStatus(newStatus);
    };

    useEffect(() => {
        const initializeStatus = async () => {
            try {
                // Start the status update service
                await StatusModule.StartStatusUpdates();

                // Register a listener id
                const listenerId = `listener_${Date.now()}`;
                await StatusModule.AddListener(listenerId);
            } catch (error) {
                console.error('Error initializing status listener:', error);
            }
        };

        // Subscribe to status updates using DeviceEventEmitter
        const subscription = DeviceEventEmitter.addListener(
            'statusUpdate',
            (statusJson: string) => {
                try {
                    // Parse the JSON
                    const statusData = typeof statusJson === 'string'
                        ? JSON.parse(statusJson)
                        : statusJson;

                    // Only update if we have text or we're explicitly clearing
                    if (statusData.Text || !status.Text) {
                        // Set status using PascalCase properties
                        updateStatus({
                            Text: statusData.Text || '',
                            Level: statusData.Level,
                            Id: statusData.Id || '',
                            Timestamp: statusData.Timestamp || new Date().toISOString(),
                        });
                    }
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
                    const statusData = JSON.parse(statusJson);
                    if (statusData.Text) {
                        updateStatus({
                            Text: statusData.Text,
                            Level: statusData.Level,
                            Id: statusData.Id,
                            Timestamp: statusData.Timestamp,
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