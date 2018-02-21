

using Remotion.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Semiodesk.Trinity.Query
{
    internal class SparqlQueryExecutor : IQueryExecutor
    {
        #region Members
        
        protected IModel Model { get; private set; }

        // A handle to the generic version of the GetResources method which is being used
        // for implementing the ExecuteCollection(QueryModel) method that supports runtime type specification.
        private MethodInfo _getResourceMethod;

        private bool _inferenceEnabled;

        #endregion

        #region Constructors

        public SparqlQueryExecutor(IModel model, bool inferenceEnabled)
        {
            Model = model;

            _inferenceEnabled = inferenceEnabled;

            // Searches for the generic method IEnumerable<T> GetResources<T>(ResourceQuery) and saves a handle
            // for later use within ExecuteCollection(QueryModel);
            _getResourceMethod = model.GetType().GetMethods().FirstOrDefault(m => m.IsGenericMethod && m.Name == "GetResources" && m.GetParameters().Any(p => p.ParameterType == typeof(ISparqlQuery)));
        }
        
        #endregion

        #region Methods

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            Type t = queryModel.SelectClause.Selector.Type;

            if(typeof(Resource).IsAssignableFrom(t))
            {
                // Handle queries which return instances of resources.
                SparqlQueryModelVisitor<T> visitor = new SparqlQueryModelVisitor<T>(new SelectTriplesQueryGenerator());
                visitor.VisitQueryModel(queryModel);

                MethodInfo getResources = _getResourceMethod.MakeGenericMethod(typeof(T));
                object[] args = new object[] { visitor.GetQuery(), _inferenceEnabled, null };

                foreach (T value in getResources.Invoke(Model, args) as IEnumerable<T>)
                {
                    yield return value;
                }
            }
            else
            {
                // Handle queries which return value type objects.
                SparqlQueryModelVisitor<T> visitor = new SparqlQueryModelVisitor<T>(new SelectBindingsQueryGenerator());
                visitor.VisitQueryModel(queryModel);

                ISparqlQuery query = visitor.GetQuery();
                ISparqlQueryResult result = Model.ExecuteQuery(query);

                // TODO: This works correctly for single bindings, check with multiple bindings.
                foreach(BindingSet bindings in result.GetBindings())
                {
                    foreach(T value in bindings.Values.OfType<T>())
                    {
                        yield return value;
                    }
                }
            }
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var sequence = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            Type t = typeof(T);

            if(t == typeof(bool))
            {
                // Generate and execute ASK query.
                SparqlQueryModelVisitor<T> visitor = new SparqlQueryModelVisitor<T>(new AskQueryGenerator());
                visitor.VisitQueryModel(queryModel);

                ISparqlQuery query = visitor.GetQuery();
                ISparqlQueryResult result = Model.ExecuteQuery(query);

                return new object[] { result.GetAnwser() }.OfType<T>().First();
            }
            else
            {
                // ??
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
