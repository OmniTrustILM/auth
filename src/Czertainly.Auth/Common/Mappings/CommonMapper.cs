using Czertainly.Auth.Common.Data;
using Czertainly.Auth.Common.Models.Dto;

namespace Czertainly.Auth.Common.Mappings
{
    public static class CommonMapper
    {
        /// <summary>
        /// Translates the wire level query request into repository query parameters. A leading '-' in SortBy requests
        /// descending order and the remainder is capitalized, because the sorting is applied through a dynamic
        /// <c>OrderBy</c> that addresses the entity property by its CLR (PascalCase) name. A SortBy that names no
        /// property at all - null, empty, whitespace, or a bare '-' - yields a null sort field, which the repository
        /// reads as "do not order".
        /// </summary>
        public static QueryStringParameters ToQueryStringParameters(this IQueryRequestDto dto)
        {
            var sortBy = dto.SortBy ?? string.Empty;
            var descending = sortBy.StartsWith('-');
            var sortField = descending ? sortBy[1..] : sortBy;

            return new QueryStringParameters
            {
                Page = dto.Page,
                PageSize = dto.PageSize,
                SortBy = string.IsNullOrWhiteSpace(sortField) ? null : char.ToUpper(sortField[0]) + sortField[1..],
                SortAscending = !descending,
            };
        }

        public static PagingMetadata ToPagingMetadata(this IPagedList pagedList)
        {
            return new PagingMetadata
            {
                CurrentPage = pagedList.CurrentPage,
                PageSize = pagedList.PageSize,
                TotalCount = pagedList.TotalCount,
                TotalPages = pagedList.TotalPages,
                HasPrevious = pagedList.HasPrevious,
                HasNext = pagedList.HasNext,
            };
        }
    }
}
