namespace CloudSoft.Domain;

public class JobPosting : ICosmosEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PartitionKey { get; set; } = "JobPosting";

    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JobPostingStatus Status { get; set; } = JobPostingStatus.Draft;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            error = "Job title is required.";
            return false;
        }

        if (Title.Length > 200)
        {
            error = "Job title must not exceed 200 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Location))
        {
            error = "Location is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            error = "Description is required.";
            return false;
        }

        if (Description.Length > 5000)
        {
            error = "Description must not exceed 5000 characters.";
            return false;
        }

        error = null;
        return true;
    }
}
