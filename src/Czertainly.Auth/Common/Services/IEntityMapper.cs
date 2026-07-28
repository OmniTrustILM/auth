using Czertainly.Auth.Common.Models.Dto;
using Czertainly.Auth.Common.Models.Entities;

namespace Czertainly.Auth.Common.Services
{
    /// <summary>
    /// Entity specific mapping operations needed by <see cref="CrudService{TEntity, TResponseDto, TDetailResponseDto}"/>.
    /// Implementations are stateless adapters over the hand-written static mappers, so the generic CRUD base does not
    /// need to know any concrete entity or DTO type.
    /// </summary>
    public interface IEntityMapper<TEntity, TResponseDto, TDetailResponseDto>
        where TEntity : class, IBaseEntity, new()
        where TResponseDto : ICrudResponseDto, new()
        where TDetailResponseDto : ICrudResponseDto, new()
    {
        /// <summary>
        /// Builds a new entity from a create request. Throws <see cref="ArgumentException"/> when the runtime type of
        /// the request is not the one the entity is created from.
        /// </summary>
        TEntity ToEntity(ICrudRequestDto dto);

        /// <summary>
        /// Copies the updatable members of an update request onto an already loaded entity. Throws
        /// <see cref="ArgumentException"/> when the runtime type of the request is not the one the entity is updated from.
        /// </summary>
        void ApplyUpdate(ICrudRequestDto dto, TEntity entity);

        TResponseDto ToDto(TEntity entity);

        TDetailResponseDto ToDetailDto(TEntity entity);
    }
}
