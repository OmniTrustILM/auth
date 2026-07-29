namespace Auth.Common.Models.Dto
{
    public interface IQueryRequestDto
    {
        public int Page { get; }
        public int PageSize { get; }
        public string? SortBy { get; }
    }
}
