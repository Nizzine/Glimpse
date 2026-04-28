namespace Glimpse.API.Library;

public record struct Genre
(
    string Name,
    IReadOnlyCollection<string> Tracks
);