using Czertainly.Auth.Common.Helpers;
using Czertainly.Auth.Common.Services;
using Czertainly.Auth.Data.Contracts;
using Czertainly.Auth.Models.Dto;
using Czertainly.Auth.Models.Entities;
using Czertainly.Auth.Models.Mappings;

namespace Czertainly.Auth.Services
{
    public class ResourceService : CrudService<Resource, ResourceDto, ResourceDetailDto>, IResourceService
    {
        private const string AnyActionName = "ANY";

        private readonly IActionService _actionService;

        public ResourceService(IRepositoryManager repositoryManager, ILogger<ResourceService> logger, IActionService actionService)
            : base(repositoryManager, repositoryManager.Resource, ResourceEntityMapper.Instance, logger)
        {
            _actionService = actionService;
        }

        public async Task<List<ResourceDetailDto>> GetAllResourcesAsync()
        {
            var resources = await _repositoryManager.Resource.GetResourcesWithActions();
            return resources.Select(resource => resource.ToDetailDto()).ToList();
        }

        public async Task AddResourcesAsync(List<ResourceSyncRequestDto> resources)
        {
            _logger.LogInformation("Saving resources");
            var resourcesMapping = await _repositoryManager.Resource.GetResourcesMapAsync(r => r.Name);
            var actionsMapping = await _repositoryManager.Action.GetDictionaryMap(a => a.Name);

            var transaction = await _repositoryManager.BeginTransactionAsync();

            try
            {
                foreach (var resourceSyncDto in resources)
                {
                    // check if to add/update resource
                    if (!resourcesMapping.TryGetValue(resourceSyncDto.Name, out var resourceEntity))
                    {
                        resourceEntity = new Resource { Name = resourceSyncDto.Name, DisplayName = DisplayNameHelper.GetDisplayName(resourceSyncDto.Name), ListObjectsEndpoint = resourceSyncDto.ListObjectsEndpoint, Actions = new List<Models.Entities.Action>() };
                        _repository.Create(resourceEntity);
                        _repository.Reload(resourceEntity);

                        resourcesMapping.Add(resourceSyncDto.Name, resourceEntity);
                    }
                    
                    // check if to add action
                    foreach (var actionName in resourceSyncDto.Actions)
                    {
                        if (actionName == AnyActionName) continue;

                        if (!actionsMapping.TryGetValue(actionName, out var actionEntity))
                        {
                            var dto = new ActionRequestDto { Name = actionName, DisplayName = DisplayNameHelper.GetDisplayName(actionName) };
                            var actionDto = await _actionService.CreateAsync(dto);
                            actionEntity = await _repositoryManager.Action.GetByKeyAsync(actionDto.Uuid);
                            actionsMapping.Add(actionName, actionEntity);
                        }
                        resourceEntity.Actions.Add(actionEntity);
                    }
                }

                await _repositoryManager.SaveAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<SyncResourcesResponseDto> SyncResourcesAsync(List<ResourceSyncRequestDto> resources)
        {
            _logger.LogInformation("Synchronizing resources from Core service");
            var result = new SyncResourcesResponseDto();

            var resourcesUsed = new HashSet<string>(StringComparer.Ordinal);
            var actionsUsed = new HashSet<string>(StringComparer.Ordinal);

            var resourcesMapping = await _repositoryManager.Resource.GetResourcesMapAsync(r => r.Name);
            var actionsMapping = await _repositoryManager.Action.GetDictionaryMap(a => a.Name);

            var transaction = await _repositoryManager.BeginTransactionAsync();

            // cleanup relations between resources and actions
            foreach (var resource in resourcesMapping.Values) resource.Actions.Clear();

            try
            {
                foreach (var resourceSyncDto in resources)
                {
                    // check if to add/update resource
                    if (!resourcesMapping.TryGetValue(resourceSyncDto.Name, out var resourceEntity))
                    {
                        resourceEntity = new Resource { Name = resourceSyncDto.Name, DisplayName = DisplayNameHelper.GetDisplayName(resourceSyncDto.Name), ListObjectsEndpoint = resourceSyncDto.ListObjectsEndpoint, Actions = new List<Models.Entities.Action>() };
                        _repository.Create(resourceEntity);
                        _repository.Reload(resourceEntity);

                        result.Resources.Added.Add(resourceSyncDto.Name);
                        resourcesMapping.Add(resourceSyncDto.Name, resourceEntity);
                    }
                    else if(resourceEntity.ListObjectsEndpoint != resourceSyncDto.ListObjectsEndpoint)
                    {
                        resourceEntity.ListObjectsEndpoint = resourceSyncDto.ListObjectsEndpoint;
                        result.Resources.Updated.Add(resourceSyncDto.Name);
                    }
                    resourcesUsed.Add(resourceSyncDto.Name);

                    // check if to add action
                    foreach (var actionName in resourceSyncDto.Actions)
                    {
                        if (actionName == AnyActionName) continue;
                        
                        if (!actionsMapping.TryGetValue(actionName, out var actionEntity))
                        {
                            var dto = new ActionRequestDto { Name = actionName, DisplayName = DisplayNameHelper.GetDisplayName(actionName) };
                            var actionDto = await _actionService.CreateAsync(dto);
                            result.Actions.Added.Add(actionName);

                            actionEntity = await _repositoryManager.Action.GetByKeyAsync(actionDto.Uuid);
                            actionsMapping.Add(actionName, actionEntity);
                        }
                        actionsUsed.Add(actionName);
                        resourceEntity.Actions.Add(actionEntity);
                    }
                }

                // check unused resources
                foreach (var resource in resourcesMapping.Values)
                {
                    if (!resourcesUsed.Contains(resource.Name))
                    {
                        _logger.LogWarning($"Resource '{resource.Name}' is not used and will be deleted.");

                        await _repository.DeleteAsync(resource.Uuid);
                        result.Resources.Removed.Add(resource.Name);
                    }
                }

                // check unused actions
                foreach (var action in actionsMapping.Values)
                {
                    if (!actionsUsed.Contains(action.Name))
                    {
                        _logger.LogWarning($"Action '{action.Name}' is not used and will be deleted.");

                        await _actionService.DeleteAsync(action.Uuid);
                        result.Actions.Removed.Add(action.Name);
                    }
                }

                // TODO: what to do with permissions that are linked to removed resource-action pair

                await _repositoryManager.SaveAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return result;
        }
    }
}
