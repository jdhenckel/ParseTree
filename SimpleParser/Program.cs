using System;
using Console = System.Console;

namespace SimpleParser
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Simple Parser");
            var parser = new ParseTree();
            for (;;)
            {
                Console.WriteLine();
                Console.WriteLine("Enter an expression with: and, or, not, (, )");
                Console.WriteLine();
                var s = Console.ReadLine();
                if (s == null) continue;
                if (s == "exit" || s == "quit") break;
                if (s.StartsWith("setop "))
                {
                    ParseTree.OperatorList = s.Substring(6).Split(new [] {' '});
                    Console.WriteLine("new operators list [" + String.Join(", ", ParseTree.OperatorList) + "]");
                    continue;
                }
                var tokenList = ParseTree.Tokenize(s);
                Console.WriteLine();
                Console.WriteLine("Token List:");
                Console.WriteLine("[" + String.Join("],[", tokenList) + "]");

                // Do the parsing
                parser.Start(s);

                if (parser.ErrorList.Count > 1)
                {
                    Console.WriteLine();
                    Console.WriteLine("Errors:");
                    foreach (var e in parser.ErrorList)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Expressions");
                    foreach (var x in parser.Expressions)
                        Console.WriteLine(x);
                    Console.WriteLine();
                    Console.WriteLine("Logic");
                    Console.WriteLine(parser.Logic);
                    Console.WriteLine();
                    Console.WriteLine("Parse Tree");
                    Console.WriteLine(parser.Root);
                }
            }
        }
    }
}
