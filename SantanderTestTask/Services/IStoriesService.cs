using SantanderTestTask.Models;

namespace SantanderTestTask.Services
{
    public interface IStoriesService
    {
        Task<IReadOnlyList<Story>> GetBestStoriesAsync(int count);
    }
}
