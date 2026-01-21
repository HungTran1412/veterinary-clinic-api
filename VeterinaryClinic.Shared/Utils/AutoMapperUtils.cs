using System.Collections;
using AutoMapper;

namespace VeterinaryClinic.Shared;

public static class AutoMapperUtils
{
    private static IMapper _mapper;

    public static void Configure(IMapper mapper)
    {
        _mapper = mapper;
    }

    public static TDestination AutoMap<TSource, TDestination>(TSource source)
    {
        // Nếu TSource và TDestination là danh sách, ta cần map kiểu phần tử bên trong
        var sourceType = typeof(TSource);
        var destType = typeof(TDestination);

        if (typeof(IEnumerable).IsAssignableFrom(sourceType) && sourceType != typeof(string) &&
            typeof(IEnumerable).IsAssignableFrom(destType) && destType != typeof(string))
        {
            var sourceElementType = sourceType.IsArray 
                ? sourceType.GetElementType() 
                : sourceType.GetGenericArguments().FirstOrDefault();
                
            var destElementType = destType.IsArray 
                ? destType.GetElementType() 
                : destType.GetGenericArguments().FirstOrDefault();

            if (sourceElementType != null && destElementType != null)
            {
                var config = new MapperConfiguration(cfg => cfg.CreateMap(sourceElementType, destElementType));
                var mapper = config.CreateMapper();
                return mapper.Map<TDestination>(source);
            }
        }

        // Trường hợp object đơn lẻ
        var configSingle = new MapperConfiguration(cfg => cfg.CreateMap<TSource, TDestination>());
        var mapperSingle = configSingle.CreateMapper();
        return mapperSingle.Map<TDestination>(source);
    }
}