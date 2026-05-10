using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;


namespace VeterinaryClinic.Shared
{
    public static class DataTableHelper
    {
        public static T ToObject<T>(this DataRow row) where T : new()
        {
            T obj = new T();
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<DataColumnAttribute>();
                if (attr != null && row.Table.Columns.Contains(attr.ColumnName))
                {
                    var value = row[attr.ColumnName];
                    if (value != DBNull.Value)
                    {
                        // Get the underlying type if the property is nullable
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        
                        // Convert to the target type
                        var convertedValue = Convert.ChangeType(value, targetType);
                        
                        // Set the property value
                        prop.SetValue(obj, convertedValue, null);
                    }
                }
            }
            return obj;
        }

        public static List<T> ToList<T>(this DataTable table) where T : new()
        {
            return table.AsEnumerable().Select(row => row.ToObject<T>()).ToList();
        }
    }
}
