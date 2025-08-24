namespace Application.DTOs;

public record PlayerScoredDto(
    string PlayerId,
    int Score
);

public record PlayerScoreDto(
    string PlayerId,
    int Score
);

public record GameEndedDto(
    string WinnerPlayerId,
    List<PlayerScoreDto> Scores
);
