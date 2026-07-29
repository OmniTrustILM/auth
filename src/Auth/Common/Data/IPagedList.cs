using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Common.Data
{
    public interface IPagedList : IList
    {
        int CurrentPage { get; }
        int TotalPages { get; }
        int PageSize { get; }
        int TotalCount { get; }
        bool HasNext { get; }
        bool HasPrevious { get; }

    }
}
