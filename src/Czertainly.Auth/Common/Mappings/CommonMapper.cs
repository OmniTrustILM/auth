using Czertainly.Auth.Common.Data;
using Czertainly.Auth.Common.Models.Dto;

namespace Czertainly.Auth.Common.Mappings
{
    public static class CommonMapper
    {
        /// <summary>
        /// Translates the wire level query request into repository query parameters. A leading '-' in SortBy requests
        /// descending order and the remainder is capitalized, because the sorting is applied through a dynamic
        /// <c>OrderBy</c> that addresses the entity property by its CLR (PascalCase) name.
        /// </summary>
        public static QueryStringParameters ToQueryStringParameters(this IQueryRequestDto dto)
        {
            var sortBy = dto.SortBy!;

            return new QueryStringParameters
            {
                Page = dto.Page,
                PageSize = dto.PageSize,
                SortBy = sortBy[0] == '-' ? char.ToUpper(sortBy[1]) + sortBy.Substring(2) : char.ToUpper(sortBy[0]) + sortBy.Substring(1),
                SortAscending = sortBy[0] != '-',
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
