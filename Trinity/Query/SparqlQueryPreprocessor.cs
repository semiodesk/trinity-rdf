using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VDS.RDF.Parsing;
using VDS.RDF.Parsing.Tokens;

namespace Semiodesk.Trinity
{
    public class SparqlQueryPreprocessor : SparqlPreprocessor
    {
        #region Members

        /// <summary>
        /// The SPARQL query form, i.e. ASK, DESCRIBE, SELECT, CONSTRUCT.
        /// </summary>
        public SparqlQueryType QueryType { get; protected set; }

        private int _nestingLevel;

        // --- STATEMENT VARIABLES

        private bool _parseVariables;

        private SparqlQueryVariableScope _variableScope = SparqlQueryVariableScope.Default;

        public bool QueryProvidesStatements { get; protected set; }

        public readonly List<string> GlobalScopeVariables = new List<string>();

        public readonly List<string> InScopeVariables = new List<string>();

        // --- SOLUTION MODIFIERS

        private IToken _offsetValueToken;

        private IToken _limitValueToken;

        public bool IsOrdered { get; protected set; }

        #endregion

        #region Constructors

        public SparqlQueryPreprocessor(TextReader input, SparqlQuerySyntax syntax)
            : base(input, syntax)
        {
            QueryType = SparqlQueryType.Unknown;
        }

        #endregion

        #region Methods

        public override IToken GetNextToken()
        {
            IToken token = base.GetNextToken();

            switch (token.TokenType)
            {
                case Token.ASK:
                    {
                        QueryType = SparqlQueryType.Ask;

                        if (_nestingLevel == 0)
                        {
                            _parseVariables = false;
                            QueryProvidesStatements = false;
                        }

                        break;
                    }
                case Token.DESCRIBE:
                    {
                        QueryType = SparqlQueryType.Describe;

                        if (_nestingLevel == 0)
                        {
                            _parseVariables = false;
                            QueryProvidesStatements = true;
                        }

                        break;
                    }
                case Token.SELECT:
                    {
                        QueryType = SparqlQueryType.Select;

                        if (_nestingLevel == 0)
                        {
                            _parseVariables = true;
                            QueryProvidesStatements = false;
                        }

                        break;
                    }
                case Token.CONSTRUCT:
                    {
                        QueryType = SparqlQueryType.Construct;

                        if (_nestingLevel == 0)
                        {
                            _parseVariables = false;
                            QueryProvidesStatements = true;
                        }

                        break;
                    }
                case Token.EOF:
                    {
                        if (_variableScope == SparqlQueryVariableScope.Global && GlobalScopeVariables.Count == 3)
                        {
                            // NOTE: This does not yet take into account that all variables need to be in S|P|O positions.
                            QueryProvidesStatements = true;
                        }

                        break;
                    }
                case Token.LEFTCURLYBRACKET:
                    {
                        _nestingLevel += 1;

                        if (_parseVariables)
                        {
                            ProcessInScopeVariables(token);
                        }

                        break;
                    }
                case Token.RIGHTCURLYBRACKET:
                    {
                        _nestingLevel -= 1;

                        // Do not parse variables which are being used in solution modifiers.
                        _parseVariables &= _nestingLevel > 0;

                        if (_parseVariables)
                        {
                            ProcessInScopeVariables(token);
                        }

                        break;
                    }
                case Token.DOT:
                    {
                        if (_parseVariables)
                        {
                            ProcessInScopeVariables(token);
                        }

                        break;
                    }
                case Token.ALL:
                    {
                        // The query has a wild-card selector. We collect all in-scope variables of
                        // the query to determine if it provides triples/statements.
                        _variableScope = SparqlQueryVariableScope.Global;

                        break;
                    }
                case Token.VARIABLE:
                    {
                        if(_parseVariables)
                        {
                            if (_nestingLevel == 0)
                            {
                                GlobalScopeVariables.Add(token.Value);
                            }
                            else
                            {
                                InScopeVariables.Add(token.Value);

                                if (_variableScope == SparqlQueryVariableScope.Global)
                                {
                                    // If we have a wildcard selector '*', we accumulate all variables of 
                                    // the query as global variables. After parsing, there must only be 
                                    // three for providing triples.
                                    GlobalScopeVariables.Add(token.Value);
                                }
                            }
                        }

                        break;
                    }
                case Token.ORDERBY:
                    {
                        IsOrdered = true;

                        break;
                    }
                case Token.LITERAL:
                    {
                        if (_nestingLevel == 0)
                        {
                            switch (PreviousTokenType)
                            {
                                case Token.OFFSET:
                                    {
                                        _offsetValueToken = token;
                                        break;
                                    }
                                case Token.LIMIT:
                                    {
                                        _limitValueToken = token;
                                        break;
                                    }
                            }
                        }

                        break;
                    }
            }

            return token;
        }

