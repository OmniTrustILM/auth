using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Common.Data
{
    public class QueryStringParameters
    {
        const int MaxPageSize = 1000;

        private int _pageSize = 10;

        // pagination
        public int Page { get; set; } = 1;
        public int PageSize
        {
            get
            {
                return _pageSize;
            }
            set
            {
                _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
            }
        }

        // sorting
        public string? SortBy { get; set; }

        public bool SortAscending { get; set; }
    }
}
