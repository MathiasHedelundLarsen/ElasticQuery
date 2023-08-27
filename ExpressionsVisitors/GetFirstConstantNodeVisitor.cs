using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace ElasticQuery.ExpressionsVisitors
{
    public sealed class GetFirstConstantNodeVisitor : ExpressionVisitor
    {
        private ConstantExpression? _constantExpression;

        public ConstantExpression? TryGetFirstConstantExpression(Expression? expression)
        {
            _constantExpression = null;

            base.Visit(expression);

            return _constantExpression;
        }

        [return: NotNullIfNotNull("node")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override Expression? Visit(Expression? node)
        {
            if (_constantExpression != null)
            {
                return node;
            }

            return base.Visit(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (_constantExpression == null)
            {
                _constantExpression = node;

                return _constantExpression;
            }

            return base.Visit(node);
        }
    }
}
