using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using ElasticQuery.ExpressionsVisitors;
using Microsoft.EntityFrameworkCore.Query;

namespace ElasticQuery
{
    public sealed class ElasticQueryProvider : IAsyncQueryProvider
    {
        private static readonly MethodInfo _enumerableCast = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast), BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo _enumerableToList = typeof(Enumerable).GetMethod(nameof(Enumerable.ToList), BindingFlags.Public | BindingFlags.Static)!;
        private static readonly Type _odataSelectSomeBase = Type.GetType("Microsoft.AspNetCore.OData.Query.Wrapper.SelectExpandWrapper, Microsoft.AspNetCore.OData")!;

        private readonly HttpClient _httpClient;

        public ElasticQueryProvider(HttpClient httpClient) 
        {
            _httpClient = httpClient;
        }

        private Internal.ElasticQuery GetRootQuery(Expression expression)
        {
            var constantExpression = new GetFirstConstantNodeVisitor().TryGetFirstConstantExpression(expression);

            if (constantExpression == null || !(constantExpression.Value is Internal.ElasticQuery innerQuery))
            {
                throw new InvalidOperationException("The expression is not setup correctly");
            }

            return innerQuery;
        }

        private TResult? ExecuteHttpRequest<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var rootQuery = GetRootQuery(expression);

            string indexName = rootQuery.IndexName;

            var queryVisitor = new ElasticQueryVisitorV2(rootQuery.ElementType);

            queryVisitor.Visit(expression);

            var sqlText = queryVisitor.AsSQLString(indexName);

            var elasticQueryPost = new
            {
                Query = sqlText,
                Params = queryVisitor.Parameters
            };

            var genericArgumentType = typeof(TResult).GetGenericArguments()[0];

            if (_odataSelectSomeBase.IsAssignableFrom(genericArgumentType) && queryVisitor.SelectExpression != null)
            {
                var data = (IEnumerable)JsonSerializer.Deserialize("[{ \"info\" : { \"name\" : \"Testing\" }, \"date\" : \"2015-01-01T12:10:30Z\", \"summary\" : \"Too hot\" }]", rootQuery.ElementType.MakeArrayType(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;

                var selectDelegate = queryVisitor.SelectExpression.Compile();

                return (TResult)_enumerableToList.MakeGenericMethod(genericArgumentType).Invoke(null, new object[] { _enumerableCast.MakeGenericMethod(genericArgumentType).Invoke(null, new object[] { data.Cast<object>().Select(x => selectDelegate.DynamicInvoke(x)) })! })!;
            }

            return Activator.CreateInstance<TResult>();
        }

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return Internal.ElasticQuery.Create(expression.Type, this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new ElasticQuery<TElement>(this, expression);
        }

        public object? Execute(Expression expression)
        {
            return ExecuteHttpRequest<JsonElement[]>(expression);
        }       

        public TResult Execute<TResult>(Expression expression)
        {
            return ExecuteHttpRequest<TResult>(expression)!;
        }

        TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            return ExecuteHttpRequest<TResult>(expression, cancellationToken)!;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResult">Required to be a List{T}</typeparam>
        /// <param name="expression"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) where TResult : IList, new()
        {
            var rootQuery = GetRootQuery(expression);

            string indexName = rootQuery.IndexName;

            var queryVisitor = new ElasticQueryVisitorV2(rootQuery.ElementType);

            queryVisitor.Visit(expression);

            var sqlText = queryVisitor.AsSQLString(indexName);

            var elasticQueryPost = new
            {
                Query = sqlText,
                Params = queryVisitor.Parameters
            };

            var genericArgumentType = typeof(TResult).GetGenericArguments()[0];

            if (_odataSelectSomeBase.IsAssignableFrom(genericArgumentType) && queryVisitor.SelectExpression != null)
            {
                var data = (IEnumerable)JsonSerializer.Deserialize("[{ \"info\" : { \"name\" : \"Testing\" }, \"date\" : \"2015-01-01T12:10:30Z\", \"summary\" : \"Too hot\" }]", rootQuery.ElementType.MakeArrayType(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;

                var selectDelegate = queryVisitor.SelectExpression.Compile();

                return (TResult)_enumerableToList.MakeGenericMethod(genericArgumentType).Invoke(null, new object[] { _enumerableCast.MakeGenericMethod(genericArgumentType).Invoke(null, new object[] { data.Cast<object>().Select(x => selectDelegate.DynamicInvoke(x)) })! })!;
            }
            
            return new TResult();
        }
    }
}
