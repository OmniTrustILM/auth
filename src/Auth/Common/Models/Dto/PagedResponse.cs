using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Common.Models.Dto
{
    public record PagedResponse<TDto>
    {
        public List<TDto> Data { get; init; }

        public PagingMetadata Links { get; init; }
    }
}
