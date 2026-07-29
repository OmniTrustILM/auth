using Auth.Common.Models.Dto;

namespace Auth.Models.Dto
{
    public record SyncResourcesResponseDto
    {
        public SyncResourcesDto Resources { get; init; } = new SyncResourcesDto();
        public SyncActionsDto Actions { get; init; } = new SyncActionsDto();

    }

    public record SyncResourcesDto
    {
        public List<string> Added { get; init; } = new List<string>();
        public List<string> Updated { get; init; } = new List<string>();
        public List<string> Removed { get; init; } = new List<string>();
    }

    public record SyncActionsDto
    {
        public List<string> Added { get; init; } = new List<string>();
        public List<string> Removed { get; init; } = new List<string>();

    }
}
