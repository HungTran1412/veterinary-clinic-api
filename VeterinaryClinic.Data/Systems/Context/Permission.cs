using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VeterinaryClinic.Shared;

namespace VeterinaryClinic.Data;

[Table("sys_permission")]
public class Permission : BaseEntity
{ 
    [Column("group_name")]
    public string GroupName { get; set; }

    [Column("code")]
    public string Code { get; set; }

    [Column("name")]
    public string Name { get; set; }
    
    /// <summary>
    /// Mo ta
    /// </summary>
    [Column("description"), MaxLength(1000)]
    public string? Description { get; set; }
}