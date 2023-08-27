using System.Linq.Expressions;

namespace ElasticQuery.ExpressionsVisitors
{
    public class ElasticOdataExpressionVisitor : ElasticSqlVisitor
    {
        public static string _DOT_ = "_DOT_";

        private readonly List<VisitedProperty> _visitedProperties;       

        public ElasticOdataExpressionVisitor(List<VisitedProperty> visitedProperties)
        {
            _visitedProperties = visitedProperties;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if (_odataPropertyContainer.IsAssignableFrom(node.NewExpression.Type))
            {
                var nameBinding = node.Bindings
                    .Where(x => x.Member.Name == "Name")
                    .Cast<MemberAssignment>()
                    .Single();

                var valueBinding = node.Bindings
                    .Where(x => x.Member.Name == "Value")
                    .Cast<MemberAssignment>()
                    .Single();

                if (nameBinding.Expression is ConstantExpression constantExpression) 
                {
                    var alias = constantExpression.Value!.ToString()!;

                    if (!_visitedProperties.Any(x => x.Alias == alias))
                    {
                        if (valueBinding.Expression is MemberExpression memberExpression)
                        {
                            _visitedProperties.Add(new VisitedProperty
                            {
                                Alias = memberExpression.Member.Name,
                                Value = memberExpression.Member.Name
                            });
                        }
                        else if (valueBinding.Expression is ConditionalExpression conditionalExpression && conditionalExpression.IfFalse is MemberExpression conditionalMemberExpression)
                        {
                            string name = conditionalMemberExpression.ToString().Remove(0, 4);

                            _visitedProperties.Add(new VisitedProperty
                            {
                                Alias = name.Replace(".", _DOT_),
                                Value = name
                            });
                        }
                        else
                        {
                            _visitedProperties.Add(new VisitedProperty
                            {
                                Alias = alias,
                                Value = alias
                            });
                        }
                    }
                }
            }

            return base.VisitMemberInit(node);
        }
    }
}
