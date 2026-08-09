using System.Globalization;

namespace Finora.Application;

public static class DecimalCalculator
{
    public static decimal Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression)) throw new FormatException("Enter a calculation.");
        var parser = new Parser(expression);
        var value = parser.ParseExpression();
        parser.SkipWhitespace();
        if (!parser.End) throw new FormatException($"Unexpected character at position {parser.Position + 1}.");
        return value;
    }

    private sealed class Parser(string text)
    {
        private int _position;
        public int Position => _position;
        public bool End => _position >= text.Length;

        public decimal ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Match('+')) value = checked(value + ParseTerm());
                else if (Match('-')) value = checked(value - ParseTerm());
                else return value;
            }
        }

        private decimal ParseTerm()
        {
            var value = ParseFactor();
            while (true)
            {
                SkipWhitespace();
                if (Match('*')) value = checked(value * ParseFactor());
                else if (Match('/'))
                {
                    var divisor = ParseFactor();
                    if (divisor == 0m) throw new DivideByZeroException("Cannot divide by zero.");
                    value /= divisor;
                }
                else return value;
            }
        }

        private decimal ParseFactor()
        {
            SkipWhitespace();
            if (Match('+')) return ParseFactor();
            if (Match('-')) return checked(-ParseFactor());
            if (Match('('))
            {
                var value = ParseExpression();
                SkipWhitespace();
                if (!Match(')')) throw new FormatException("Missing closing parenthesis.");
                return value;
            }
            return ParseNumber();
        }

        private decimal ParseNumber()
        {
            SkipWhitespace();
            var start = _position;
            var seenSeparator = false;
            while (!End)
            {
                var c = text[_position];
                if (char.IsDigit(c)) { _position++; continue; }
                if ((c == '.' || c == ',') && !seenSeparator) { seenSeparator = true; _position++; continue; }
                break;
            }
            if (start == _position) throw new FormatException($"Expected a number at position {_position + 1}.");
            var token = text[start.._position].Replace(',', '.');
            if (!decimal.TryParse(token, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
                throw new FormatException($"Invalid number '{token}'.");
            return value;
        }

        public void SkipWhitespace()
        {
            while (!End && char.IsWhiteSpace(text[_position])) _position++;
        }

        private bool Match(char c)
        {
            if (!End && text[_position] == c) { _position++; return true; }
            return false;
        }
    }
}
