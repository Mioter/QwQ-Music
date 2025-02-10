using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

public class CalcConverter : IValueConverter
{
    private static readonly char[] AllOperators =
    {
        '+', '-', '*', '/', '%', '(', ')',
    };
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

    private static double EvaluateExpression(string expression)
    {
        var values = new Stack<double>();
        var operators = new Stack<char>();

        for (int i = 0; i < expression.Length; i++)
        {
            char currentChar = expression[i];
            if (char.IsWhiteSpace(currentChar)) continue;

            if (char.IsDigit(currentChar) || currentChar == '.')
            {
                string numStr = ExtractNumber(expression, ref i);
                values.Push(double.Parse(numStr));
            }
            else switch (currentChar)
            {
                case '(':
                    operators.Push(currentChar);
                    break;
                case ')':
                    {
                        while (operators.Peek() != '(')
                        {
                            ApplyTopOperator(values, operators);
                        }
                        operators.Pop(); // Remove '('
                        break;
                    }
                default:
                    {
                        if (AllOperators.Contains(currentChar))
                        {
                            // Handle unary minus
                            if (currentChar == '-' && IsUnaryOperator(expression, i))
                            {
                                values.Push(0); // Treat unary '-' as 0 - value
                            }

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
        // Check if the '-' is a unary operator
        if (index == 0) return true;
        char prevChar = expression[index - 1];
        return prevChar == '(' || AllOperators.Contains(prevChar);
    }

    private static void ApplyTopOperator(Stack<double> values, Stack<char> operators)
    {
        char op = operators.Pop();
        double b = values.Pop();
        double a = values.Count > 0 ? values.Pop() : 0; // Handle case with insufficient values
        values.Push(ApplyOperator(op, a, b));
    }

    private static string ExtractNumber(string expression, ref int index)
    {
        var numStr = new StringBuilder();
        while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.' || expression[index] == '-'))
        {
            // Handle negative numbers
            if (expression[index] == '-' && numStr.Length > 0)
                break;
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
            _ => throw new ArgumentException("Invalid operator: " + op),
        };
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is not string mathEquation)
            throw new ArgumentNullException(nameof(parameter));

        string equation = mathEquation.Replace(" ", "")
            .Replace("@VALUE", value?.ToString())
            .Replace("@value", value?.ToString());

        try
        {
            return EvaluateExpression(equation);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("表达式计算失败", ex);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
