using System.ComponentModel.DataAnnotations;

namespace CloudSoft.Web.Dtos;

/// <summary>
/// Input DTO for creating/updating a job posting via the REST API.
/// </summary>
public class JobPostingDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required, MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = "Draft";
}
