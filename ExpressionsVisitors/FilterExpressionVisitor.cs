using System.Collections;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace ElasticQuery.ExpressionsVisitors
{
    public class FilterExpressionVisitor : ElasticSqlVisitor
    {
        private StringBuilder _sb;
        private readonly Type _inputType;
        private readonly List<object> _filterParams;
        private readonly NestedObjectExpressionVisitor _nestedObjectVisitor;

        public FilterExpressionVisitor(Type inputType, List<object> filterParams)
        {
            _nestedObjectVisitor = new NestedObjectExpressionVisitor();
            _sb = new StringBuilder();
            _inputType = inputType;
            _filterParams = filterParams;
        }

        public string? GetFilter(Expression expression)
        {
            LambdaExpression? lambda = expression as LambdaExpression;

            if (expression is UnaryExpression unaryExpression)
            {
                lambda = unaryExpression.Operand as LambdaExpression;
            }

            if (lambda!.ReturnType != typeof(bool))
            {
                return null;
            }

            _sb = new StringBuilder();

            Visit(expression);

            return _sb.ToString();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.DeclaringType == _inputType)
            {
                _sb.Append(node.Member.Name);
            }
            else
            {
                var value = GetValue(node);

                if (value != null)
                {
                    _sb.Append(" ? ");
                    _filterParams.Add(value);
                }
                else
                {
                    _sb.Append(NULL);
                }
            }

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value != null)
            {
                _sb.Append(" ? ");
                _filterParams.Add(node.Value);
            }
            else
            {
                _sb.Append(NULL);
            }

            return node;
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            _sb.Append("(");

            if (node.Left is MethodCallExpression methodCallExpression && methodCallExpression.Method.Name == nameof(string.Compare) && methodCallExpression.Method.DeclaringType == typeof(string))
            {
                _sb.Append((methodCallExpression.Arguments[0] as MemberExpression)!.Member.Name);

                switch (node.NodeType)
                {
                    case ExpressionType.Equal:
                        var val = GetValue(node.Right);

                        if (val == null)
                        {
                            _sb.Append(" IS ");
                        }
                        else
                        {
                            _sb.Append(" = ");
                        }
                        break;

                    case ExpressionType.NotEqual:
                        var notEqVal = GetValue(node.Right);

                        if (notEqVal == null)
                        {
                            _sb.Append(" IS NOT ");
                        }
                        else
                        {
                            _sb.Append(" <> ");
                        }
                        break;

                    case ExpressionType.LessThan:
                        _sb.Append(" < ");
                        break;

                    case ExpressionType.LessThanOrEqual:
                        _sb.Append(" <= ");
                        break;

                    case ExpressionType.GreaterThan:
                        _sb.Append(" > ");
                        break;

                    case ExpressionType.GreaterThanOrEqual:
                        _sb.Append(" >= ");
                        break;
                }

                var value = GetValue(methodCallExpression.Arguments[1]);

                if (value != null)
                {
                    _sb.Append(" ? ");
                    _filterParams.Add(value);
                }
                else
                {
                    _sb.Append(NULL);
                }
            }
            else
            {
                if (node.Left is ConditionalExpression conditionalExpression)
                {
                    _nestedObjectVisitor.Visit(conditionalExpression.IfFalse);
                    _sb.Append(_nestedObjectVisitor.Members.Pop().Member.Name);

                    while (_nestedObjectVisitor.Members.Count > 0)
                    {
                        _sb.Append("." + _nestedObjectVisitor.Members.Pop().Member.Name);
                    }
                }
                else if (node.Left is MethodCallExpression callExpression && callExpression.Method.Name == nameof(IList.Contains))
                {
                    var values = (IList)GetValue(callExpression.Arguments[0])!;
                    var memberName = (callExpression.Arguments[1] as MemberExpression)?.Member.Name;

                    _sb.Append("(");

                    for (int i = 0; i < values.Count - 1; i++)
                    {
                        var value = values[i];

                        if (value == null)
                        {
                            _sb.Append(memberName + " = " + NULL + " OR ");
                        }
                        else
                        {
                            _sb.Append(memberName + " = ? OR ");
                            _filterParams.Add(value);
                        }
                    }

                    var lastValue = values[values.Count - 1];

                    if (lastValue == null)
                    {
                        _sb.Append(memberName + " = " + NULL);
                    }
                    else
                    {
                        _sb.Append(memberName + " = ?");
                        _filterParams.Add(lastValue);
                    }

                    _sb.Append(")");                    
                }
                else
                {
                    Visit(node.Left);
                }

                switch (node.NodeType)
                {
                    case ExpressionType.And:
                        _sb.Append(" & ");
                        break;

                    case ExpressionType.AndAlso:
                        _sb.Append(" AND ");
                        break;

                    case ExpressionType.Or:
                        _sb.Append(" | ");
                        break;

                    case ExpressionType.OrElse:
                        _sb.Append(" OR ");
                        break;

                    case ExpressionType.Equal:
                        var val = GetValue(node.Right);

                        if (val == null)
                        {
                            _sb.Append(" IS ");
                        }
                        else
                        {
                            _sb.Append(" = ");
                        }
                        break;

                    case ExpressionType.NotEqual:
                        var notEqVal = GetValue(node.Right);

                        if (notEqVal == null)
                        {
                            _sb.Append(" IS NOT ");
                        }
                        else
                        {
                            _sb.Append(" <> ");
                        }
                        break;

                    case ExpressionType.LessThan:
                        _sb.Append(" < ");
                        break;

                    case ExpressionType.LessThanOrEqual:
                        _sb.Append(" <= ");
                        break;

                    case ExpressionType.GreaterThan:
                        _sb.Append(" > ");
                        break;

                    case ExpressionType.GreaterThanOrEqual:
                        _sb.Append(" >= ");
                        break;

                    default:
                        throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", node.NodeType));
                }


                Visit(node.Right);
            }

            _sb.Append(")");

            return node;
        }
    }
}
