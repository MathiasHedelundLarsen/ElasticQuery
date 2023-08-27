using System.Linq.Expressions;
using System.Reflection;

namespace ElasticQuery.ExpressionsVisitors
{
    public sealed class SelectExpressionVisitor : ElasticSqlVisitor
    {        
        private readonly Type _inputType;
        private readonly List<object> _parameters;
        private readonly List<VisitedProperty> _visitedProperties;

        public LambdaExpression? SelectExpression { get; private set; }

        public SelectExpressionVisitor(Type inputType, List<VisitedProperty> visitedProperties, List<object> parameters)
        {
            _inputType = inputType;
            _visitedProperties = visitedProperties;
            _parameters = parameters;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            SelectExpression = node;
            return base.VisitLambda(node);
        }

        protected override Expression VisitNew(NewExpression node)
        {
            if (node.Arguments.Any()) //It is a constructor mainly used by anonymous types
            {
                var constructorParameterNames = node.Constructor!
                    .GetParameters()
                    .Select(x => x.Name)
                    .ToArray();

                for (int i = 0; i < constructorParameterNames.Length; i++)
                {
                    VisitProperty(node.Arguments[i], constructorParameterNames[i]);                   
                }

                return node;
            }

            return base.VisitNew(node);
        }

        private void VisitProperty(Expression expression, string? name)
        {
            if (expression is MemberExpression memberExpression && memberExpression.Member.DeclaringType == _inputType)
            {
                _visitedProperties.Add(new VisitedProperty
                {
                    Alias = name,
                    Value = memberExpression.Member.Name
                });
            }
            else
            {
                var value = GetValue(expression);

                if (value != null)
                {
                    _visitedProperties.Add(new VisitedProperty
                    {
                        Alias = name,
                        Value = "?" //https://www.elastic.co/guide/en/elasticsearch/reference/current/sql-rest-params.html
                    });

                    _parameters.Add(value);
                }
                else
                {
                    _visitedProperties.Add(new VisitedProperty
                    {
                        Alias = name,
                        Value = NULL
                    });
                }
            }
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (_odataPropertyContainer.IsAssignableFrom(node.NewExpression.Type))
            {
                new ElasticOdataExpressionVisitor(_visitedProperties).Visit(node);

                return node;
            }

            return base.VisitMemberInit(node);
        }

        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            Type? memberType = null;

            if (node.Member is FieldInfo fieldInfo)
            {
                memberType = fieldInfo.FieldType;
            }
            else if (node.Member is PropertyInfo propertyInfo)
            {
                memberType = propertyInfo.PropertyType;
            }

            if (memberType != null && memberType.IsBasicType(true))
            {
                if (_odataPropertyContainer.IsAssignableFrom(node.Member.DeclaringType))
                {
                    var memberName = GetValue(node.Expression) as string;

                    if (!string.IsNullOrWhiteSpace(memberName))
                    {
                        _visitedProperties.Add(new VisitedProperty
                        {
                            Alias = memberName,
                            Value = memberName
                        });
                    }
                }
                else
                {
                    VisitProperty(node.Expression, node.Member.Name);
                }
            }

            return base.VisitMemberAssignment(node);
        }
    }
}
