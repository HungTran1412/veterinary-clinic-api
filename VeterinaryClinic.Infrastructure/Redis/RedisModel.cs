namespace VeterinaryClinic.Business.Core;

public record RedisInCreaseModel
{
    public string Key { get; set; }
    public long Value { get; set; }
}

public record RedisModel
{
    public string Key { get; set; }
    public string Value { get; set; }
    public int Second { get; set; }
}
    
public record RedisTModel<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public int Second { get; set; }
}
    
