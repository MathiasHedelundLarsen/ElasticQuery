using System.Linq.Expressions;

namespace ElasticQuery.ExpressionsVisitors
{
    public class NestedObjectExpressionVisitor : ExpressionVisitor
    {
        public Stack<MemberExpression> Members { get; } = new Stack<MemberExpression>();       

        protected override Expression VisitMember(MemberExpression node)
        {
            Members.Push(node);
            return base.VisitMember(node);
        }
    }
}
