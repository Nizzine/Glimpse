namespace Glimpse.API.Library;

public record Genre
(
    string Name,
    HashSet<string> Tracks
);