using System.Runtime.Serialization;

namespace Auth.Common.Data
{
    public interface IQueryFilter
    {
        public string Column { get; set; }
        public string Condition { get; set; }
        public object Value { get; set; }
    }
}
