using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Common.Data
{
    public interface IQueryStringParameters
    {
        // pagination
        public int Page { get; set; }
        public int PageSize { get; set; }

        // sorting
        public string Sort { get; set; }

        public Array Filters { get; set; }
    }
}
