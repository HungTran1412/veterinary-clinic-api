using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business
{
    public class BaseQueryFilterModel
    {
        public string TextSearch { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int? UserId { get; set; }
        public bool? IsActive { get; set; }
        public string PropertyName { get; set; } = "CreatedDate";
        //asc - desc
        public string Ascending { get; set; } = "desc";
        public BaseQueryFilterModel()
        {
            PageNumber = QueryFilter.DefaultPageNumber;
            PageSize = QueryFilter.DefaultPageSize;
        }
    }
}
