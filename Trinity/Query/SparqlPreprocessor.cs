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
// Copyright (c) Semiodesk GmbH 2016

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;

namespace Semiodesk.Trinity
{
    /// <summary>
    /// A simple SPARQL parser.
    /// </summary>
    /// <remarks>
    /// This class is primarily intended to support a limited range of query 
    /// preprocessing tasks such as setting the values of query parameters (@-variables)
    /// as quickly as possible. It does not aim to support the full SPARQL standard syntax.
    /// </remarks>
    public class SparqlPreprocessor : SparqlTokeniser
    {
        #region Members

        /// <summary>
        /// We use the list of tokens to generate the query string with bound parameters on demand.
        /// </summary>
        protected readonly List<IToken> Tokens = new List<IToken>();

        /// <summary>
        /// Type of the last read token.
        /// </summary>
        protected int PreviousTokenType;

        /// <summary>
        /// URIs of the graphs queried or manipulated by the query.
        /// </summary>
        public readonly HashSet<string> DefaultGraphs = new HashSet<string>();

        /// <summary>
        /// Namespace prefixes defined in the query.
        /// </summary>
        public readonly HashSet<string> DeclaredPrefixes = new HashSet<string>();

        /// <summary>
        /// Namespace prefixes referenced in the query.
        /// </summary>
        public readonly HashSet<string> UsedPrefixes = new HashSet<string>();

        // PARAMETERS

        /// <summary>
        /// Names of the bindable query parameters starting with '@'.
        /// </summary>
        public readonly HashSet<string> Parameters = new HashSet<string>();

        /// <summary>
        /// Bound literal values of the query parameters.
        /// </summary>
        public readonly Dictionary<string, string> ParameterValues = new Dictionary<string, string>();

        /// <summary>
        /// Token types of the query parameters.
        /// </summary>
        public readonly Dictionary<string, int> ParameterTypes = new Dictionary<string, int>();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <c>SparqlPreprocessor</c> class.
        /// </summary>
        /// <param name="input">A text reader.</param>
        /// <param name="syntax">SPARQL syntax level.</param>
        public SparqlPreprocessor(TextReader input, SparqlQuerySyntax syntax) :
            base(input, syntax)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts analyzing the SPARQL query.
        /// </summary>
        /// <param name="declarePrefixes">Trz to add prefix definitions for the namespaces used but not declared in the query.</param>
        public void Process(bool declarePrefixes)
        {
            IToken token;

            do
            {
                token = GetNextToken();

                Tokens.Add(token);
            }
            while (token.TokenType != Token.EOF);

            // Declare the globally registered RDF namespace prefixes used in the query.
            if (!declarePrefixes)
            {
                return;
            }

            foreach (string prefix in UsedPrefixes)
            {
                if (OntologyDiscovery.Namespaces.ContainsKey(prefix))
                {
                    Uri uri = OntologyDiscovery.Namespaces[prefix];

                    AddPrefix(prefix, uri);
                }
                else
                {
                    string msg = string.Format("The prefix '{0}' is not registered with any ontology in app.config", prefix);

                    throw new KeyNotFoundException(msg);
                }
            }
        }

        /// <summary>
        /// Gets the next parseable Token from the Input or raises an Error.
        /// </summary>
        /// <returns></returns>
        public override IToken GetNextToken()
        {
            IToken token;

            // We read all parameter tokens from the input before calling the base method.
            // This prevents the base method from throwing exceptions for invalid input.
            do
            {
                token = TryGetQueryParameter();

                if (token != null)
                {
                    Tokens.Add(token);
                }
            }
            while (token != null);

            // Sometimes the GetNextToken() function does not report the correct last (previous) token type.
            // We remeber the token type that has been read _before_ the current one.
            PreviousTokenType = LastTokenType;

            // Now proceed into the base class.
            token = base.GetNextToken();

            switch(token.TokenType)
            {
                case Token.PREFIX:
                    {
                        // We add the namespace prefix of the qualified name to the set of declared prefixes.
                        string prefix = token.Value.Split(':').FirstOrDefault();

                        if (!string.IsNullOrEmpty(prefix))
                        {
                            DeclaredPrefixes.Add(prefix);
                        }

                        break;
                    }
                case Token.QNAME:
                    {
                        // The function may substitute the token for a Parameter token..
                        token = ProcessQName(token);

                        break;
                    }
                case Token.URI:
                    {
                        token = ProcessUri(token);

                        break;
                    }
            }

            return token;
        }

