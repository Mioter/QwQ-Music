using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Avalonia.Data.Converters;

namespace QwQ_Music.Converter;

// 进行数学计算的转换器。
// 在mathEquation中使用@VALUE作为绑定值的占位符，允许全大写与全小写。
// 操作符顺序：先括号，然后根据优先级从左到右进行计算
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

    // 计算数学表达式的值
    private static double EvaluateExpression(string expression)
    {
        Stack<double> values = new(); // 存储数值
        Stack<char> operators = new(); // 存储操作符

        for (int i = 0; i < expression.Length; i++)
        {
            if (char.IsWhiteSpace(expression[i])) continue;

            if (char.IsDigit(expression[i]) || expression[i] == '.')
            {
                // 提取数字
                string numStr = ExtractNumber(expression, ref i);
                values.Push(double.Parse(numStr));
            }
            else
                switch (expression[i])
                {
                    case '(':
                        // 左括号入栈
                        operators.Push(expression[i]);
                        break;
                    case ')':
                        // 右括号，弹出所有操作符直到遇到左括号
                        while (operators.Count > 0 && operators.Peek() != '(')
                        {
                            values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
                        }
                        operators.Pop(); // 弹出左括号
                        break;
                    default:
                        if (AllOperators.Contains(expression[i]))
                        {
                            // 处理操作符优先级
                            while (
                                operators.Count > 0
                             && operators.Peek() != '('
                             && OperatorPrecedence[operators.Peek()] >= OperatorPrecedence[expression[i]]
                            )
                            {
                                values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
                            }
                            operators.Push(expression[i]);
                        }
                        break;
                }
        }

        // 处理剩余的操作符
        while (operators.Count > 0)
        {
            values.Push(ApplyOperator(operators.Pop(), values.Pop(), values.Pop()));
        }

        return values.Pop(); // 返回最终结果
    }

    // 提取数字
    private static string ExtractNumber(string expression, ref int index)
    {
        StringBuilder numStr = new();
        while (index < expression.Length && (char.IsDigit(expression[index]) || expression[index] == '.'))
        {
            numStr.Append(expression[index++]);
        }
        index--; // 回退一位
        return numStr.ToString();
    }

    // 应用操作符进行计算
    private static double ApplyOperator(char op, double b, double a)
    {
        return op switch
        {
            '+' => a + b,
            '-' => a - b,
            '*' => a * b,
            '/' => a / b,
            '%' => a % b,
            _ => throw new ArgumentException("无效的操作符"),
        };
    }

    #region IValueConverter Members

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // 检查参数是否为空
        if (parameter is not string mathEquation)
            throw new ArgumentNullException(nameof(parameter), "Parameter cannot be null");

        // 替换占位符并移除空格
        mathEquation = mathEquation.Replace(" ", "").Replace("@VALUE", value?.ToString()).Replace("@value", value?.ToString());

        // 解析并计算表达式
        try
        {
            return EvaluateExpression(mathEquation);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to evaluate the mathematical expression", ex);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    #endregion
}
