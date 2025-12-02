using SantanderTestTask.Models;

namespace SantanderTestTask.Repository
{
    public interface IStoriesRepository
    {
        Task<IReadOnlyList<int>> GetBestStoriesIdsAsync();

        Task<IReadOnlyList<Story>> GetStoriesAsync();
    }
}
