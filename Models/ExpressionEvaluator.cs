using System;
using System.Globalization;

namespace Calc.Models;

/// <summary>
/// 递归下降表达式解析器，支持复杂科学计算。
/// 支持：+ - * / % ^ ( )、一元负号、阶乘 !、
/// 函数：sin cos tan asin acos atan sinh cosh tanh ln log log2 sqrt cbrt abs exp floor ceil round
/// 常量：π(pi) e；支持 DEG/RAD 角度模式。
/// </summary>
public class ExpressionEvaluator
{
    public bool DegreeMode { get; set; } = false;

    /// <summary>上次计算结果，供表达式中的 ans 引用</summary>
    public double LastAnswer { get; set; }

    private string _input = "";
    private int _pos;

    public double Evaluate(string expression)
    {
        _input = Normalize(expression);
        _pos = 0;
        if (string.IsNullOrWhiteSpace(_input))
            throw new ArgumentException("表达式为空");

        double result = ParseExpression();
        SkipSpaces();
        if (_pos < _input.Length)
            throw new FormatException($"无法识别的字符: '{_input[_pos]}' (位置 {_pos + 1})");
        return result;
    }

    private static string Normalize(string expr) => expr
        .Replace("×", "*").Replace("÷", "/").Replace("−", "-")
        .Replace("π", "pi").Replace("√", "sqrt")
        .Replace("，", ",").Replace(" ", "");

    private void SkipSpaces()
    {
        while (_pos < _input.Length && _input[_pos] == ' ') _pos++;
    }

    private bool Match(char c)
    {
        SkipSpaces();
        if (_pos < _input.Length && _input[_pos] == c) { _pos++; return true; }
        return false;
    }

    private char Peek()
    {
        SkipSpaces();
        return _pos < _input.Length ? _input[_pos] : '\0';
    }

    // expression := term (('+' | '-') term)*
    private double ParseExpression()
    {
        double left = ParseTerm();
        while (true)
        {
            char c = Peek();
            if (c == '+') { _pos++; left += ParseTerm(); }
            else if (c == '-') { _pos++; left -= ParseTerm(); }
            else break;
        }
        return left;
    }

    // term := factor (('*' | '/' | '%') factor)*
    private double ParseTerm()
    {
        double left = ParseFactor();
        while (true)
        {
            char c = Peek();
            if (c == '*') { _pos++; left *= ParseFactor(); }
            else if (c == '/') { _pos++; left /= ParseFactor(); }
            else if (c == '%') { _pos++; left %= ParseFactor(); }
            else break;
        }
        return left;
    }

    // factor := unary ('^' factor)?  以及后缀 '!'（隐式乘法：2π、2(3+4)、3sin(x)）
    private double ParseFactor()
    {
        double left = ParseUnary();
        while (true)
        {
            char c = Peek();
            if (c == '^') { _pos++; left = Math.Pow(left, ParseFactor()); }
            else if (c == '!') { _pos++; left = Factorial(left); }
            else if (c == '(' || char.IsLetter(c)) // 隐式乘法
            {
                double right = ParseUnary();
                left *= right;
            }
            else break;
        }
        return left;
    }

    // unary := ('-' | '+') unary | primary
    private double ParseUnary()
    {
        char c = Peek();
        if (c == '-') { _pos++; return -ParseUnary(); }
        if (c == '+') { _pos++; return ParseUnary(); }
        return ParsePrimary();
    }

    private double ParsePrimary()
    {
        SkipSpaces();
        char c = Peek();

        if (c == '(')
        {
            _pos++;
            double v = ParseExpression();
            if (!Match(')')) throw new FormatException("缺少右括号 ')'");
            return v;
        }

        if (char.IsDigit(c) || c == '.')
            return ParseNumber();

        if (char.IsLetter(c))
            return ParseIdentifier();

        throw new FormatException($"意外的字符 '{c}'");
    }

    private double ParseNumber()
    {
        SkipSpaces();
        int start = _pos;
        while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.')) _pos++;
        if (start == _pos) throw new FormatException("缺少数字");
        string num = _input[start.._pos];
        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            throw new FormatException($"无效数字: {num}");
        return v;
    }

    private double ParseIdentifier()
    {
        SkipSpaces();
        int start = _pos;
        while (_pos < _input.Length && char.IsLetter(_input[_pos])) _pos++;
        string id = _input[start.._pos].ToLowerInvariant();

        switch (id)
        {
            case "pi":
                return Math.PI;
            case "e":
                return Math.E;
            case "ans":
                return LastAnswer;
        }

        // 函数调用：可带括号，也支持 "sin30" 形式
        double arg;
        if (Match('('))
        {
            arg = ParseExpression();
            // 支持多参数函数 round(a,b)
            if (Match(','))
            {
                double arg2 = ParseExpression();
                if (!Match(')')) throw new FormatException("缺少右括号 ')'");
                return ApplyFunction2(id, arg, arg2);
            }
            if (!Match(')')) throw new FormatException("缺少右括号 ')'");
        }
        else
        {
            arg = ParseUnary();
        }
        return ApplyFunction(id, arg);
    }

    private double ToAngle(double v) => DegreeMode ? v * Math.PI / 180.0 : v;
    private double FromAngle(double v) => DegreeMode ? v * 180.0 / Math.PI : v;

    private static double Factorial(double n)
    {
        if (n < 0 || n != Math.Floor(n)) throw new ArgumentException("阶乘仅支持非负整数");
        if (n > 170) throw new OverflowException("阶乘结果超出范围");
        double r = 1;
        for (int i = 2; i <= (int)n; i++) r *= i;
        return r;
    }

    private double ApplyFunction(string id, double x) => id switch
    {
        "sin" => Math.Round(Math.Sin(ToAngle(x)), 12),
        "cos" => Math.Round(Math.Cos(ToAngle(x)), 12),
        "tan" => Math.Round(Math.Tan(ToAngle(x)), 12),
        "asin" => FromAngle(Math.Asin(x)),
        "acos" => FromAngle(Math.Acos(x)),
        "atan" => FromAngle(Math.Atan(x)),
        "sinh" => Math.Sinh(x),
        "cosh" => Math.Cosh(x),
        "tanh" => Math.Tanh(x),
        "ln" => Math.Log(x),
        "log" => Math.Log10(x),
        "log2" => Math.Log2(x),
        "sqrt" => Math.Sqrt(x),
        "cbrt" => Math.Cbrt(x),
        "abs" => Math.Abs(x),
        "exp" => Math.Exp(x),
        "floor" => Math.Floor(x),
        "ceil" => Math.Ceiling(x),
        "round" => Math.Round(x),
        "sign" => Math.Sign(x),
        _ => throw new FormatException($"未知函数或常量: '{id}'")
    };

    private static double ApplyFunction2(string id, double a, double b) => id switch
    {
        "round" => Math.Round(a, (int)b),
        "pow" => Math.Pow(a, b),
        "log" => Math.Log(a, b),
        "mod" => a % b,
        "atan2" => Math.Atan2(a, b),
        _ => throw new FormatException($"未知双参数函数: '{id}'")
    };
}
