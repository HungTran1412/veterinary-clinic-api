namespace VeterinaryClinic.Business;

public abstract record NotificationBaseModel
{
    public int Id { get; init; }
    public int UserId { get; init; }
    public string Title { get; init; }
    public string Message { get; init; }
    public string Type { get; init; }
    
    public bool IsRead { get; init; }
    public int? RelatedEntityId { get; init; }
    public string? RelatedEntityType { get; init; }
    
    public bool IsActive { get; init; } = true;

    public int Order { get; init; }
    public DateTime? CreatedDate { get; init; }
}

public record NotificationModel : NotificationBaseModel
{
    public string? UserFullName { get; init; }
    
}

public record NotificationFilterModel : BaseQueryFilterModel
{
    public string? Type { get; init; }
}