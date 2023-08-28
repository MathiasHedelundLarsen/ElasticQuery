using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace ElasticQuery.Internal
{
    public abstract class ElasticQuery
    {
        protected const string InMemoryQueryableExtensionMethodsRequiresUnreferencedCode = "Enumerating elastic queries can require unreferenced code because expressions referencing IQueryable extension methods can get rebound to IEnumerable extension methods. The IEnumerable extension methods could be trimmed causing the application to fail at runtime.";

        public abstract string IndexName { get; }
        public abstract Type ElementType { get; }

        internal ElasticQuery() { }        

        [RequiresUnreferencedCode(InMemoryQueryableExtensionMethodsRequiresUnreferencedCode)]
        internal static IQueryable Create(Type elementType, ElasticQueryProvider elasticQueryProvider, Expression expression)
        {
            Type queryType = typeof(ElasticQuery<>).MakeGenericType(elementType);

            var constructor = queryType.GetConstructor(BindingFlags.NonPublic, new Type[] { typeof(ElasticQueryProvider), typeof(Expression) })!;

            return (IQueryable)constructor.Invoke(new object[] { elasticQueryProvider, expression })!;
        }

        public static IQueryable Create(ElasticQueryProvider elasticQueryProvider, string indexName)
        {
            var elementType = elasticQueryProvider.GetAnonymousSchemaType(indexName);

            Type queryType = typeof(ElasticQuery<>).MakeGenericType(elementType);

            var constructor = queryType.GetConstructor(BindingFlags.NonPublic, new Type[] { typeof(ElasticQueryProvider), typeof(string) })!;

            return (IQueryable)constructor.Invoke(new object[] { elasticQueryProvider, indexName })!;
        }

        public static async Task<IQueryable> CreateAsync(ElasticQueryProvider elasticQueryProvider, string indexName)
        {
            var elementType = await elasticQueryProvider.GetAnonymousSchemaTypeAsync(indexName);

            Type queryType = typeof(ElasticQuery<>).MakeGenericType(elementType);

            var constructor = queryType.GetConstructor(BindingFlags.NonPublic, new Type[] { typeof(ElasticQueryProvider), typeof(string) })!;

            return (IQueryable)constructor.Invoke(new object[] { elasticQueryProvider, indexName })!;
        }
    }
}
