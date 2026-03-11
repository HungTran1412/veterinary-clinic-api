using System.Linq.Expressions;
using System.Reflection;

namespace VeterinaryClinic.Business
{
    public static class LinqExtension
    {
        public static IQueryable<T> OrderByField<T>(this IQueryable<T> q, string sortField, string ascending)
        {
            if (string.IsNullOrEmpty(sortField))
            {
                return q;
            }
            var param = Expression.Parameter(typeof(T), "p");
            var prop = Expression.Property(param, sortField);
            var exp = Expression.Lambda(prop, param);
            string method = ascending == "asc" ? "OrderBy" : "OrderByDescending";
            Type[] types = new Type[] { q.ElementType, exp.Body.Type };
            var mce = Expression.Call(typeof(Queryable), method, types, q.Expression, exp);
            return q.Provider.CreateQuery<T>(mce);
        }
        public static IQueryable<T> Filter<T>(this IQueryable<T> q, List<FilterObject> filters)
        {
            for (var i = 0; i < filters.Count(); i++)
            {
                var element = filters[i];
                PropertyInfo getter = typeof(T).GetProperty(element.PropertyName);
                if (getter == null) continue;
                q = q.Where(c => element.Values.Contains(getter.GetValue(c, null).ToString()));
            }
            return q;
        }
        public static List<T2> GroupBy<T1, T2>(this List<T1> list, string propertyName, int countMin)
        {
            var list2 = list.GroupBy(c => c.GetType().GetProperty(propertyName).GetValue(c, null)).Where(g => g.Count() > countMin).Select(g => (T2)g.Key).ToList();
            return list2;
        }
    }
    public class FilterObject
    {
        public string PropertyName { get; set; }
        public List<string> Values { get; set; }
    }  
}