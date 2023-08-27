using System.Linq.Expressions;
using System.Reflection;

namespace ElasticQuery.ExpressionsVisitors
{
    public abstract class ElasticSqlVisitor : ExpressionVisitor
    {
        protected const string NULL = "NULL";

        protected static readonly Type _odataPropertyContainer = Type.GetType("Microsoft.AspNetCore.OData.Query.Container.PropertyContainer, Microsoft.AspNetCore.OData")!;

        protected static object? GetValue(Expression expression)
        {
            object? value = null;

            if (expression is ConstantExpression constantExpression)
            {
                value = constantExpression.Value;
            }
            else if (expression is MemberExpression memberExpression)
            {
                if (memberExpression.Expression is ConstantExpression memberConstant)
                {
                    if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        value = fieldInfo.GetValue(memberConstant.Value);
                    }
                    else if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        value = propertyInfo.GetValue(memberConstant.Value);
                    }
                }
                //It is a static member such as string.Empty or DateTime.UtcNow
                else if (memberExpression.Expression == null)
                {
                    if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        value = fieldInfo.GetValue(null);
                    }
                    else if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        value = propertyInfo.GetValue(null);
                    }
                }
                //Check if the member is in a nested object
                else if (memberExpression.Expression is MemberExpression innerExpression && innerExpression.Expression is ConstantExpression innerConstantExpression)
                {
                    object? innerMemberValue = null;

                    if (innerExpression.Member is FieldInfo innerFieldInfo)
                    {
                        innerMemberValue = innerFieldInfo.GetValue(innerConstantExpression.Value);
                    }
                    else if (innerExpression.Member is PropertyInfo innerPropertyInfo)
                    {
                        innerMemberValue = innerPropertyInfo.GetValue(innerConstantExpression.Value);
                    }

                    if (memberExpression.Member is FieldInfo fieldInfo)
                    {
                        value = fieldInfo.GetValue(innerMemberValue);
                    }
                    else if (memberExpression.Member is PropertyInfo propertyInfo)
                    {
                        value = propertyInfo.GetValue(innerMemberValue);
                    }
                }
            }

            return value;
        }
    }
}
