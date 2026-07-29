using Auth.Common.Exceptions;
using Auth.Data.Contracts;
using Auth.Models.Dto;
using Auth.Models.Entities;

namespace Auth.Services
{
    public class PermissionService : IPermissionService
    {
        protected readonly ILogger _logger;
        private readonly IPermissionRepository _repository;
        private readonly IRepositoryManager _repositoryManager;

        public PermissionService(IRepositoryManager repositoryManager, ILogger<PermissionService> logger)
        {
            _logger = logger;
            _repositoryManager = repositoryManager;
            _repository = repositoryManager.Permission;
        }

        #region Retrieving permissions

        public async Task<SubjectPermissionsDto> GetRolePermissionsAsync(Guid roleUuid)
        {
            await CheckRole(roleUuid, false);

            var permissions = await _repository.GetRolePermissions(roleUuid);

            var rolePermissions = MergePermissions(permissions);
            await AddVerbosePermissions(rolePermissions);

            return rolePermissions;
        }

        public async Task<ResourcePermissionsDto> GetRoleResourcesPermissionsAsync(Guid roleUuid, Guid resourceUuid)
        {
            await CheckRole(roleUuid, false);

            var permissions = await _repository.GetRoleResourcePermissions(roleUuid, resourceUuid);
            var subjectPermissions = MergePermissions(permissions);

            var resource = await _repositoryManager.Resource.GetByKeyAsync(resourceUuid);
            if (subjectPermissions.Resources.Count != 0)
            {
                var resourcePermissions = subjectPermissions.Resources[0];
                AddVerboseResourcePermissions(resourcePermissions, resource);
                return resourcePermissions;
            }

            return new ResourcePermissionsDto { Name = resource.Name, AllowAllActions = subjectPermissions.AllowAllResources };
        }

        public async Task<List<ObjectPermissionsDto>> GetRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid)
        {
            var resourcePermissions = await GetRoleResourcesPermissionsAsync(roleUuid, resourceUuid);
            return resourcePermissions.Objects;
        }

        public async Task<SubjectPermissionsDto> GetUserPermissionsAsync(Guid userUuid)
        {
            var user = await _repositoryManager.User.GetByKeyAsync(userUuid);

            var permissions = await _repository.GetUserPermissions(userUuid);

            return MergePermissions(permissions);
        }

        private async Task AddVerbosePermissions(SubjectPermissionsDto subjectPermissions)
        {
            var allResourcesMap = await _repositoryManager.Resource.GetResourcesWithActionsMap();

            foreach (var resourcePermissions in subjectPermissions.Resources)
            {
                if (!allResourcesMap.TryGetValue(resourcePermissions.Name, out var resource)) _logger.LogWarning($"Missing resource {resourcePermissions.Name} that is used in merging permissions");
                else AddVerboseResourcePermissions(resourcePermissions, resource);

                allResourcesMap.Remove(resourcePermissions.Name);
            }
        }

        private void AddVerboseResourcePermissions(ResourcePermissionsDto resourcePermissions, Resource resource)
        {
            foreach (var objectPermissions in resourcePermissions.Objects)
            {
                foreach (var action in resource.Actions)
                {
                    if ((resourcePermissions.AllowAllActions || resourcePermissions.Actions.Contains(action.Name))
                        && !objectPermissions.Deny.Contains(action.Name) && !objectPermissions.Allow.Contains(action.Name))
                        objectPermissions.Allow.Add(action.Name);
                }
                objectPermissions.Allow.Sort();
            }
        }

        #endregion

        #region Updating permissions

