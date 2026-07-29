using Czertainly.Auth.Common.Models.Dto;
using Czertainly.Auth.Common.Services;
using Czertainly.Auth.Models.Dto;
using ActionEntity = Czertainly.Auth.Models.Entities.Action;

namespace Czertainly.Auth.Models.Mappings
{
    public static class ActionMapper
    {
        /// <summary>
        /// An action create request carries exactly the members an update request carries, so the field list lives in
        /// <see cref="ApplyTo"/> alone.
        /// </summary>
        public static ActionEntity ToEntity(this ActionRequestDto dto)
        {
            var action = new ActionEntity();
            dto.ApplyTo(action);

            return action;
        }

        public static void ApplyTo(this ActionRequestDto dto, ActionEntity action)
        {
            action.Name = dto.Name!;
            action.DisplayName = dto.DisplayName!;
        }

        public static ActionDto ToDto(this ActionEntity action)
        {
            return new ActionDto
            {
                Uuid = action.Uuid,
                Name = action.Name,
                DisplayName = action.DisplayName,
            };
        }
    }

    /// <summary>
    /// Actions have no separate detail representation, so the list and the detail response are the same DTO.
    /// </summary>
    public sealed class ActionEntityMapper : IEntityMapper<ActionEntity, ActionDto, ActionDto>
    {
        public static readonly ActionEntityMapper Instance = new();

        public ActionEntity ToEntity(ICrudRequestDto dto)
        {
            if (dto is not ActionRequestDto actionRequestDto) throw new ArgumentException($"Cannot create action from '{dto.GetType().Name}'.", nameof(dto));

            return ActionMapper.ToEntity(actionRequestDto);
        }

        public void ApplyUpdate(ICrudRequestDto dto, ActionEntity entity)
        {
            if (dto is not ActionRequestDto actionRequestDto) throw new ArgumentException($"Cannot update action from '{dto.GetType().Name}'.", nameof(dto));

            ActionMapper.ApplyTo(actionRequestDto, entity);
        }

        public ActionDto ToDto(ActionEntity entity) => ActionMapper.ToDto(entity);

        public ActionDto ToDetailDto(ActionEntity entity) => ActionMapper.ToDto(entity);
    }
}
