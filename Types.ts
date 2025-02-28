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