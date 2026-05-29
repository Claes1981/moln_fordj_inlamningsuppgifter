namespace CloudSoft.Web.Dtos;

/// <summary>
/// Output DTO for job posting responses via the REST API.
/// Exposes only the fields needed by API consumers.
/// </summary>
public class JobPostingOutputDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
