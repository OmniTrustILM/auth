using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Common.Models.Dto
{
    public record PagingMetadata
    {
        public int CurrentPage { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
        public bool HasPrevious { get; init; }
        public bool HasNext { get; init; }

    }
}
