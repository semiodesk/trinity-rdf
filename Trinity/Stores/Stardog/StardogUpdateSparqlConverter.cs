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
//  Mark Stemmler <mark.stemmler@schneider-electric.com>

using System;
using System.Collections.Generic;
using System.Linq;
using VDS.RDF;

namespace Semiodesk.Trinity.Store.Stardog
{
    /// <summary>
    /// SPARQL converter. Takes a typical Stardog "update" query and decomposes it so that it can be used with the StardogConnector's UpdateGraph method.
    /// Does require a valid Store instance to extract the Removals from the current connection.  
    /// </summary>
    public class StardogUpdateSparqlConverter
    {
        #region Types

        class TripleSet
        {
            public ParsedNode Subject { get; set; }

            public ParsedNode Predicate { get; set; }

            public ParsedNode Object { get; set; }

            public override string ToString() => $"<{Subject}> <{Predicate}> <{Object}>";
        }

        class ParsedNode
        {
            public string Value { get; set; }

            public string LiteralType { get; set; }

            public bool IsLiteralNode { get; internal set; }

            public override string ToString() => Value;
        }

        class PeakedNode
        {
            public bool NodePresent { get; set; }

            public bool NodeIsLiteral { get; set; }

            public string Start { get; set; }

            public string End { get; set; }
        }

        #endregion

        #region Members

        private readonly StardogStore _store = null;

        /// <summary>
        /// The last SPARQL query supplied to <seealso cref="ParseQuery"/>
        /// </summary>
        public string LastParsedQuery { get; private set; }

        /// <summary>
        /// The URI of the Graph Additions and Deletes will be applied to.
        /// </summary>
        public string GraphUri { get; private set; }

        /// <summary>
        /// The URI of the entity being updated/saved.
        /// </summary>
        public string PrimaryUri { get; private set; }

        /// <summary>
        /// Triple instances which will be removed
        /// </summary>
        public IList<Triple> Removals { get; private set; }

        /// <summary>
        /// Triple instances converted from UpdateTriples
        /// </summary>
        public IList<Triple> Additions { get; private set; }

