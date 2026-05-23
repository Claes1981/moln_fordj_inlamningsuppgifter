namespace CloudSoft.Domain;

public class JobApplication : ICosmosEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PartitionKey { get; set; } = "JobApplication";

    public string JobPostingId { get; set; } = string.Empty;
    public string CandidateName { get; set; } = string.Empty;
    public string CoverLetter { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public bool IsValid(out string? error)
    {
        if (string.IsNullOrWhiteSpace(JobPostingId))
        {
            error = "Job posting is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(CandidateName))
        {
            error = "Candidate name is required.";
            return false;
        }

        if (CandidateName.Length > 200)
        {
            error = "Candidate name must not exceed 200 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(CoverLetter))
        {
            error = "Cover letter is required.";
            return false;
        }

        if (CoverLetter.Length > 5000)
        {
            error = "Cover letter must not exceed 5000 characters.";
            return false;
        }

        error = null;
        return true;
    }
}
