using Czertainly.Auth.Common.Data;
using Czertainly.Auth.Common.Exceptions;
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
        /// <remarks>
        /// An absent SortBy - null, empty or whitespace-only - is a legitimate request for no ordering and yields a null
        /// sort field, which the repository reads as "do not order". A SortBy that is present but cannot name a property,
        /// which is only a '-' with nothing but whitespace behind it, is malformed rather than absent and is rejected.
        /// </remarks>
        /// <exception cref="InvalidFormatException">
        /// SortBy carries the descending prefix but no property name behind it.
        /// </exception>
        public static QueryStringParameters ToQueryStringParameters(this IQueryRequestDto dto)
        {
            var sortBy = dto.SortBy ?? string.Empty;
            var descending = sortBy.StartsWith('-');
            var sortField = descending ? sortBy[1..] : sortBy;

            // Line endings are stripped because the message reaches the log through the exception middleware.
            if (descending && string.IsNullOrWhiteSpace(sortField))
            {
                throw new InvalidFormatException($"Invalid sortBy value '{sortBy.ReplaceLineEndings(string.Empty)}': the descending prefix '-' must be followed by a property name.");
            }

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
