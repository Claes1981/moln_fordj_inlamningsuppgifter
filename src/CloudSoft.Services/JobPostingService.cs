using CloudSoft.Domain;

namespace CloudSoft.Services;

public class JobPostingService : IJobPostingService
{
    private readonly IRepository<JobPosting> _repository;

    public JobPostingService(IRepository<JobPosting> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<JobPosting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetAllAsync(cancellationToken);
    }

    public async Task<IEnumerable<JobPosting>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        var all = await _repository.GetAllAsync(cancellationToken);
        return all.Where(j => j.Status == JobPostingStatus.Published && j.IsActive);
    }

    public async Task<JobPosting?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<JobPosting> CreateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default)
    {
        if (!jobPosting.IsValid(out string? error))
        {
            throw new InvalidOperationException(error);
        }

        jobPosting.CreatedAt = DateTime.UtcNow;
        return await _repository.AddAsync(jobPosting, cancellationToken);
    }

    public async Task UpdateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default)
    {
        if (!jobPosting.IsValid(out string? error))
        {
            throw new InvalidOperationException(error);
        }

        jobPosting.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(jobPosting, cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
    }

    public async Task PublishAsync(string id, CancellationToken cancellationToken = default)
    {
        var jobPosting = await _repository.GetByIdAsync(id, cancellationToken);
        if (jobPosting == null)
        {
            throw new InvalidOperationException($"Job posting with id '{id}' not found.");
        }

        if (!jobPosting.IsValid(out string? error))
        {
            throw new InvalidOperationException($"Cannot publish: {error}");
        }

        jobPosting.Status = JobPostingStatus.Published;
        jobPosting.IsActive = true;
        jobPosting.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(jobPosting, cancellationToken);
    }

    public async Task CloseAsync(string id, CancellationToken cancellationToken = default)
    {
        var jobPosting = await _repository.GetByIdAsync(id, cancellationToken);
        if (jobPosting == null)
        {
            throw new InvalidOperationException($"Job posting with id '{id}' not found.");
        }

        jobPosting.Status = JobPostingStatus.Closed;
        jobPosting.IsActive = false;
        jobPosting.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(jobPosting, cancellationToken);
    }
}