        private IToken TryGetQueryParameter()
        {
            // Get the next character.
            char next = Peek();

            while (Char.IsWhiteSpace(next))
            {
                this.DiscardWhiteSpace();

                next = Peek();
            }

            if(next == '@' && LastTokenType != Token.LITERAL)
            {
                StartNewToken();

                while(next == '@' || Char.IsLetterOrDigit(next))
                {
                    ConsumeCharacter();

                    next = Peek();
                }

                // Add the parameter name to the list of parameters.
                Parameters.Add(Value);

                int parameterType = CustomToken.PARAMETER;

                switch (LastTokenType)
                {
                    case Token.FROM:
                    case Token.FROMNAMED:
                    {
                        parameterType = CustomToken.GRAPHPARAMETER;

                        break;
                    }
                }

                // Remember the parameter type.
                ParameterTypes[Value] = parameterType;

                return new ParameterToken(parameterType, Value, CurrentLine, StartPosition, EndPosition);
            }

            return null;
        }

        private IToken ProcessQName(IToken token)
        {
            // We add the namespace prefix of the qualified name to the set of used prefixes.
            string prefix = token.Value.Split(':').FirstOrDefault();

            if (!string.IsNullOrEmpty(prefix) && !DeclaredPrefixes.Contains(prefix))
            {
                UsedPrefixes.Add(prefix);
            }

            switch (PreviousTokenType)
            {
                case Token.FROM:
                case Token.FROMNAMED:
                    {
                        // If the qualified name references a graph, add it to the list of default graphs.
                        DefaultGraphs.Add(token.Value);
                        
                        break;
                    }
            }

            return token;
        }

        private IToken ProcessUri(IToken token)
        {
            if (PreviousTokenType == Token.FROM || PreviousTokenType == Token.FROMNAMED)
            {
                // If the URI references a graph, add it to the list of default graphs.
                DefaultGraphs.Add(token.Value);
            }

            return token;
        }

        private void AddPrefix(string prefix, Uri uri)
        {
            Tokens.Insert(0, new PrefixToken(string.Format("{0}: <{1}>", prefix, uri), -1, -1, -1));
            Tokens.Insert(0, new PrefixDirectiveToken(-1, -1));
        }

        /// <summary>
        /// Add FROM definition to the query.
        /// </summary>
        /// <param name="uri">URI of the graph.</param>
        public void AddDefaultGraph(Uri uri)
        {
            AddGraph(uri, new FromKeywordToken(-1, -1));
        }

        /// <summary>
        /// Add a FROM NAMED definition to the query.
        /// </summary>
        /// <param name="uri">URI of the graph.</param>
        public void AddNamedGraph(Uri uri)
        {
            AddGraph(uri, new FromNamedKeywordToken(-1, -1));
        }

        private void AddGraph(Uri uri, IToken token)
        {
            string u = uri.OriginalString;

            if (DefaultGraphs.Contains(u))
            {
                return;
            }

            DefaultGraphs.Add(u);

            // Try to append the dataset clause before the outermost WHERE.
            int i = Tokens.FindIndex(t => t.TokenType == Token.WHERE);

            if (i == -1)
            {
                // If there is none, try to put it before the first left curly bracket.
                i = Tokens.FindIndex(t => t.TokenType == Token.LEFTCURLYBRACKET);

                if (i == -1)
                {
                    // If there is none, add it at the end of the query.
                    i = Tokens.Count - 1;
                }
            }

            Tokens.Insert(i, new UriToken(string.Format("<{0}>", uri.OriginalString), -1, -1, -1));
            Tokens.Insert(i, token);
        }

        /// <summary>
        /// Gets the PREFIX definitions in the query.
        /// </summary>
        /// <returns></returns>
        public string GetPrefixDeclarations()
        {
            StringBuilder resultBuilder = new StringBuilder();

            foreach (IToken token in Tokens)
            {
                switch(token.TokenType)
                {
                    case Token.PREFIXDIRECTIVE:
                    case Token.PREFIX:
                        {
                            resultBuilder.Append(token.Value);
                            resultBuilder.Append(' ');

                            break;
                        }
                    case Token.ASK:
                    case Token.DESCRIBE:
                    case Token.SELECT:
                    case Token.CONSTRUCT:
                        {
                            return resultBuilder.ToString();
                        }
                }
            }

            return resultBuilder.ToString();
        }

