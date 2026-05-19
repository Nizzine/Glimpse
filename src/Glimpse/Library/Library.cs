using Glimpse.API.Library;

namespace Glimpse.Library;

public record Library
(
    uint Version,
    IReadOnlyCollection<string> LibraryPaths,
    IReadOnlyCollection<string> ExcludedDirectories,
    IReadOnlyCollection<Track> Tracks,
    IReadOnlyCollection<Album> Albums,
    IReadOnlyCollection<Artist> Artists,
    IReadOnlyCollection<Genre> Genres
);