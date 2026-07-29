using Auth.Common.Models.Dto;
using Auth.Common.Services;
using Auth.Models.Dto;
using Auth.Models.Entities;

namespace Auth.Models.Mappings
{
    public static class ResourceMapper
    {
        /// <summary>
        /// A resource create request carries exactly the members an update request carries, so the field list lives in
        /// <see cref="ApplyTo"/> alone.
        /// </summary>
        public static Resource ToEntity(this ResourceRequestDto dto)
        {
            var resource = new Resource();
            dto.ApplyTo(resource);

            return resource;
        }

        public static void ApplyTo(this ResourceRequestDto dto, Resource resource)
        {
            resource.Name = dto.Name!;
            resource.DisplayName = dto.DisplayName!;
            resource.ListObjectsEndpoint = dto.ListObjectsEndpoint;
        }

        public static ResourceDto ToDto(this Resource resource)
        {
            return new ResourceDto
            {
                Uuid = resource.Uuid,
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                ListObjectsEndpoint = resource.ListObjectsEndpoint,
            };
        }

        public static ResourceDetailDto ToDetailDto(this Resource resource)
        {
            return new ResourceDetailDto
            {
                Uuid = resource.Uuid,
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                ListObjectsEndpoint = resource.ListObjectsEndpoint,
                // Actions is loaded through the repository's detail includes or an explicit Include on every path
                // reaching this mapper - see UserMapper.ToDetailDto for the same reasoning.
                Actions = resource.Actions?.Select(action => action.ToDto()).ToList() ?? [],
            };
        }
    }

    public sealed class ResourceEntityMapper : IEntityMapper<Resource, ResourceDto, ResourceDetailDto>
    {
        public static readonly ResourceEntityMapper Instance = new();

        public Resource ToEntity(ICrudRequestDto dto)
        {
            if (dto is not ResourceRequestDto resourceRequestDto) throw new ArgumentException($"Cannot create resource from '{dto.GetType().Name}'.", nameof(dto));

            return ResourceMapper.ToEntity(resourceRequestDto);
        }

        public void ApplyUpdate(ICrudRequestDto dto, Resource entity)
        {
            if (dto is not ResourceRequestDto resourceRequestDto) throw new ArgumentException($"Cannot update resource from '{dto.GetType().Name}'.", nameof(dto));

            ResourceMapper.ApplyTo(resourceRequestDto, entity);
        }

        public ResourceDto ToDto(Resource entity) => ResourceMapper.ToDto(entity);

        public ResourceDetailDto ToDetailDto(Resource entity) => ResourceMapper.ToDetailDto(entity);
    }
}
