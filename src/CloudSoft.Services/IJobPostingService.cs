using CloudSoft.Domain;

namespace CloudSoft.Services;

public interface IJobPostingService
{
    Task<IEnumerable<JobPosting>> GetAllAsync();
    Task<IEnumerable<JobPosting>> GetPublishedAsync();
    Task<JobPosting?> GetByIdAsync(string id);
    Task<JobPosting> CreateAsync(JobPosting jobPosting);
    Task UpdateAsync(JobPosting jobPosting);
    Task DeleteAsync(string id);
    Task PublishAsync(string id);
    Task CloseAsync(string id);
}