        public async Task<SubjectPermissionsDto> SaveRolePermissionsAsync(Guid roleUuid, RolePermissionsRequestDto rolePermissions)
        {
            var role = await CheckRole(roleUuid, true);
            _logger.LogInformation($"Saving permissions of role '{role.Name}:{roleUuid}'");

            var transaction = await _repositoryManager.BeginTransactionAsync();

            try
            {
                _repository.DeleteRolePermissionsWithoutObjects(roleUuid);

                var resourcesMapping = await _repositoryManager.Resource.GetDictionaryMap(r => r.Name);
                var actionsMapping = await _repositoryManager.Action.GetDictionaryMap(a => a.Name);

                if (rolePermissions.AllowAllResources) _repository.Create(new Permission { RoleUuid = roleUuid, IsAllowed = true });
                if (rolePermissions.Resources != null)
                {
                    foreach (var resourcePermissions in rolePermissions.Resources)
                    {
                        if (!resourcesMapping.TryGetValue(resourcePermissions.Name, out var resource)) throw new EntityNotFoundException($"Unknown resource '{resourcePermissions.Name}'");

                        var resourceUuid = resource.Uuid;
                        if (resourcePermissions.AllowAllActions) _repository.Create(new Permission { RoleUuid = roleUuid, ResourceUuid = resourceUuid, IsAllowed = true });
                        else if (resourcePermissions.Actions != null)
                        {
                            foreach (var actionName in resourcePermissions.Actions)
                            {
                                if (!actionsMapping.TryGetValue(actionName, out var action)) throw new EntityNotFoundException($"Unknown action '{actionName}'");

                                var actionUuid = action.Uuid;
                                _repository.Create(new Permission { RoleUuid = roleUuid, ResourceUuid = resourceUuid, ActionUuid = actionUuid, IsAllowed = true });
                            }
                        }

                        if (resourcePermissions.Objects != null)
                        {
                            _repository.DeleteRoleResourceObjectsPermissions(roleUuid, resourceUuid);
                            if (resourcePermissions.Objects.Count > 0 && string.IsNullOrEmpty(resource.ListObjectsEndpoint)) throw new InvalidActionException($"Cannot save object permissions. Resource '{resource.DisplayName}' does not support object access permissions");
                            var resourceActions = resourcePermissions.AllowAllActions ? null : (resourcePermissions.Actions ?? new List<string>());
                            foreach (var objectPermissions in resourcePermissions.Objects)
                            {
                                AddRoleResourceObjectPermissions(roleUuid, resourceUuid, objectPermissions, resourceActions, actionsMapping);
                            }
                        }
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

            return await GetRolePermissionsAsync(roleUuid);
        }

        public async Task SaveRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid, List<ObjectPermissionsRequestDto> objectsPermissions)
        {
            var role = await CheckRole(roleUuid, true);
            var resource = await _repositoryManager.Resource.GetByKeyAsync(resourceUuid);
            _logger.LogInformation($"Saving permissions of role '{role.Name}:{roleUuid}' for resource {resource.Name}");

            if (String.IsNullOrEmpty(resource.ListObjectsEndpoint)) throw new InvalidActionException($"Cannot save object permissions. Resource '{resource.DisplayName}' does not support object access permissions");

            var transaction = await _repositoryManager.BeginTransactionAsync();
            try
            {
                _repository.DeleteRoleResourceObjectsPermissions(roleUuid, resourceUuid);

                var actionsMapping = await _repositoryManager.Action.GetDictionaryMap(a => a.Name);
                var resourcePermissions = await GetRoleResourcesPermissionsAsync(roleUuid, resourceUuid);
                foreach (var objectPermissions in objectsPermissions)
                {
                    AddRoleResourceObjectPermissions(roleUuid, resourceUuid, objectPermissions, resourcePermissions.AllowAllActions ? null : resourcePermissions.Actions, actionsMapping);
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

        public async Task SaveRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid, ObjectPermissionsRequestDto objectPermissions)
        {
            var role = await CheckRole(roleUuid, true);
            var resource = await _repositoryManager.Resource.GetByKeyAsync(resourceUuid);
            _logger.LogInformation($"Saving permissions of role '{role.Name}:{roleUuid}' for resource {resource.Name} and its object {objectUuid}");

            if (String.IsNullOrEmpty(resource.ListObjectsEndpoint)) throw new InvalidActionException($"Cannot save object permissions. Resource '{resource.DisplayName}' does not support object access permissions");

            var transaction = await _repositoryManager.BeginTransactionAsync();
            try
            {
                _repository.DeleteRoleResourceObjectPermissions(roleUuid, resourceUuid, objectUuid);
                var actionsMapping = await _repositoryManager.Action.GetDictionaryMap(a => a.Name);
                var resourcePermissions = await GetRoleResourcesPermissionsAsync(roleUuid, resourceUuid);
                AddRoleResourceObjectPermissions(roleUuid, resourceUuid, objectPermissions, resourcePermissions.AllowAllActions ? null : resourcePermissions.Actions, actionsMapping);

                await _repositoryManager.SaveAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid)
        {
            var role = await CheckRole(roleUuid, true);
            var resource = await _repositoryManager.Resource.GetByKeyAsync(resourceUuid);
            _logger.LogInformation($"Deleting permissions of role '{role.Name}:{roleUuid}' for resource {resource.Name} and its object {objectUuid}");

            _repository.DeleteRoleResourceObjectPermissions(roleUuid, resourceUuid, objectUuid);

            await _repositoryManager.SaveAsync();
        }

        #endregion

        private async Task<Role> CheckRole(Guid roleUuid, bool checkSystemRole)
        {
            var role = await _repositoryManager.Role.GetByKeyAsync(roleUuid);
            //if (checkSystemRole && role.SystemRole) throw new InvalidActionException("Cannot update system role permissions");

            return role;
        }

        private void AddRoleResourceObjectPermissions(Guid roleUuid, Guid resourceUuid, ObjectPermissionsRequestDto objectPermissions, List<string>? resourceActions, Dictionary<string, Models.Entities.Action> actionsMapping)
        {
            var allowActions = resourceActions == null || objectPermissions.Allow == null ? Enumerable.Empty<string>() : objectPermissions.Allow.Where(a => !resourceActions.Contains(a));
            foreach (var allowAction in allowActions)
            {
                if (!actionsMapping.TryGetValue(allowAction, out var action)) throw new EntityNotFoundException($"Unknown action '{allowAction}'");
                var actionUuid = action.Uuid;

                _repository.Create(new Permission
                {
                    RoleUuid = roleUuid,
                    ResourceUuid = resourceUuid,
                    ActionUuid = actionUuid,
                    ObjectUuid = objectPermissions.Uuid,
                    ObjectName = objectPermissions.Name,
                    IsAllowed = true
                });
            }

            if (objectPermissions.Deny != null)
            {
                foreach (var denyAction in objectPermissions.Deny)
                {
                    if (!actionsMapping.TryGetValue(denyAction, out var action)) throw new EntityNotFoundException($"Unknown action '{denyAction}'");
                    var actionUuid = action.Uuid;

                    _repository.Create(new Permission
                    {
                        RoleUuid = roleUuid,
                        ResourceUuid = resourceUuid,
                        ActionUuid = actionUuid,
                        ObjectUuid = objectPermissions.Uuid,
                        ObjectName = objectPermissions.Name,
                        IsAllowed = false
                    });
                }
            }
        }

        private SubjectPermissionsDto MergePermissions(IEnumerable<Permission> permissions)
        {
            var result = new SubjectPermissionsDto();

            var resourcesMapping = new SortedDictionary<string, ResourcePermissionsDto>(StringComparer.Ordinal);
            var resourceActionsMapping = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var resourceObjectsMapping = new Dictionary<string, HashSet<Guid>>(StringComparer.Ordinal);
            var objectsNamesMapping = new Dictionary<Guid, string?>();
            var objectAllowedActionsMapping = new Dictionary<Guid, HashSet<string>>();
            var objectDeniedActionsMapping = new Dictionary<Guid, HashSet<string>>();

            foreach (var permission in permissions)
            {
                // if some role allows all resources and other no, allow all and let overriding on lower level
                if (!permission.ResourceUuid.HasValue)
                {
                    result.AllowAllResources = result.AllowAllResources || permission.IsAllowed;
                    continue;
                }

                var resourceName = permission.Resource.Name;
                if (!resourcesMapping.TryGetValue(resourceName, out var resource)) resourcesMapping.Add(resourceName, resource = new ResourcePermissionsDto { Name = resourceName });
                if (!permission.ActionUuid.HasValue)
                {
                    // if some role allows all resource actions and other no, allow all and let overriding on lower level
                    resource.AllowAllActions = resource.AllowAllActions || permission.IsAllowed;
                    continue;
                }

                var actionName = permission.Action.Name;
                if (!resourceActionsMapping.TryGetValue(resourceName, out var resourceActions)) resourceActionsMapping.Add(resourceName, resourceActions = new HashSet<string>(StringComparer.Ordinal));
                if (!permission.ObjectUuid.HasValue)
                {
                    // deny action on resource for all object not supported
                    if (!permission.IsAllowed) continue;

                    // add action to resource allowed actions
                    resourceActions.Add(actionName);
                    continue;
                }

                // add object UUID to resource object permissions
                var objectUuid = permission.ObjectUuid.Value;
                if (!resourceObjectsMapping.TryGetValue(resourceName, out var resourceObjects)) resourceObjectsMapping.Add(resourceName, resourceObjects = new HashSet<Guid>());
                if (!objectsNamesMapping.ContainsKey(objectUuid)) objectsNamesMapping.Add(objectUuid, permission.ObjectName);
                resourceObjects.Add(objectUuid);

                // add action to object allow/deny actions
                var actionsMapping = permission.IsAllowed ? objectAllowedActionsMapping : objectDeniedActionsMapping;
                if (!actionsMapping.TryGetValue(objectUuid, out var objectActions)) actionsMapping.Add(objectUuid, objectActions = new HashSet<string>(StringComparer.Ordinal));
                objectActions.Add(actionName);
            }

            // merge permissions and add it to result DTO
            foreach (var resourcePermissionsDto in resourcesMapping.Values)
            {
                // if resource allows all actions, individual actions listing is redundant
                HashSet<string>? resourceActions = null;
                if (!resourcePermissionsDto.AllowAllActions && resourceActionsMapping.TryGetValue(resourcePermissionsDto.Name, out resourceActions))
                {
                    resourcePermissionsDto.Actions.AddRange(resourceActions);
                    resourcePermissionsDto.Actions.Sort();
                }

                // merge resource objects permissions if there are some defined
                if (resourceObjectsMapping.TryGetValue(resourcePermissionsDto.Name, out var resourceObjects))
                {
                    foreach (var objectUuid in resourceObjects)
                    {
                        var objectPermissionsDto = new ObjectPermissionsDto { Uuid = objectUuid, Name = objectsNamesMapping[objectUuid] };

                        // first add denied actions
                        if (objectDeniedActionsMapping.TryGetValue(objectUuid, out var deniedActions))
                        {
                            objectPermissionsDto.Deny.AddRange(deniedActions);
                            objectPermissionsDto.Deny.Sort();
                        }

                        // then filter allowed actions
                        if (!resourcePermissionsDto.AllowAllActions && objectAllowedActionsMapping.TryGetValue(objectUuid, out var allowedActions))
                        {
                            foreach (var allowedAction in allowedActions)
                            {
                                // if not already allowed in resource actions or not already denied, add allowed action
                                if ((resourceActions == null || !resourceActions.Contains(allowedAction))
                                    && (deniedActions == null || !deniedActions.Contains(allowedAction))) objectPermissionsDto.Allow.Add(allowedAction);
                            }
                        }

                        resourcePermissionsDto.Objects.Add(objectPermissionsDto);
                    }
                }

                result.Resources.Add(resourcePermissionsDto);
            }

            return result;
        }
    }
}
