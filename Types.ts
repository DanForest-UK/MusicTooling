// Define types with exports
export interface SongInfo {
    id: string;
    name: string;
    artist: string[];
    album: string;
    rating: number;
    path: string;
}

export interface AppModel {
    songs: SongInfo[];
    chosenSongs: string[];
    minimumRating: number;
}
// spotify-errors.ts
// Spotify Error Types
export interface SpotifyError {
    // Match the PascalCase from C# serialization
    ErrorCode: string;
    Message: string;
    ResourceId: string;

    // Also include camelCase aliases for backwards compatibility
    errorCode?: string;
    message?: string;
    resourceId?: string;
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
    success: boolean;
    partialSuccess?: boolean;
    error?: SpotifyError;
    errors?: SpotifyError[];
    data?: T;

    // PascalCase versions for C# serialization
    Success?: boolean;
    PartialSuccess?: boolean;
    Error?: SpotifyError;
    Errors?: SpotifyError[];
    Data?: T;
}