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
    /// A _very_ simple SPARQL parser. This class is primarily intended to support
    /// a limited range of query preprocessing tasks such as setting the values of 
    /// query parameters (@-variables) as quickly as possible. It does not aim to 
    /// support the full SPARQL standard syntax.
    /// </summary>
    public class SparqlProcessor : SparqlTokeniser
    {
        #region Members

        protected readonly List<IToken> Tokens = new List<IToken>();

        public SparqlQueryType QueryType { get; protected set; }

        public readonly HashSet<string> DefaultGraphs = new HashSet<string>();

        public readonly HashSet<string> DeclaredPrefixes = new HashSet<string>();

        public readonly HashSet<string> UsedPrefixes = new HashSet<string>();

        protected int NestingLevel;

        // SOLUTION MODIFIERS

        public bool IsOrdered { get; protected set; }

        protected IToken OffsetValueToken;

        protected IToken LimitValueToken;

        // PARAMETERS

        public readonly HashSet<string> Parameters = new HashSet<string>();

        public readonly Dictionary<string, string> ParameterValues = new Dictionary<string, string>();

        // STATEMENT VARIABLES

        public bool ProvidesStatements { get; protected set; }

        protected bool ParseStatementVariables;

        public readonly List<string> GlobalScopeVariables = new List<string>();

        protected readonly List<string> InScopeVariables = new List<string>();

        protected SparqlQueryVariableScope VariableScope = SparqlQueryVariableScope.Default;

        #endregion

        #region Constructors

        public SparqlProcessor(TextReader input, SparqlQuerySyntax syntax) :
            base(input, syntax)
        {
            QueryType = SparqlQueryType.Unknown;
        }

        #endregion

        #region Methods

        public void Process()
        {
            IToken token;

            do
            {
                token = GetNextToken();

                Tokens.Add(token);
            }
            while (token.TokenType != Token.EOF);

            if(VariableScope == SparqlQueryVariableScope.Global && GlobalScopeVariables.Count == 3)
            {
                // NOTE: This does not yet take into account that all variables need to be in S|P|O positions.
                ProvidesStatements = true;
            }
        }

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
                case Token.ASK:
                    {
                        QueryType = SparqlQueryType.Ask;

                        if (NestingLevel == 0)
                        {
                            ParseStatementVariables = false;
                            ProvidesStatements = false;
                        }

                        break;
                    }
                case Token.DESCRIBE:
                    {
                        QueryType = SparqlQueryType.Describe;

                        if (NestingLevel == 0)
                        {
                            ParseStatementVariables = false;
                            ProvidesStatements = true;
                        }

                        break;
                    }
                case Token.SELECT:
                    {
                        QueryType = SparqlQueryType.Select;

                        if (NestingLevel == 0)
                        {
                            ParseStatementVariables = true;
                            ProvidesStatements = false;
                        }

                        break;
                    }
                case Token.CONSTRUCT:
                    {
                        QueryType = SparqlQueryType.Construct;

                        if (NestingLevel == 0)
                        {
                            ParseStatementVariables = false;
                            ProvidesStatements = true;
                        }

                        break;
                    }
                case Token.LEFTCURLYBRACKET:
                    {
                        NestingLevel += 1;

                        if (ParseStatementVariables)
                        {
                            ProcessInScopeVariables(token);
                        }

                        break;
                    }
                case Token.RIGHTCURLYBRACKET:
                    {
                        NestingLevel -= 1;

                        if (ParseStatementVariables)
                        {
                            ProcessInScopeVariables(token);
                        }

                        break;
                    }
                case Token.DOT:
                    {
                        if (ParseStatementVariables)
                        {
                            ProcessInScopeVariables(token);
                        }

                        break;
                    }
                case Token.ALL:
                    {
                        // The query has a wild-card selector. We collect all in-scope variables of
                        // the query to determine if it provides triples/statements.
                        VariableScope = SparqlQueryVariableScope.Global;

                        break;
                    }
                case Token.VARIABLE:
                    {
                        if (NestingLevel == 0)
                        {
                            GlobalScopeVariables.Add(token.Value);
                        }
                        else if (ParseStatementVariables)
                        {
                            if (VariableScope == SparqlQueryVariableScope.Global)
                            {
                                // If we have a wildcard selector '*', we accumulate all variables of 
                                // the query as global variables. After parsing, there must only be 
                                // three for providing triples.
                                GlobalScopeVariables.Add(token.Value);
                            }
                            else
                            {
                                InScopeVariables.Add(token.Value);
                            }
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
                case Token.ORDERBY:
                    {
                        IsOrdered = true;

                        break;
                    }
                case Token.LITERAL:
                    {
                        if (NestingLevel == 0)
                        {
                            switch (LastTokenType)
                            {
                                case Token.OFFSET:
                                    {
                                        OffsetValueToken = token;
                                        break;
                                    }
                                case Token.LIMIT:
                                    {
                                        LimitValueToken = token;
                                        break;
                                    }
                            }
                        }

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
                // Discard whitespace when not in a token.
                this.DiscardWhiteSpace();

                next = Peek();
            }

            if(next == '@')
            {
                StartNewToken();

                while(next == '@' || Char.IsLetterOrDigit(next))
                {
                    ConsumeCharacter();

                    next = Peek();
                }

                // Add the parameter name to the list of parameters.
                Parameters.Add(Value);

                LastTokenType = CustomToken.PARAMETER;

                return new ParameterToken(CustomToken.PARAMETER, Value, CurrentLine, StartPosition, EndPosition);
            }

            return null;
        }

        private void ProcessInScopeVariables(IToken token)
        {
            // We compare the in-scope variables with the global variables. The query only
            // provides statements if there is one triple pattern that contains the global
            // variables in excactly the same order.
            if (GlobalScopeVariables.Count == 3 && GlobalScopeVariables.Count == InScopeVariables.Count)
            {
                ProvidesStatements = Enumerable.SequenceEqual(GlobalScopeVariables, InScopeVariables);

                // We're done if we found a triple pattern that provides statement variables of global scope.
                ParseStatementVariables = !ProvidesStatements;
            }

            // After parsing a triple pattern, clear the in-scope variable cache.
            InScopeVariables.Clear();
        }

        private IToken ProcessQName(IToken token)
        {
            // We add the namespace prefix of the qualified name to the set of used prefixes.
            string prefix = token.Value.Split(':').FirstOrDefault();

            if (!string.IsNullOrEmpty(prefix) && !DeclaredPrefixes.Contains(prefix))
            {
                UsedPrefixes.Add(prefix);
            }

            switch(LastTokenType)
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
            if(LastTokenType == Token.FROM || LastTokenType == Token.FROMNAMED)
            {
                // If the URI references a graph, add it to the list of default graphs.
                DefaultGraphs.Add(token.Value);
            }

            return token;
        }

        public void AddPrefix(string prefix, Uri uri)
        {
            Tokens.Insert(0, new PrefixToken(string.Format("{0}: <{1}>", prefix, uri), -1, -1, -1));
            Tokens.Insert(0, new PrefixDirectiveToken(-1, -1));
        }

        public void AddDefaultGraph(Uri uri)
        {
            AddGraph(uri, new FromKeywordToken(-1, -1));
        }

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
        /// Adds a LIMIT clause to the query in order to restrict it to put an upper bound on the number of solutions returned. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        public void SetLimit(int limit)
        {
            string value = limit.ToString();

            if(LimitValueToken == null)
            {
                Tokens.Insert(Tokens.Count - 1, new OffsetKeywordToken(-1, -1));
                Tokens.Insert(Tokens.Count - 1, new PlainLiteralToken(value, -1, -1, -1));
            }
            else
            {
                int i = Tokens.IndexOf(LimitValueToken);

                LimitValueToken = new PlainLiteralToken(value, -1, -1, -1);

                Tokens[i] = LimitValueToken;
            }
        }

        /// <summary>
        /// Adds an OFFSET clause to the query which causes the solutions generated to start after the specified number of solutions. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        public void SetOffset(int offset)
        {
            string value = offset.ToString();

            if (OffsetValueToken == null)
            {
                Tokens.Insert(Tokens.Count - 1, new OffsetKeywordToken(-1, -1));
                Tokens.Insert(Tokens.Count - 1, new PlainLiteralToken(value, -1, -1, -1));
            }
            else
            {
                int i = Tokens.IndexOf(OffsetValueToken);

                OffsetValueToken = new PlainLiteralToken(value, -1, -1, -1);

                Tokens[i] = OffsetValueToken;
            }
        }

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

        public string GetRootGraphPattern()
        {
            return ToString(true);
        }

        public string GetOrderByClause()
        {
            // We want the next token after the last closing curly bracket.
            int i = Tokens.FindLastIndex(t => t.TokenType == Token.RIGHTCURLYBRACKET) + 1;

            // If i == 0, then FindLastIndex returned -1;
            if(i == 0)
            {
                return "";
            }

            StringBuilder resultBuilder = new StringBuilder();

            bool orderby = false;

            while(i < Tokens.Count)
            {
                IToken token = Tokens[i];

                orderby = token.TokenType == Token.ORDERBY || orderby && token.TokenType == Token.VARIABLE;

                if (orderby)
                {
                    resultBuilder.Append(token.Value);
                    resultBuilder.Append(' ');
                }
                else if(resultBuilder.Length > 0)
                {
                    break;
                }

                i++;
            }

            return resultBuilder.ToString();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        private string ToString(bool rootOnly)
        {
            StringBuilder queryBuilder = new StringBuilder();

            int nestingLevel = 0;

            bool append = !rootOnly;

            foreach (IToken token in Tokens)
            {
                switch (token.TokenType)
                {
                    default:
                        {
                            if(append)
                            {
                                queryBuilder.Append(token.Value);
                                queryBuilder.Append(' ');
                            }

                            break;
                        }
                    case Token.LEFTCURLYBRACKET:
                        {
                            if (append)
                            {
                                queryBuilder.Append(token.Value);
                                queryBuilder.Append(' ');
                            }

                            nestingLevel += 1;

                            append = !rootOnly || rootOnly && nestingLevel > 0;

                            break;
                        }
                    case Token.RIGHTCURLYBRACKET:
                        {
                            nestingLevel -= 1;

                            append = !rootOnly || rootOnly && nestingLevel > 0;

                            if (append)
                            {
                                queryBuilder.Append(token.Value);
                                queryBuilder.Append(' ');
                            }

                            break;
                        }
                    case Token.URI:
                        {
                            if (append)
                            {
                                queryBuilder.AppendFormat("<{0}> ", token.Value);
                            }

                            break;
                        }
                    case Token.LITERAL:
                        {
                            if (append)
                            {
                                queryBuilder.AppendFormat("'{0}' ", token.Value);
                            }

                            break;
                        }
                    case Token.HATHAT:
                        {
                            if (append)
                            {
                                queryBuilder.Append(token.Value);
                            }

                            break;
                        }
                    case CustomToken.PARAMETER:
                        {
                            if (append)
                            {
                                string key = token.Value;

                                if (!ParameterValues.ContainsKey(key))
                                {
                                    string msg = string.Format("No value set for query parameter {0}.", key);

                                    throw new KeyNotFoundException(msg);
                                }

                                queryBuilder.Append(ParameterValues[key]);
                            }

                            break;
                        }
                }
            }

            return queryBuilder.ToString();
        }

        #endregion
    }

    internal class ParameterToken : BaseToken
    {
        #region Members

        public readonly string Name;

        #endregion

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
    }

    public enum SparqlQueryVariableScope { Global, Default }
}
