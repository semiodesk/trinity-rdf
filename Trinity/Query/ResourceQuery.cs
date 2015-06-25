// LICENSE:
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
// AUTHORS:
//
//  Moritz Eberl <moritz@semiodesk.com>
//  Sebastian Faubel <sebastian@semiodesk.com>
//
// Copyright (c) Semiodesk GmbH 2015

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// 
    /// </summary>
    public class ResourceQuery
    {
        #region Members

        /// <summary>
        /// The RDF Vocabulary (RDF)
        /// </summary>
        protected static class rdf
        {
            /// <summary>
            /// The subject is an instance of a class.
            /// </summary>
            public static readonly Property type = new Property(new UriRef("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
        }

        /// <summary>
        /// The identifier being used as a variable name in the generated query.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Other queries which refer to this query.
        /// </summary>
        internal readonly HashSet<ResourceQuery> DependentQueries = new HashSet<ResourceQuery>();

        /// <summary>
        /// The properties of the resource.
        /// </summary>
        internal readonly List<StatementPattern> WhereStatements = new List<StatementPattern>();

        private int _offset = -1;

        public int Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

        private int _limit = -1;

        public int Limit
        {
            get { return _limit; }
            set { _limit = value; }
        }

        private bool _inferencingEnabled = false;

        internal bool InferencingEnabled
        {
            get { return _inferencingEnabled; }
            set { _inferencingEnabled = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>     
        public ResourceQuery() { }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="resource">The subject resource.</param>
        public ResourceQuery(IResource resource)
        {
            Uri = resource.Uri;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="uri">The Uniform Resource Identifier of the subject.</param>
        public ResourceQuery(Uri uri)
        {
            Uri = uri;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="types">An array of types of the resource.</param>
        public ResourceQuery(params Class[] types)
        {
            AddTypesConstraint(types);
        }

        public ResourceQuery(IEnumerable<Class> types)
        {
            AddTypesConstraint(types);
        }

        #endregion

        #region Methods
        private void AddTypesConstraint(IEnumerable<Class> types)
        {
            foreach (Class type in types)
            {
                WhereStatements.Add(new StatementPattern(this.Uri, rdf.type.Uri, type));
            }
        }

        public void Contains(string value, bool caseSensitive = false)
        {
            if (!string.IsNullOrEmpty(value))
            {
                StatementPattern pattern = new StatementPattern(this.Uri, null, value).Contains(value, caseSensitive);

                WhereStatements.Add(pattern);
            }
        }

        public StatementPattern Where(Property property, IEnumerable<object> objects)
        {
            if (objects == null)
            {
                throw new ArgumentNullException("objects");
            }

            StatementPattern pattern = null;

            foreach (object o in objects)
            {
                pattern = Where(property, o);
            }

            return pattern;
        }

        public StatementPattern Where(Property property, object value = null)
        {
            if (value is ResourceQuery)
            {
                ResourceQuery query = value as ResourceQuery;
                query.DependentQueries.Add(this);
                this.DependentQueries.Add(query);
            }

            StatementPattern pattern;

            if (property != null)
            {
                if (value != null)
                {
                    pattern = new StatementPattern(this.Uri, property.Uri, value);
                }
                else
                {
                    pattern = new StatementPattern(this.Uri, property.Uri, null);
                }
            }
            else
            {
                if (value != null)
                {
                    pattern = new StatementPattern(this.Uri, null, value);
                }
                else
                {
                    throw new ArgumentException(string.Format("Error: Invalid arguments {0} {1}.", property, value));
                }
            }

            WhereStatements.Add(pattern);

            return pattern;
        }

        public ResourceQuery Clone()
        {
            ResourceQuery result;

            if (Clone(out result, new List<ResourceQuery>(), new List<ResourceQuery>()))
            {
                return result;
            }
            else
            {
                throw new Exception(string.Format("{0}: Error: Unkown error occured while cloning resource {1}.", typeof(ResourceQuery), this));
            }
        }

        private bool Clone(out ResourceQuery result, IList<ResourceQuery> processed, IList<ResourceQuery> generated)
        {
            if (!processed.Contains(this))
            {
                result = new ResourceQuery();
                result.Offset = Offset;
                result.Limit = Limit;

                processed.Add(this);
                generated.Add(result);

                foreach (ResourceQuery query in DependentQueries)
                {
                    ResourceQuery q;

                    if (query.Clone(out q, processed, generated))
                    {
                        q.DependentQueries.Add(result);
                        result.DependentQueries.Add(q);
                    }
                }

                foreach (StatementPattern pattern in WhereStatements)
                {
                    if (pattern.Object is ResourceQuery && processed.Contains(pattern.Object as ResourceQuery))
                    {
                        result.WhereStatements.Add(new StatementPattern(pattern.Subject, pattern.Predicate, generated[processed.IndexOf(pattern.Object as ResourceQuery)]));
                    }
                    else
                    {
                        result.WhereStatements.Add(pattern);
                    }
                }

                return true;
            }
            else
            {
                result = null;

                return false;
            }
        }

        #endregion
    }
}
