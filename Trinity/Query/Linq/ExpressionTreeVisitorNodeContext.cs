using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using VDS.RDF;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Primary;

namespace Semiodesk.Trinity.Query
{
    internal class ExpressionTreeVisitorNodeContext
    {
        #region Members

        public INode Node;

        public ISparqlExpression Expression;

        #endregion

        #region Constructors

        private ExpressionTreeVisitorNodeContext(ISparqlExpression expression)
        {
            Expression = expression;
        }

        private ExpressionTreeVisitorNodeContext(INode node, ISparqlExpression expression = null)
        {
            Node = node;
            Expression = expression;
        }

        #endregion

        #region Methods

        public static ExpressionTreeVisitorNodeContext FromVariableName(string variableName)
        {
            ISparqlExpression expresssion = new VariableTerm(variableName);

            return new ExpressionTreeVisitorNodeContext(expresssion);
        }

        public static ExpressionTreeVisitorNodeContext FromConstantExpression(ConstantExpression constantExpression)
        {
            string value = constantExpression.Value.ToString();

            INode node;

            if (constantExpression.Type == typeof(string))
            {
                node = new NodeFactory().CreateLiteralNode(value);
            }
            else
            {
                Uri datatype = XsdTypeMapper.GetXsdTypeUri(constantExpression.Type);

                node = new NodeFactory().CreateLiteralNode(value, datatype);
            }

            ISparqlExpression expression = new ConstantTerm(node);

            return new ExpressionTreeVisitorNodeContext(node, expression);
        }

        public static ExpressionTreeVisitorNodeContext FromMemberExpression(MemberExpression memberExpression)
        {
            MemberInfo member = memberExpression.Member;

            RdfPropertyAttribute attribute = member.TryGetRdfPropertyAttribute();

            INode node = new NodeFactory().CreateUriNode(attribute.MappedUri);

            return new ExpressionTreeVisitorNodeContext(node);
        }

        #endregion
    }
}
