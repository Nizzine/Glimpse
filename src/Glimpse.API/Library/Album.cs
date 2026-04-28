namespace Glimpse.API.Library;

public record Album
(
    string Name,
    HashSet<string> Tracks
);