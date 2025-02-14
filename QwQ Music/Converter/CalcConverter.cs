using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

public class CalcConverter : IValueConverter
{
    private static readonly char[] AllOperators = ['+', '-', '*', '/', '%', '(', ')'];
    private static readonly Dictionary<char, int> OperatorPrecedence = new()
    {
        {
            '+', 1
        },
        {
            '-', 1
        },
        {
            '*', 2
        },
        {
            '/', 2
        },
        {
            '%', 2
        },
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string mathEquation)
            throw new ArgumentNullException(nameof(parameter), "Parameter must be a valid mathematical expression.");

        // 替换占位符并移除空格
        string equation = ReplacePlaceholders(mathEquation, value);

        try
        {
            return EvaluateExpression(equation);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to evaluate the expression.", ex);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static string ReplacePlaceholders(string equation, object? value) {
        if(value is bool boolValue) value = boolValue ? 1 : 0; 
        return equation.Replace(" ", "")
            .Replace("@VALUE", value?.ToString())
            .Replace("@value", value?.ToString());
    }

    private static double EvaluateExpression(string expression)
    {
        var values = new Stack<double>();
        var operators = new Stack<char>();

        for (int i = 0; i < expression.Length; i++)
        {
            char currentChar = expression[i];

            if (char.IsWhiteSpace(currentChar)) continue;

            if (char.IsDigit(currentChar) || currentChar == '.' || currentChar == '-' && IsUnaryOperator(expression, i))
            {
                string number = ExtractNumber(expression, ref i);
                values.Push(double.Parse(number, CultureInfo.InvariantCulture));
            }
            else
            {
                switch (currentChar)
                {
                    case '(':
                        operators.Push(currentChar);
                        break;

                    case ')':
                        while (operators.Peek() != '(')
                        {
                            ApplyTopOperator(values, operators);
                        }
                        operators.Pop(); // Remove '('
                        break;

                    default:
                        if (AllOperators.Contains(currentChar))
                        {
                            while (operators.Count > 0 && operators.Peek() != '(' &&
                                   OperatorPrecedence[operators.Peek()] >= OperatorPrecedence[currentChar])
                            {
                                ApplyTopOperator(values, operators);
                            }
                            operators.Push(currentChar);
                        }
                        break;
                }
            }
        }

        while (operators.Count > 0)
        {
            ApplyTopOperator(values, operators);
        }

        return values.Pop();
    }

    private static bool IsUnaryOperator(string expression, int index)
    {
        return index == 0 || expression[index - 1] == '(' || AllOperators.Contains(expression[index - 1]);
    }

    private static void ApplyTopOperator(Stack<double> values, Stack<char> operators)
    {
        char op = operators.Pop();
        double b = values.Pop();
        double a = values.Count > 0 ? values.Pop() : 0; // Handle unary operator case
        values.Push(ApplyOperator(op, a, b));
    }

    private static string ExtractNumber(string expression, ref int index)
    {
        var numStr = new StringBuilder();

        while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.' || expression[index] == '-' && numStr.Length == 0))
        {
            numStr.Append(expression[index++]);
        }

        index--; // Adjust index back
        return numStr.ToString();
    }

    private static double ApplyOperator(char op, double a, double b)
    {
        return op switch
        {
            '+' => a + b,
            '-' => a - b,
            '*' => a * b,
            '/' => a / b,
            '%' => a % b,
            _ => throw new ArgumentException($"Invalid operator: {op}"),
        };
    }
}
