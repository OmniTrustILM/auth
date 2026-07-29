using Auth.Common.Models.Dto;
using Auth.Common.Models.Entities;

namespace Auth.Common.Services
{
    /// <summary>
    /// Entity specific mapping operations needed by <see cref="CrudService{TEntity, TResponseDto, TDetailResponseDto}"/>.
    /// Implementations are stateless adapters over the hand-written static mappers, so the generic CRUD base does not
    /// need to know any concrete entity or DTO type.
    /// </summary>
    /// <remarks>
    /// The two response DTOs are covariant because they only ever flow out of this interface. TEntity cannot be, since
    /// <see cref="ApplyUpdate"/> consumes it while <see cref="ToEntity"/> produces it. Neither response DTO carries a
    /// <c>new()</c> constraint: nothing here constructs them, so requiring instantiability would only narrow the type
    /// arguments a covariant conversion can reach.
    /// </remarks>
    public interface IEntityMapper<TEntity, out TResponseDto, out TDetailResponseDto>
        where TEntity : class, IBaseEntity, new()
        where TResponseDto : ICrudResponseDto
        where TDetailResponseDto : ICrudResponseDto
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