        private void ProcessInScopeVariables(IToken token)
        {
            if(GlobalScopeVariables.Count == 3)
            {
                if (!QueryProvidesStatements)
                {
                    // We compare the in-scope variables with the global variables. The query only
                    // provides statements if there is one triple pattern that contains the global
                    // variables in excactly the same order.
                    QueryProvidesStatements = Enumerable.SequenceEqual(GlobalScopeVariables, InScopeVariables);
                }
            }
            else
            {
                QueryProvidesStatements = false;
            }

            // After parsing a triple pattern, clear the in-scope variable cache.
            InScopeVariables.Clear();
        }

        /// <summary>
        /// Adds a LIMIT clause to the query in order to restrict it to put an upper bound on the number of solutions returned. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        public void SetLimit(int limit)
        {
            string value = limit.ToString();

            if (_limitValueToken == null)
            {
                Tokens.Insert(Tokens.Count - 1, new OffsetKeywordToken(-1, -1));
                Tokens.Insert(Tokens.Count - 1, new PlainLiteralToken(value, -1, -1, -1));
            }
            else
            {
                int i = Tokens.IndexOf(_limitValueToken);

                _limitValueToken = new PlainLiteralToken(value, -1, -1, -1);

                Tokens[i] = _limitValueToken;
            }
        }

        /// <summary>
        /// Adds an OFFSET clause to the query which causes the solutions generated to start after the specified number of solutions. 
        /// </summary>
        /// <param name="model">The number of return values.</param>
        public void SetOffset(int offset)
        {
            string value = offset.ToString();

            if (_offsetValueToken == null)
            {
                Tokens.Insert(Tokens.Count - 1, new OffsetKeywordToken(-1, -1));
                Tokens.Insert(Tokens.Count - 1, new PlainLiteralToken(value, -1, -1, -1));
            }
            else
            {
                int i = Tokens.IndexOf(_offsetValueToken);

                _offsetValueToken = new PlainLiteralToken(value, -1, -1, -1);

                Tokens[i] = _offsetValueToken;
            }
        }

        public string GetRootGraphPattern()
        {
            return Serialize(1);
        }

        public string GetOrderByClause()
        {
            // We want the next token after the last closing curly bracket.
            int i = Tokens.FindLastIndex(t => t.TokenType == Token.RIGHTCURLYBRACKET) + 1;

            // If i == 0, then FindLastIndex returned -1;
            if (i == 0)
            {
                return "";
            }

            StringBuilder resultBuilder = new StringBuilder();

            bool orderby = false;

            while (i < Tokens.Count)
            {
                IToken token = Tokens[i];

                orderby = token.TokenType == Token.ORDERBY || orderby && token.TokenType == Token.VARIABLE;

                if (orderby)
                {
                    resultBuilder.Append(token.Value);
                    resultBuilder.Append(' ');
                }
                else if (resultBuilder.Length > 0)
                {
                    break;
                }

                i++;
            }

            return resultBuilder.ToString();
        }

        #endregion
    }
}
