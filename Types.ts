// Define types with exports

// Spotify status enum to match C# enum
export enum SpotifyStatus {
    NotSearched = 0,
    Found = 1,
    NotFound = 2,
    Liked = 3
}

// Define record type interfaces
export interface SongId {
    Value: number;
}

export interface SongName {
    Value: string;
}

export interface SongPath {
    Value: string;
}

export interface Artist {
    Value: string;
}

export interface Album {
    Value: string;
}

export interface SongRating {
    Value: number;
}

export interface SongInfo {
    Id: SongId;
    Name: SongName;
    Artist: Artist[];
    Album: Album;
    Rating: SongRating;
    Path: SongPath;
    ArtistStatus: SpotifyStatus;
    SongStatus: SpotifyStatus;
}

// Dictionary type for songs
export interface SongsDictionary {
    [id: string]: SongInfo;
}

export interface SongTuple {
    Index: number;
    Song: SongInfo;
}

export interface AppModel {
    Songs: SongTuple[] | SongsDictionary;
    ChosenSongs: number[];
    MinimumRating: SongRating;
}

// Spotify Error Types
export interface SpotifyError {
    // Match the PascalCase from C# record serialization
    ErrorCode: string;
    Message: string;
    ResourceId: string;
}

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

// Specific error types matching the C# counterparts
export interface AlreadyAuthenticatedError extends SpotifyError {
    ErrorCode: 'ALREADY_AUTHENTICATED';
}

export interface SongNotFoundError extends SpotifyError {
    ErrorCode: 'SONG_NOT_FOUND';
    ResourceId: string; // SongInfo.Id of the song
}

export interface ArtistNotFoundError extends SpotifyError {
    ErrorCode: 'ARTIST_NOT_FOUND';
    ResourceId: string; // Name of the artist
}

export interface AuthenticationError extends SpotifyError {
    ErrorCode: 'AUTH_ERROR';
    ResourceId: 'auth';
}

export interface RateLimitError extends SpotifyError {
    ErrorCode: 'RATE_LIMIT';
    RetryAfterSeconds?: number;
}

export interface ApiError extends SpotifyError {
    ErrorCode: 'API_ERROR';
    StatusCode?: number;
}

// Type guard functions to help with type narrowing
export const isAlreadyAuthenticatedError = (error: SpotifyError): error is AlreadyAuthenticatedError =>
    error.ErrorCode === 'ALREADY_AUTHENTICATED';

export const isSongNotFoundError = (error: SpotifyError): error is SongNotFoundError =>
    error.ErrorCode === 'SONG_NOT_FOUND';

export const isArtistNotFoundError = (error: SpotifyError): error is ArtistNotFoundError =>
    error.ErrorCode === 'ARTIST_NOT_FOUND';

export const isAuthenticationError = (error: SpotifyError): error is AuthenticationError =>
    error.ErrorCode === 'AUTH_ERROR';

export const isRateLimitError = (error: SpotifyError): error is RateLimitError =>
    error.ErrorCode === 'RATE_LIMIT';

export const isApiError = (error: SpotifyError): error is ApiError =>
    error.ErrorCode === 'API_ERROR';

// Response type for Spotify operations
export interface SpotifyResponse<T = boolean> {
    success?: boolean;
    partialSuccess?: boolean;
    error?: SpotifyError;
    errors?: SpotifyError[];
    data?: T;
    message?: string;
    cancelled?: boolean;
    noSongsToProcess?: boolean;
    noArtistsToProcess?: boolean;
}