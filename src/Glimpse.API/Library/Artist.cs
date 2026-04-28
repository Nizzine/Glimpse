namespace Glimpse.API.Library;

public record Artist
(
    string Name,
    HashSet<string> Tracks
);