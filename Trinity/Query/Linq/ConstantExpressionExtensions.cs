using System;
using System.Linq.Expressions;
using VDS.RDF;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    public static class ConstantExpressionExtensions
    {
        public static ISparqlExpression AsSparqlExpression(this ConstantExpression constantExpression)
        {
            string value = constantExpression.Value.ToString();

            if (constantExpression.Type == typeof(string))
            {
                return new ConstantTerm(new NodeFactory().CreateLiteralNode(value));
            }
            else
            {
                Uri datatype = XsdTypeMapper.GetXsdTypeUri(constantExpression.Type);

                return new ConstantTerm(new NodeFactory().CreateLiteralNode(value, datatype));
            }
        }

        public static INode AsNode(this ConstantExpression constantExpression)
        {
            string value = constantExpression.Value.ToString();

            if (constantExpression.Type == typeof(string))
            {
                return new NodeFactory().CreateLiteralNode(value);
            }
            else
            {
                Uri datatype = XsdTypeMapper.GetXsdTypeUri(constantExpression.Type);

                return new NodeFactory().CreateLiteralNode(value, datatype);
            }
        }
    }
}
