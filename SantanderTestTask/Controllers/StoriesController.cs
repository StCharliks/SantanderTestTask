using Microsoft.AspNetCore.Mvc;
using SantanderTestTask.Models;
using SantanderTestTask.Services;

namespace SantanderTestTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoriesController : ControllerBase
    {
        private readonly IStoriesService _storiesService;

        public StoriesController(IStoriesService storiesService)
        {
            _storiesService = storiesService;
        }

        [HttpGet("best/{count}")]
        public async Task<IReadOnlyList<Story>> GetBestStories([FromRoute]int count, CancellationToken cancellationToken)
        {
            return await _storiesService.GetBestStoriesAsync(count);
        }
    }
}
