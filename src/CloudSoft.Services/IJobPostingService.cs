using CloudSoft.Domain;

namespace CloudSoft.Services;

public interface IJobPostingService
{
    Task<IEnumerable<JobPosting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<JobPosting>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<JobPosting?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<JobPosting> CreateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task PublishAsync(string id, CancellationToken cancellationToken = default);
    Task CloseAsync(string id, CancellationToken cancellationToken = default);
}
