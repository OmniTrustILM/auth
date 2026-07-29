using System.Text.Json.Serialization;

namespace Auth.Common.Models.Dto
{
    public record QueryRequestDto : IQueryRequestDto
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 1000;

        public string? SortBy { get; set; } = "uuid";
    }
}
