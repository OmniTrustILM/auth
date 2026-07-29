using Auth.Common.Data;
using Auth.Common.Services;
using Auth.Models.Dto;

namespace Auth.Services
{
    public interface IResourceService : ICrudService<ResourceDto, ResourceDetailDto>
    {
        Task<List<ResourceDetailDto>> GetAllResourcesAsync();

        Task AddResourcesAsync(List<ResourceSyncRequestDto> resources);

        Task<SyncResourcesResponseDto> SyncResourcesAsync(List<ResourceSyncRequestDto> resources);

    }
}
