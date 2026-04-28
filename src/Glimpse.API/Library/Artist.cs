namespace Glimpse.API.Library;

public record struct Artist
(
    string Name,
    IReadOnlyCollection<string> Tracks
);