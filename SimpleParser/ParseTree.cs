using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleParser
{
    /// <summary>  Parser for boolean expressions </summary>
    /// <remarks> 
    ///    This parser accepts the following:
    ///    Logical connectives : and or not
    ///    Predicates can be only
    ///    - Attribute,
    ///    - Attribute Operator Value, or   
    ///    - Attribute Operator (Value, ...), or   
    ///    - Attribute.Method(Value, ...).
    ///    The list of valid Operator/Method is defined by the caller (and must be sorted descending by length).
    ///    The Method must start with a letter. And Operator must not start with a letter.
    ///    The Value can be a string, or integer, bool, null, or a complex expression: (12 + (1==2 ? 3 : 5))
    ///    The only restriction is that the value must not contain: and, or, not. However the value is allowed
    ///    to contain &&, ||, !, which do the same thing.
    ///    
    ///    To use it:
    ///    1. Set the OperatorList  -- first time only
    ///    2. call new ParseTree()
    ///    3. (optional) call Tokenize(expression) to preview the first step of parser
    ///    4. call Start(expression) to run the full parse    
    ///    5. look at the ErrorList to see errors  
    ///    6. if no errors, look at Logic and Expressions to get result.
    ///    7. use Root to look at the parse tree
    /// </remarks>

    public class ParseTree
    {
        public TreeNode Root { get; set; }
        public string Logic { get; set; }
        public List<SimpleExpression> Expressions { get; set; }
        public List<string> ErrorList { get; set; }
        public List<string> TokenList { get; set; }
        private int i;  // position in the token list

        /// <summary>   Gets or sets a list of operators. </summary>
        /// <remarks> Caller must supply a list of valid operators. 
        /// The list must be sorted by length from longest to shortest.
        /// The list must not include the logic operators : and, or, not, (, ).
        /// The operators that start with letters are assumed to be method names (such as "Contains").
        /// </remarks>
        /// <value> A List of operators. </value>

        public static string[] OperatorList { get; set; }

        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>   Constructor. </summary>
        /// <param name="query"> Optional query. if supplied is the same as calling Start(query). </param>
        public ParseTree(string query = null)
        {
            Expressions = new List<SimpleExpression>();
            ErrorList = new List<string>();

            if (!String.IsNullOrWhiteSpace(query))
            {
                Start(query);
            }
        }

        public override string ToString()
        {
            return "ParseTree: Logic " + Logic +
                   ", Expressions " + String.Join(", ", Expressions) + ", Error " + ErrorList.Count;
        }

        /// <summary>Clears the results.</summary>
        public void Reset()
        {
            Root = null;
            Logic = "";
            Expressions.Clear();
            ErrorList.Clear();
        }

        /*
         * This is a recursive descent parser based on the following grammar.
         *   S -> D
         *   D -> C MD
         *   MD -> or D | nil
         *   C -> T MC
         *   MC -> and C | nil
         *   T -> P | (D) | !T
         *   P -> attribute operator value or expression
         *   P -> attribute.MethodName(value or expression)
         *   P -> attribute.MethodName()
         */
        public void Start(string query)
        {
            Reset();
            TokenList = Tokenize(query);
            i = 0;
            Root = Disjunct();
            if (i < TokenList.Count)
            {
                ErrorList.Add(CreateMessage(i, "Parser stopped unexpectedly"));
            }
        }


        /// <summary>
        /// Return true if the string starts with a letter
        /// </summary>
        public static bool StartsWithALetter(string s)
        {
            return !String.IsNullOrWhiteSpace(s) && Char.IsLetter(s[0]);
        }


        /// <summary>
        /// Break up a string into tokens so that it can be parsed.
        /// </summary>
        /// A token can be 'and', 'or', (, ), 'not', any sequence of <>=, or any set of characters delimited by whitespace.
        /// Also, in order to simplify the parser, merge any (xxxx) that does not contain 'and' or 'or' into a single token.
        /// <param name="query">query</param>
        public static List<string> Tokenize(string query)
        {
            if (OperatorList == null)
            {
                // Default list, if caller doesn't supply one
                OperatorList = new[] { "==", "!=", "<=", ">=", "<", ">", "in", "Contains", "StartsWith", "EndsWith" };
            }

            string pattern = "(";
            for (int i = 0; i < OperatorList.Length; ++i)
            {
                if (!StartsWithALetter(OperatorList[i]))
                {
                    pattern += Regex.Escape(OperatorList[i]) + "|";
                }
            }
            pattern += "\\bnot\\b|\\band\\b|\\bor\\b|\\(|\\))";
            var logicOperator = new[] { "not", "and", "or", "(" };
            var raw = Regex.Split(query, pattern);
            var list = new List<string>();
            for (int i = 0; i < raw.Length; ++i)
            {
                if (!String.IsNullOrWhiteSpace(raw[i]))
                {
                    raw[i] = raw[i].Trim();
                    list.Add(raw[i]);
                    if (raw[i] == "(" && list.Count > 1 && !logicOperator.Contains(list[list.Count - 2]))
                    {
                        // Merge everything between the matching parentheses into a single token
                        int n = 1;
                        var curr = raw[i];
                        for (int j = i + 1; j < raw.Length; ++j)
                        {
                            curr += raw[j];
                            if (raw[j] == "(")
                                ++n;
                            else if (raw[j] == ")" && --n == 0)
                            {
                                list[list.Count - 1] = curr;
                                i = j;
                                break;
                            }
                            else if (raw[j] == "and" || raw[j] == "or")
                                break;
                        }
                    }
                }
            }

            return list;
        }

        TreeNode Disjunct()
        {
            var a = Conjunct();
            var b = MoreDisjunct();
            return b == null ? a : new TreeNode() {Name = "or", Left = a, Right = b};
        }

        TreeNode MoreDisjunct()
        {
            if (i < TokenList.Count && TokenList[i] == "or")
            {
                Logic += " or ";
                ++i;
                return Disjunct();
            }
            return null;
        }

        TreeNode Conjunct()
        {
            var a = Term();
            var b = MoreConjunct();
            return b == null ? a : new TreeNode() { Name = "and", Left = a, Right = b };
        }

        TreeNode MoreConjunct()
        {
            if (i < TokenList.Count && TokenList[i] == "and")
            {
                Logic += " and ";
                ++i;
                return Conjunct();
            }
            return null;
        }

        TreeNode Term()
        {
            if (i < TokenList.Count && TokenList[i] == "not")
            {
                Logic += "not ";
                ++i;
                var a = Term();
                return new TreeNode() { Name = "not", Left = a };
            }
            if (i < TokenList.Count && TokenList[i] == "(")
            {
                Logic += "(";
                ++i;
                var a = Disjunct();
                if (i < TokenList.Count && TokenList[i] == ")")
                {
                    Logic += ")";
                    ++i;
                }
                else
                {
                    ErrorList.Add(CreateMessage(i, "Expected close parenthesis"));
                }
                return a;
            }
            return Predicate();
        }

        TreeNode Predicate()
        {
            for (int n = 0; ; ++n, ++i)
            {
                // Scan for the "follow-set" of the Predicate
                if (i == TokenList.Count || TokenList[i] == ")" || TokenList[i] == "and" || TokenList[i] == "or")
                {
                    var a = AddExpression(i - n, n);
                    return new TreeNode {Name = a};
                }
            }
        }


        /// <summary>
        /// Create a new expression using the tokens i to i+n-1
        /// </summary>
        /// <param name="t">the first token in the expression</param>
        /// <param name="n">the number of tokens in the expression</param>
        string AddExpression(int t, int n)
        {
            if (n == 0)
            {
                ErrorList.Add(CreateMessage(t, "Expected attribute expression"));
                Logic += "?";
                return "?";
            }

            string nextId = Alphabet.Substring(Expressions.Count, 1);

            string attr = "";
            string oper = OperatorList[0];
            string valu = "";

            if (n == 1)
            {
                attr = TokenList[t];
                oper = "==";
                valu = "true";
            }
            else if (n == 2)
            {
                int k = TokenList[t].LastIndexOf('.');
                if (k > 1 && k < TokenList[t].Length - 1 && TokenList[t + 1].StartsWith("(") && TokenList[t + 1].EndsWith(")"))
                {
                    // for example foo.Name.StartsWith("miller")
                    attr = TokenList[t].Substring(0, k);
                    oper = TokenList[t].Substring(k + 1);
                    valu = TokenList[t + 1].Substring(1, TokenList[t + 1].Length - 2);
                }
                else
                {
                    attr = TokenList[t];
                    valu = TokenList[t + 1];
                    ErrorList.Add(CreateMessage(t + 1, "Expected comparison value"));
                }
            }
            else
            {
                attr = TokenList[t];
                oper = TokenList[t + 1];
                valu = String.Join("", TokenList.ToArray(), t + 2, n - 2);
                if (!OperatorList.Contains(oper))
                {
                    ErrorList.Add(CreateMessage(t + 1, "Unknown operator"));
                }
            }

            var ex = new SimpleExpression
            {
                Id = nextId,
                Attribute = attr,
                Operator = oper,
                Value = valu
            };
            Expressions.Add(ex);
            Logic += nextId;
            return ex.Name();
        }

        /// <summary>   Creates an error message. </summary>
        /// <param name="msg"> The description of the error. </param>
        /// <returns>   The message that tells what went wrong, and which token caused it. </returns>
        string CreateMessage(int pos, string msg)
        {
            var s = msg + " at ";
            for (int j = 0; j < TokenList.Count; ++j)
            {
                if (pos == j) s += " ###>";
                s += ' ' + TokenList[j];
                if (pos == j) s += " <###";
            }
            if (pos == TokenList.Count) s += " ###> (end of input) <###";
            return s;
        }

    }
}
