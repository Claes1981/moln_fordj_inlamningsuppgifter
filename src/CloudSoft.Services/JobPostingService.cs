using CloudSoft.Domain;

namespace CloudSoft.Services;

public class JobPostingService : IJobPostingService
{
    private readonly IRepository<JobPosting> _repository;

    public JobPostingService(IRepository<JobPosting> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<JobPosting>> GetAllAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<JobPosting>> GetPublishedAsync()
    {
        var all = await _repository.GetAllAsync();
        return all.Where(j => j.Status == JobPostingStatus.Published && j.IsActive);
    }

    public async Task<JobPosting?> GetByIdAsync(string id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<JobPosting> CreateAsync(JobPosting jobPosting)
    {
        if (!jobPosting.IsValid(out string? error))
        {
            throw new InvalidOperationException(error);
        }

        jobPosting.CreatedAt = DateTime.UtcNow;
        return await _repository.AddAsync(jobPosting);
    }

    public async Task UpdateAsync(JobPosting jobPosting)
    {
        if (!jobPosting.IsValid(out string? error))
        {
            throw new InvalidOperationException(error);
        }

        jobPosting.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(jobPosting);
    }

    public async Task DeleteAsync(string id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task PublishAsync(string id)
    {
        var jobPosting = await _repository.GetByIdAsync(id);
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
        await _repository.UpdateAsync(jobPosting);
    }

    public async Task CloseAsync(string id)
    {
        var jobPosting = await _repository.GetByIdAsync(id);
        if (jobPosting == null)
        {
            throw new InvalidOperationException($"Job posting with id '{id}' not found.");
        }

        jobPosting.Status = JobPostingStatus.Closed;
        jobPosting.IsActive = false;
        jobPosting.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(jobPosting);
    }
}
