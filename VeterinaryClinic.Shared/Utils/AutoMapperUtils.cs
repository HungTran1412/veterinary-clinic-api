using AutoMapper;
using AutoMapper.EquivalencyExpression;
using System.Collections.Generic;

namespace VeterinaryClinic.Shared
{
    public static class AutoMapperUtils
    {
        private static IMapper GetMapper<TSource, TDestination>()
        {
            var config = new MapperConfiguration(cfg => {
                cfg.AddCollectionMappers();
                cfg.AllowNullCollections = true;
                cfg.AllowNullDestinationValues = true;
                cfg.CreateMap<TSource, TDestination>(MemberList.None);
            });
             
            IMapper mapper = new Mapper(config);
            return mapper;
        }
        
        private static IMapper GetMapper<TSource, TDestination>(string idString)
        {
            var config = new MapperConfiguration(cfg => {
                cfg.AddCollectionMappers();
                cfg.AllowNullCollections = true;
                cfg.AllowNullDestinationValues = true;
                cfg.CreateMap<TSource, TDestination>(MemberList.None)
                   .ForSourceMember(idString, s => s.DoNotValidate())
                   .ForMember(idString, s => s.Ignore());
            });
            IMapper mapper = new Mapper(config);
            return mapper;
        }

        #region Single
        public static TDestination AutoMap<TSource, TDestination>(TSource source)
        {
            if (source == null) return default;
            var mapper = GetMapper<TSource, TDestination>();
            TDestination dest = mapper.Map<TDestination>(source);
            return dest;
        }

        public static TDestination AutoMap<TSource, TDestination>(TSource source, string idString)
        {
            if (source == null) return default;
            var mapper = GetMapper<TSource, TDestination>(idString);
            TDestination dest = mapper.Map<TDestination>(source);
            return dest;
        }
        
        public static TDestination AutoMap<TSource, TDestination>(TSource source, TDestination dest)
        {
            if (source == null) return dest;
            var mapper = GetMapper<TSource, TDestination>();
            dest = mapper.Map(source, dest);
            return dest;
        }

        public static TDestination AutoMap<TSource, TDestination>(TSource source, TDestination dest, string idString)
        {
            if (source == null) return dest;
            var mapper = GetMapper<TSource, TDestination>(idString);
            dest = mapper.Map(source, dest);
            return dest;
        }
        #endregion

        #region List
        public static List<TDestination> AutoMap<TSource, TDestination>(List<TSource> source)
        {
            if (source == null) return new List<TDestination>();
            var mapper = GetMapper<TSource, TDestination>();
            List<TDestination> dest = mapper.Map<List<TDestination>>(source);
            return dest;
        }

        public static List<TDestination> AutoMap<TSource, TDestination>(List<TSource> source, string idString)
        {
            if (source == null) return new List<TDestination>();
            var mapper = GetMapper<TSource, TDestination>(idString);
            List<TDestination> dest = mapper.Map<List<TDestination>>(source);
            return dest;
        }
        
        public static List<TDestination> AutoMap<TSource, TDestination>(List<TSource> source, List<TDestination> dest)
        {
            if (source == null) return dest;
            var mapper = GetMapper<TSource, TDestination>();
            dest = mapper.Map(source, dest);
            return dest;
        }

        public static List<TDestination> AutoMap<TSource, TDestination>(List<TSource> source, List<TDestination> dest, string idString)
        {
            if (source == null) return dest;
            var mapper = GetMapper<TSource, TDestination>(idString);
            dest = mapper.Map(source, dest);
            return dest;
        }
        #endregion
    }
}
