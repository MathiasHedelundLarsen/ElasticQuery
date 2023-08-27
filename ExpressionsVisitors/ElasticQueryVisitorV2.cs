using System.Linq.Expressions;
using System.Reflection;

namespace ElasticQuery.ExpressionsVisitors
{
    public sealed class ElasticQueryVisitorV2 : ExpressionVisitor
    {        
        private readonly Type _elementType;
        private readonly List<string> _filters = new List<string>();
        private readonly List<object> _filterParams = new List<object>();

        private readonly List<object> _selectorParams = new List<object>();
        private readonly List<VisitedProperty> _selectors = new List<VisitedProperty>();

        private readonly List<LambdaExpression> _orderdByAscending = new List<LambdaExpression>();

        public object[] Parameters => _selectorParams.Concat(_filterParams).ToArray();

        public LambdaExpression? SelectExpression { get; private set; }

        public ElasticQueryVisitorV2(Type elementType)
        {
            _elementType = elementType;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == nameof(Queryable.Select))
            {                
                var selectVisitor = new SelectExpressionVisitor(_elementType, _selectors, _selectorParams);
                
                selectVisitor.Visit(node.Arguments[1]!);

                SelectExpression = selectVisitor.SelectExpression;
            }
            else if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == nameof(Queryable.Where))
            {
                _filters.Add(new FilterExpressionVisitor(_elementType, _filterParams).GetFilter(node.Arguments[1])!);
            }
            else if (node.Method.DeclaringType == typeof(Queryable) && node.Method.Name == nameof(Queryable.OrderBy))
            {
                
            }

            return base.VisitMethodCall(node);
        }

        public string AsSQLString(string indexName)
        {
            if (_selectors.Count == 0)
            {
                _selectors.AddRange(_elementType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(x => new VisitedProperty
                {
                    Alias = x.Name, 
                    Value = x.Name
                }));
            }

            string select = string.Join(", ", _selectors.Select(x => x.ToString()));

            string where = string.Empty;

            if (_filters.Count > 0)
            {
                where = $" WHERE {string.Join(" AND ", _filters)}";
            }

            return $"SELECT {select} FROM {indexName}{where}";
        }
    }
}
