namespace BoardGameTracker.Common.DTOs;

public class RagAnswerDto
{
    public string Answer { get; set; } = string.Empty;
    public bool HasContext { get; set; }
    public List<RagCitationDto> Citations { get; set; } = new();
}
