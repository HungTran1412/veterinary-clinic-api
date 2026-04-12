using VeterinaryClinic.Data;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Business;

public abstract record PermissionBaseModel
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string GroupName { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public int IdPhanHe { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public record PermissionModel : PermissionBaseModel
{
        
}   
    
public record CreatePermissionModel : PermissionModel
{
    public Guid? CreatedUserId { get; set; }        
}

public record UpdatePermissionModel : PermissionModel
{
    public int? ModifiedUserId { get; set; }

    public void UpdateEntity(Permission entity)
    {
        entity.Name = this.Name;
        entity.GroupName = this.GroupName;
        entity.IsActive = this.IsActive;
        entity.Description = this.Description;
        entity.Order = this.Order;
        entity.ModifiedDate = DateTime.Now;
        entity.ModifiedUserId = this.ModifiedUserId;
    }
}

public record PermissionSelectItemModel : SelectItemModel
{
    public int IdPhanHe { get; set; }
    public string PhanHe { get; set; }
    public string MoTaPhanHe { get; set; }
    public string GroupName { get; set; }
}

public record PermissionQueryFilter
{
    public string TextSearch { get; set; }
    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }
    public bool? IsActive { get; set; }
    public string PropertyName { get; set; } = "CreatedDate";
    //asc - desc
    public string Ascending { get; set; } = "desc";
    public PermissionQueryFilter()
    {
        PageNumber = QueryFilter.DefaultPageNumber;
        PageSize = QueryFilter.DefaultPageSize;
    }
}