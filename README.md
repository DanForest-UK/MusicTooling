# MusicTools

MusicTools is a React Native application with a C# backend that helps you manage your music files and integrate with Spotify. It allows you to scan your local music files, view and filter them by rating, and interact with Spotify to like songs and follow artists.

## Features

- Scan local music files and extract metadata
- View detailed song information including artist, album, and rating
- Filter songs by rating
- Select songs for Spotify integration
- Authenticate with Spotify
- Like songs on Spotify that match your local files
- Follow artists from your selected songs

## Requirements

- Windows 10 or later
- React Native development environment
- .NET development environment
- Spotify Developer account

## Setup

### 1. Clone the repository

```
git clone https://github.com/DanForest-UK/MusicTooling
cd musictools
```

### 2. Install dependencies and build

```
npm install
npm install --save react-native-windows@0.77.0
cd windows
msbuild /t:Restore MusicTools.sln
```

### 3. Configure Spotify API credentials

To use the Spotify integration features, you need to create a `spotify_settings.json` file with your Spotify API credentials.

1. Register your application on the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard/)
2. Get your Client ID and Client Secret
3. Create a file named `spotifySettings.json` in the application directory with the following content:

```json
{
  "ClientId": "your_client_id_here",
  "ClientSecret": "your_client_secret_here"
}
```

There is an example file in the repo. You can copy it and replace the placeholders with your own credentials.

**Important**: Do not commit this file to version control. It is already included in the `.gitignore` file.

### 4. Configure Spotify Redirect URI

Make sure to add `musictools://auth/callback` as a Redirect URI in your Spotify Developer Dashboard application settings.

## Running the Application

### Development

```
npx react-native run-windows
```

### Building a Release

```
npx react-native run-windows --release
```

## Usage

1. Launch the application
2. Click "Scan files" to scan your local music library
3. Use the rating filter to narrow down your song selection
4. Select songs by clicking on them
5. Click "Spotify Options" to open the Spotify integration panel
6. Connect to Spotify by clicking "Connect to Spotify"
7. Use "Like Songs" to add selected songs to your Spotify library
8. Use "Follow Artists" to follow the artists of selected songs

## File System Access

The application needs permission to access your file system to scan music files. You may be prompted to grant this permission when first running the app.

## Architecture

- **React Native UI**: Written in TypeScript, provides the user interface
- **C# Backend**: Implements native modules for file system access and Spotify integration
- **Spotify API Integration**: Connects to Spotify for authentication and actions

## Troubleshooting

### Authentication Issues

If you're having trouble with Spotify authentication:

1. Make sure your Client ID and Client Secret are correct in `spotify_settings.json`
2. Verify that your Redirect URI is properly configured in the Spotify Developer Dashboard
3. Check the application logs for specific error messages

### File Scanning Issues

If file scanning isn't working:

1. Ensure the application has file system access permission
2. Check that the music files are in a supported format

