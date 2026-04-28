namespace Glimpse.API.Library;

public record struct Album
(
    string Name,
    IReadOnlyCollection<string> Tracks
);