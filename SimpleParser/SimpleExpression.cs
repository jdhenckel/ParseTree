namespace SimpleParser
{
    public class SimpleExpression
    {
        public string Id;
        public string Attribute;
        public string Operator;
        public string Value;

        public override string ToString()
        {
            return "[" + Id + ": " + Attribute + ", " + Operator + ", " + Value + "]";
        }

        public string Name()
        {
            if (Operator == "==" && Value == "true") return Attribute;
            return Attribute + " " + Operator + " " + Value;
        }
    }
}