        /// <summary>
        /// Parsed TripleSet instances which constitute the Additions
        /// </summary>
        private List<TripleSet> _updateTriples { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Create an instance of the class <c>StardogUpdateSparqlConvertor</c>.
        /// </summary>
        public StardogUpdateSparqlConverter() {}

        /// <summary>
        /// Create an instance of the class <c>StardogUpdateSparqlConvertor</c>.
        /// </summary>
        /// <param name="store">Startdog store instance.</param>
        public StardogUpdateSparqlConverter(StardogStore store)
        {
            _store = store;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Given a typical SPARQL update query, it will be parsed and decomposed into the appropriate artifacts suitable to call the StardogConnector UpdateGraph method.
        /// </summary>
        /// <param name="sparqlQuery">SPARQL query string.</param>
        public void ParseQuery(string sparqlQuery)
        {
            LastParsedQuery = sparqlQuery;
            ExtractGraphNameAndUriFromQuery(sparqlQuery);

            var insertPortion = sparqlQuery.Between("INSERT { ", " . } WHERE ");
            _updateTriples = ParseQueryIntoTripleSets(insertPortion);
            var nf = new NodeFactory();
            var additions = _updateTriples.Select(x => CreateTriple(x, nf)).ToList();

            // Either a pure insert (which we already have info for) or we're not connected to a store so there's nothing we can query for anyway
            if (string.IsNullOrEmpty(PrimaryUri) || _store == null)
            {
                Removals = null;
            }
            else
            {
                // Per guidance from Stardog, include the pragma to disable reasoning here (requires StarDog 6.1 or later)
                // Since this query is only to determine actually persisted triples and reasoned triples aren't removable!
                var queryTriplesToRemove = $@"SELECT ?s_ ?p_ ?o_ FROM <{GraphUri}> 
                                              WHERE {{ #pragma reasoning off
                                                 ?s_ ?p_ ?o_ . FILTER ( ?s_ = <{PrimaryUri}> ) }}";
                var inStore = _store.ExecuteQuery(queryTriplesToRemove);

                var pendingRemovals = inStore.SparqlResultSet.Results.Select(x => new Triple(x["s_"], x["p_"], x["o_"])).ToList();

                // Stardog will suffer extreme performance degradation if we blindly remove and add back the same triple (UPDATE: This was a Stardog issue addressed in 6.2 or later)
                // Additions will have priority, but if an addition is simply adding back a removal, we'll remove them from both lists as this would be a no-op
                if (additions.Count == 0)
                {
                    // This is a pure delete, we'll actually just allow all removals
                    Removals = pendingRemovals;
                }
                else
                {
                    var neededRemovals = new List<Triple>();
                    foreach (var pendingRemoval in pendingRemovals)
                    {
                        var matchingAddition = additions.FirstOrDefault(x => x.Equals(pendingRemoval));
                        if (matchingAddition != null)
                        {
                            // We have a match so we'll remove it from the Additions and won't add it to the Removals
                            additions.Remove(matchingAddition);
                        }
                        else
                        {
                            // This is a change so we'll remove the old triple
                            neededRemovals.Add(pendingRemoval);
                        }
                    }

                    Removals = neededRemovals;
                }
            }

            Additions = additions;
        }

        private void ExtractGraphNameAndUriFromQuery(string sparqlQuery)
        {
            string graphUri;
            string primaryUri;

            // Examples of the three cases: Pure Insert, Pure Delete, Update (Delete & Insert)
            // PureDelete:  DELETE WHERE { GRAPH <http://www.foo.com/> { ?s ?p <http://www.foo.se.com/5dac76df-1268-4b7c-8fdb-adb1dd660a32> . } }
            // Pure Insert: WITH <http://www.foo.com/> INSERT { triples here . } WHERE { }
            // Update:      WITH <http://www.foo.com/> DELETE { <http://www.foo.com/bd1e4760-2b8b-48de-9eef-939b7242e8b4> ?p ?o . } INSERT { triples here . } WHERE { OPTIONAL { <http://www.foo.com/bd1e4760-2b8b-48de-9eef-939b7242e8b4> ?p ?o . } }

            // Pure delete looks very different than an update (which also contains a delete) or a pure insert (which doesn't)
            if (sparqlQuery.StartsWith("DELETE WHERE {"))
            {
                graphUri = sparqlQuery.Between("<", ">");
                primaryUri = sparqlQuery.Between("<", ">", 2);
            }
            else
            {
                // Update?
                graphUri = sparqlQuery.Between("WITH <", "> DELETE");
                if (string.IsNullOrEmpty(graphUri))
                {
                    // This is a Pure Insert query
                    graphUri = sparqlQuery.Between("WITH <", "> INSERT");
                    primaryUri = string.Empty; // We'll infer it when we CreateTriple later
                }
                else
                {
                    // This is an update query
                    primaryUri = sparqlQuery.Between("DELETE { <", "> ?p ?o . }");
                }
            }

            GraphUri = graphUri;
            PrimaryUri = primaryUri;
        }

        private Triple CreateTriple(TripleSet tripleSet, NodeFactory nf)
        {
            var s = tripleSet.Subject == null ?
                nf.CreateUriNode(new Uri(PrimaryUri)) :
                CreateNode(nf, tripleSet.Subject);

            // Pure inserts don't have the DELETE portion of the query expected in ParseQuery so we'll infer it here
            if (string.IsNullOrEmpty(PrimaryUri)) PrimaryUri = s.ToString();

            var p = CreateNode(nf, tripleSet.Predicate);

            var o = CreateNode(nf, tripleSet.Object);

            return new Triple(s, p, o);
        }

        private INode CreateNode(NodeFactory nf, ParsedNode parsedNode)
        {
            if (!parsedNode.IsLiteralNode)
            {
                return nf.CreateUriNode(new Uri(parsedNode.Value));
            }

            // Not sure if there are more things that might be escaped for now, I'll just deal with \'
            var unescaped = parsedNode.Value.Replace("\\'", "'");
            return string.IsNullOrEmpty(parsedNode.LiteralType) ?
                nf.CreateLiteralNode(unescaped) :
                nf.CreateLiteralNode(unescaped, new Uri(parsedNode.LiteralType));
        }

        /// <summary>
        /// Returns string based triples, separated by a ' ; ' string.  If only two are found, the s value is returned as null since it is assumed that predicate and object are present.
        /// </summary>
        private static List<TripleSet> ParseQueryIntoTripleSets(string query)
        {
            var results = new List<TripleSet>();
            var working = query.Trim();
            for (; ; )
            {
                var rawFirst = working.Between("<", ">", out var pointer);
                if (string.IsNullOrEmpty(rawFirst)) break;
                var first = new ParsedNode { Value = rawFirst };
                working = working.Substring(pointer);

                var peakAhead = IsNodePresent(working);
                if (peakAhead.NodePresent)
                {
                    var second = ParsePeakedNode(peakAhead, ref working, out pointer);

                    // Look for the next node
                    peakAhead = IsNodePresent(working);
                    if (peakAhead.NodePresent)
                    {
                        var third = ParsePeakedNode(peakAhead, ref working, out pointer);
                        results.Add(new TripleSet { Subject = first, Predicate = second, Object = third });
                    }
                    else
                    {
                        results.Add(new TripleSet { Subject = null, Predicate = first, Object = second });
                    }
                }
                else
                {
                    // probably won't happen but whatever
                    results.Add(new TripleSet { Subject = first, Predicate = null, Object = null });
                }
            }

            return results;
        }

        private static ParsedNode ParsePeakedNode(PeakedNode peakAhead, ref string working, out int pointer)
        {
            var parsedNode = new ParsedNode();
            if (peakAhead.NodeIsLiteral)
            {
                if (peakAhead.Start == "'" && peakAhead.End == "'")
                {
                    parsedNode.Value = working.BetweenSingleQuotes(out pointer);
                }
                else
                {
                    parsedNode.Value = working.Between(peakAhead.Start, peakAhead.End, out pointer);
                }
                parsedNode.IsLiteralNode = true;
            }
            else
            {
                parsedNode.Value = working.Between(peakAhead.Start, peakAhead.End, out pointer);

            }
            working = working.Substring(pointer);

            // Handle explicit type directive 
            if (peakAhead.NodeIsLiteral && working.Trim().StartsWith("^^"))
            {
                parsedNode.LiteralType = working.Between("<", ">", out pointer);
                working = working.Substring(pointer);
            }

            return parsedNode;
        }

        /// <summary>
        /// Peaks ahead to see if there is another node present; indicated by the next non-whitespace of a &lt; or ' character.
        /// </summary>
        private static PeakedNode IsNodePresent(string raw)
        {
            var trimmed = raw.TrimStart();

            if (trimmed.StartsWith("'''")) return new PeakedNode { NodePresent = true, NodeIsLiteral = true, Start = "'''", End = "'''" };
            if (trimmed.StartsWith("'")) return new PeakedNode { NodePresent = true, NodeIsLiteral = true, Start = "'", End = "'" };
            if (trimmed.StartsWith("<")) return new PeakedNode { NodePresent = true, NodeIsLiteral = false, Start = "<", End = ">" };
            return new PeakedNode { NodePresent = false, NodeIsLiteral = true };
        }

        #endregion
    }
}
