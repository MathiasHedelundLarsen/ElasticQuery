using ElasticQuery.ExpressionsVisitors;
using ElasticQuery.Internal;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace ElasticQuery
{
    public class ElasticQuery<T> : Internal.ElasticQuery, IOrderedQueryable<T>, IAsyncEnumerable<T>
    {
        private readonly ElasticQueryProvider _elasticQueryProvider;

        public Expression Expression { get; }
        public override string IndexName { get; }
        public override Type ElementType { get; } = typeof(T);
        public IQueryProvider Provider => _elasticQueryProvider;

        public ElasticQuery(ElasticQueryProvider elasticQueryProvider)
        {            
            _elasticQueryProvider = elasticQueryProvider;

            var indexAttribute = typeof(T).GetCustomAttribute<ElasticIndexNameAttribute>();

            if (indexAttribute == null)
            {
                throw new InvalidOperationException($"The type {typeof(T).FullName} does not have the ElasticIndexNameAttribute");
            }

            IndexName = indexAttribute.Name;

            Expression = Expression.Constant(this);
        }

        /// <summary>
        /// This is referenced by reflection from ElasticQuery.Create
        /// </summary>
        /// <param name="elasticQueryProvider"></param>
        /// <param name="indexName"></param>
        private ElasticQuery(ElasticQueryProvider elasticQueryProvider, string indexName)
        {
            _elasticQueryProvider = elasticQueryProvider;
            IndexName = indexName;
            Expression = Expression.Constant(this);
        }

        /// <summary>
        /// This is referenced by reflection from ElasticQuery.Create
        /// </summary>
        /// <param name="elasticQueryProvider"></param>
        /// <param name="expression"></param>
        /// <exception cref="InvalidOperationException"></exception>
        [RequiresUnreferencedCode("This is referenced by reflection from ElasticQuery.Create")]
        internal ElasticQuery(ElasticQueryProvider elasticQueryProvider, Expression expression)
        {            
            Expression = expression;
            _elasticQueryProvider = elasticQueryProvider;

            var constantExpression = new GetFirstConstantNodeVisitor().TryGetFirstConstantExpression(expression);

            if (constantExpression == null || !(constantExpression.Value is Internal.ElasticQuery innerQuery))
            {
                throw new InvalidOperationException("The expression is not setup correctly");
            }

            IndexName = innerQuery.IndexName;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        private IEnumerator<T> GetEnumerator()
        {
            return new ElasticQueryEnumerator<T>(_elasticQueryProvider, Expression);
        }

        public override string? ToString()
        {
            if (Expression is ConstantExpression c && c.Value == this)
            {
                return GetType().FullName;
            }
            
            return Expression.ToString();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new ElasticQueryEnumerator<T>(_elasticQueryProvider, Expression, cancellationToken);
        }
    }
}