        /// <summary>
        /// Set the value for a query parameter which is preceeded by '@'.
        /// </summary>
        /// <param name="parameter">The parameter name including the '@'.</param>
        /// <param name="value">The paramter value.</param>
        public void Bind(string parameter, object value)
        {
            if (string.IsNullOrEmpty(parameter))
            {
                throw new ArgumentException("Empty or null value for SPARQL query parameter.");
            }
            else if (parameter[0] != '@')
            {
                throw new ArgumentException("SPARQL query parameters must start with '@'.");
            }
            else if (value == null)
            {
                throw new ArgumentNullException("SPARQL query parameter values may not be null.");
            }
            else if (ParameterTypes[parameter] == CustomToken.GRAPHPARAMETER)
            {
                if (ParameterValues.ContainsKey(parameter))
                {
                    string g = ParameterValues[parameter];

                    DefaultGraphs.Remove(g);
                }

                string uri = SparqlSerializer.SerializeValue(value);
                string url = uri.TrimStart('<').TrimEnd('>');

                if (DefaultGraphs.Contains(url))
                {
                    throw new ArgumentException("FROM parameter value {0} is already set. Have you previously set the model property of the query?", uri);
                }

                DefaultGraphs.Add(url);

                ParameterValues[parameter] = uri;
            }
            else
            {
                ParameterValues[parameter] = SparqlSerializer.SerializeValue(value);
            }
        }

        /// <summary>
        /// Return the query with all bound variables.
        /// </summary>
        /// <param name="outputLevel">Level of the sub graph to be returned (0 := entire query).</param>
        /// <returns></returns>
        protected string Serialize(int outputLevel = 0)
        {
            StringBuilder outputBuilder = new StringBuilder();

            // The current iteration depth.
            int level = 0;

            for (int i = 0; i < Tokens.Count; i++)
            {
                IToken token = Tokens[i];

                if (token.TokenType == Token.LEFTCURLYBRACKET)
                {
                    level += 1;

                    // Do not output the brackets at the output level.
                    if (level == outputLevel) continue;
                }
                else if (token.TokenType == Token.RIGHTCURLYBRACKET)
                {
                    level -= 1;
                }

                if (level < outputLevel)
                {
                    continue;
                }

                switch (token.TokenType)
                {
                    default:
                        {
                            outputBuilder.Append(token.Value);
                            outputBuilder.Append(' ');

                            break;
                        }
                    case Token.URI:
                        {
                            outputBuilder.AppendFormat("<{0}> ", token.Value);

                            break;
                        }
                    case Token.LITERAL:
                    case Token.LONGLITERAL:
                        {
                            IToken next = i < Tokens.Count ? Tokens[i + 1] : null;

                            outputBuilder.Append(SparqlSerializer.SerializeString(token.Value));

                            if(next.TokenType != Token.LANGSPEC)
                            {
                                outputBuilder.Append(' ');
                            }

                            break;
                        }
                    case Token.LANGSPEC:
                        {
                            outputBuilder.AppendFormat("@{0} ", token.Value);

                            break;
                        }
                    case Token.HATHAT:
                        {
                            outputBuilder.Append(token.Value);

                            break;
                        }
                    case CustomToken.PARAMETER:
                    case CustomToken.GRAPHPARAMETER:
                        {
                            string key = token.Value;

                            if (!ParameterValues.ContainsKey(key))
                            {
                                string msg = string.Format("No value set for query parameter {0}.", key);

                                throw new KeyNotFoundException(msg);
                            }

                            outputBuilder.Append(ParameterValues[key]);
                            outputBuilder.Append(' ');

                            break;
                        }
                }
            }

            return outputBuilder.ToString().Trim();
        }

        /// <summary>
        /// Returns the entire query string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Serialize();
        }

        #endregion
    }

    internal class ParameterToken : BaseToken
    {
        #region Constructors

        public ParameterToken(IToken token)
            : base(CustomToken.PARAMETER, token.Value, token.StartLine, token.EndLine, token.StartPosition, token.EndPosition)
        {
        }

        public ParameterToken(int tokenType, string value, int startLine, int startPos, int endPos)
            : base(tokenType, value, startLine, startLine, startPos, endPos)
        {
        }

        #endregion
    }

    internal static class CustomToken
    {
        public const int PARAMETER = 1001;
        public const int GRAPHPARAMETER = 1002;
    }

    /// <summary>
    /// Graph pattern scope of variables defined in a SPARQL query.
    /// </summary>
    public enum SparqlQueryVariableScope
    {
        /// <summary>
        /// A variable accessible in all levels of the query.
        /// </summary>
        Global,
        /// <summary>
        /// A locally accessible variable.
        /// </summary>
        Default
    }
}
