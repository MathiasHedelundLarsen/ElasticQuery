namespace ElasticQuery
{
    public static class TypeExtensions
    {
        public static bool IsBasicType(this Type type, bool includeNullable)
        {
            if (includeNullable)
            {
                var underlying = Nullable.GetUnderlyingType(type);

                if (underlying != null)
                {
                    return IsBasicType(underlying, false);
                }
            }

            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(Guid);
        }
    }
}
