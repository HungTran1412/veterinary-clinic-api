namespace VeterinaryClinic.Shared
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DataColumnAttribute : Attribute
    {
        public string ColumnName { get; }

        public DataColumnAttribute(string columnName)
        {
            ColumnName = columnName;
        }
    }
}
