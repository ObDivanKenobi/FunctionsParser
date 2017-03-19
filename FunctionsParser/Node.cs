using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace FunctionsParser
{
    enum BinaryOperations
    {
        Plus, Minus, Multiply, Divide, Power, Unset
    }
    static class BinaryOperationsExtention
    {
        public static string ToStr(this BinaryOperations op)
        {
            switch (op)
            {
                case BinaryOperations.Plus: { return "+"; }
                case BinaryOperations.Minus: { return "-"; }
                case BinaryOperations.Multiply: { return "*"; }
                case BinaryOperations.Divide: { return "/"; }
                default: { return "^"; }
            }
        }

        public static BinaryOperations FromString(string op)
        {
            switch (op)
            {
                case "+": return BinaryOperations.Plus;
                case "-": return BinaryOperations.Minus;
                case "*": return BinaryOperations.Multiply;
                case "/": return BinaryOperations.Divide;
                case "^": return BinaryOperations.Power;
                default: throw new ArgumentException("Undefined value.");
            }
        }
    }

    abstract class Node
    {
        static char[] plus_minus = { '-', '+' };
        static char[] mult_divide = { '*', '/' };
        static char power = '^';
        static string functions = "sin|cos|tg|ctg|ln";
        public static string Functions { get { return functions; } }

        public bool IsNumber { get { return false; } }
        public bool IsVariable { get { return false; } }

        protected Func<double, double> function;

        public abstract Node Differentiate();
        public abstract Func<double, double> Functionalize();
        public abstract TreeNode ToTree();
        public abstract Node Clone();

        /// <summary>
        /// Поиск первой операции-разделителя, не находящейся внутри скобок
        /// </summary>
        /// <param name="expr">строка с формулой</param>
        /// <param name="p_m">позиция последней операции сложения или вычитания, не находящейся в скобках (-1, если не найдено)</param>
        /// <param name="m_d">позиция последней операции умножения или деления, не находящейся в скобках (-1, если не найдено или ранее найдено сложение/вычитание)</param>
        /// <param name="pwr">позиция последней операции возведения в степень, не находящейся в скобках (-1, если не найдено или ранее найдена менее приоритетная операция)</param>
        static void divider_position(string expr, out int p_m, out int m_d, out int pwr)
        {
            pwr = p_m = m_d = -1;
            Stack<bool> brackets = new Stack<bool>();
            int i = expr.Length - 1;
            bool p_m_found = false,
                 m_d_found = false;
            while (i >= 0 && !p_m_found && !m_d_found)
            {
                char ch = expr[i];
                if (ch == ')')
                    brackets.Push(true);
                else if (ch == '(')
                {
                    if (brackets.Count == 0) throw new ArgumentException("Не совпадает число открывающих и закрывающих скобок в выражении!");
                    brackets.Pop();
                }
                //если очередная операция сложения/вычитания не находится в скобках, то запомнить и выйти из процедуры
                else if (plus_minus.Contains(ch) && brackets.Count == 0)
                {
                    p_m = i;
                    return;
                }
                //если очередная операция умножения/деления не находится в скобках и мы еще не находили таких операций раньше, запомнить
                else if (mult_divide.Contains(ch) && brackets.Count == 0 && m_d == -1)
                {
                    m_d = i;
                }
                else if (ch == power && brackets.Count == 0 && pwr == -1)
                {
                    pwr = i;
                }
                --i;
            }
        }

        static bool IsUnaryFunction(string expr, out string operation, out string argument)
        {
            argument = operation = string.Empty;
            string arg_template = "[(].*[)]$",
                   operation_template = $"^({functions})";
            Regex template = new Regex(operation_template + arg_template, RegexOptions.Singleline),
                  arg = new Regex(arg_template, RegexOptions.Singleline),
                  op = new Regex(operation_template, RegexOptions.Singleline);
            if (template.IsMatch(expr))
            {
                argument = arg.Match(expr).Value;
                operation = op.Match(expr).Value;
                return true;
            }
            return false;
        }

        public static Node CreateNewNode(string expression)
        {
            if (expression == string.Empty)
            {
                throw new ArgumentException("Пустая строка не является допустимым выражением!");
            }

            //удаляем, если есть, скобки вокруг поступившего выражения
            if (Regex.IsMatch(expression, @"^[(]\S*[)]$"))
            {
                expression = expression.Remove(expression.Length - 1, 1);
                expression = expression.Remove(0, 1);
            }

            //надо попытаться разделить выражение на два по самой низкоприоритетной операции из наличествующих
            int p_m_position, m_d_position, pow_position;
            divider_position(expression, out p_m_position, out m_d_position, out pow_position);

            string argument, operation;
            int dividerPosition = -1;

            if (p_m_position != -1)
                dividerPosition = p_m_position;
            else if (m_d_position != -1)
                dividerPosition = m_d_position;
            else if (pow_position != -1)
                dividerPosition = pow_position;
            else if (IsUnaryFunction(expression, out operation, out argument))
                return new FunctionNode(operation, argument);
            else if (Regex.IsMatch(expression, @"^\d+([.,]\d+)?$"))
                return new NumberNode(expression);
            else
                return new VariableNode();

            //иначе разделить
            string operand1 = expression.Substring(0, dividerPosition),
                    operand2 = expression.Substring(dividerPosition + 1);
            operation = expression.Substring(dividerPosition, 1);

            return new FunctionNode(operand1, operation, operand2);
        }
    }

    class FunctionNode : Node
    {
        Node operation;

        public FunctionNode(string operand1, string operation, string operand2)
        {
            this.operation = new BinaryFunctionNode(operand1, operation, operand2);
        }

        public FunctionNode(Node op1, BinaryOperations op, Node op2)
        {
            operation = new BinaryFunctionNode(op1, op, op2);
        }

        public FunctionNode(string func, string arg)
        {
            operation = new UnaryFunctionNode(func, arg);
        }

        public FunctionNode(string op, Node arg)
        {
            operation = new UnaryFunctionNode(op, arg);
        }

        public FunctionNode(Node operation)
        {
            this.operation = operation;
        }

        public override Node Clone()
        {
            return new FunctionNode(operation);
        }

        public override Node Differentiate()
        {
            return new FunctionNode(operation.Differentiate());
        }

        public override Func<double, double> Functionalize()
        {
            function = operation.Functionalize();
            return function;
        }

        public override TreeNode ToTree()
        {
            return operation.ToTree();
        }
    }

    class BinaryFunctionNode : Node
    {
        Node operand1, operand2;
        BinaryOperations operation;

        public BinaryFunctionNode(string op1, string op, string op2)
        {
            operand1 = CreateNewNode(op1);
            operand2 = CreateNewNode(op2);
            operation = BinaryOperationsExtention.FromString(op);
        }

        public BinaryFunctionNode(Node op1, BinaryOperations op, Node op2)
        {
            operand1 = op1;
            operand2 = op2;
            operation = op;
        }

        public override Node Clone()
        {
            return new BinaryFunctionNode(operand1.Clone(), operation, operand2.Clone());
        }

        /// <summary>
        /// Дифференцирует дерево с вершиной в текущем узле.
        /// </summary>
        /// <exception cref="InvalidOperationException">Если заданный оператор не соответствует ни одному из BinaryOperators.</exception>
        public override Node Differentiate()
        {
            switch (operation)
            {
                case BinaryOperations.Plus:
                case BinaryOperations.Minus:
                return new BinaryFunctionNode(operand1.Differentiate(), operation, operand2.Differentiate());
                case BinaryOperations.Multiply:
                {
                    //untested
                    #region Multiply
                    //(cf)' = c*f'
                    if (operand1.IsNumber)
                        return new BinaryFunctionNode(operand1.Clone(), operation, operand2.Differentiate());
                    //(fc)' = (f')*c
                    else if (operand2.IsNumber)
                        return new BinaryFunctionNode(operand1.Differentiate(), operation, operand2.Clone());
                    //(fg)' = f'g+fg'
                    else
                    {
                        Node f = operand1.Clone(),
                                 g = operand2.Clone(),
                                 f_dif = operand1.Differentiate(),
                                 g_dif = operand2.Differentiate();
                        return new BinaryFunctionNode(new BinaryFunctionNode(f_dif, BinaryOperations.Multiply, g),
                                              BinaryOperations.Plus,
                                              new BinaryFunctionNode(f, BinaryOperations.Multiply, g_dif)
                                             );
                    }
                    #endregion
                }
                case BinaryOperations.Divide:
                {
                    //untested
                    #region Divide
                    //(с)' = 0 !!!!!!!!
                    //(f/c)' = f'/c
                    if (operand2.IsNumber)
                        return new BinaryFunctionNode(operand1.Differentiate(), operation, operand2.Clone());
                    //(c/f)' = c*(1/f)' = c*(f^-1)' = -c*f^(-2)*f'
                    else if (operand1.IsNumber)
                    {
                        NumberNode c = -(operand1.Clone() as NumberNode);
                        Node f = operand2.Clone(),
                                 f_dif = operand2.Differentiate(),
                                 f_exp2 = new BinaryFunctionNode(f, BinaryOperations.Power, new NumberNode(-2));
                        return new BinaryFunctionNode(c, BinaryOperations.Multiply, new BinaryFunctionNode(f_exp2, BinaryOperations.Multiply, f_dif));
                    }
                    //(f/g)' = f'g - fg' / g^2
                    else
                    {
                        Node f = operand1.Clone(),
                                 g = operand2.Clone(),
                                 f_dif = operand1.Differentiate(),
                                 g_dif = operand1.Differentiate(),
                                 left = new BinaryFunctionNode(new BinaryFunctionNode(f_dif, BinaryOperations.Multiply, g), BinaryOperations.Minus, new BinaryFunctionNode(f, BinaryOperations.Multiply, g_dif)),
                                 right = new BinaryFunctionNode(g, BinaryOperations.Power, new NumberNode(2));
                        return new BinaryFunctionNode(left, operation, right);
                    }
                    #endregion
                }
                case BinaryOperations.Power:
                {
                    //untested
                    #region Power
                    //(x^c)' = c*x^(c-1)
                    if (operand2.IsNumber && operand1.IsVariable)
                    {
                        NumberNode degreeNode = operand2 as NumberNode;
                        double degree = degreeNode.Number;
                        --degreeNode;

                        return new BinaryFunctionNode(new NumberNode(degree), BinaryOperations.Multiply, Clone());
                    }
                    //(f^c)' = c*f'*f^c-1 ???
                    else if (operand2.IsNumber)
                    {
                        NumberNode degreeNode = operand2 as NumberNode;
                        double degree = degreeNode.Number;
                        --degreeNode;

                        Node self = Clone(),
                                 f = operand1.Clone(),
                                 f_dif = operand1.Differentiate();

                        return new BinaryFunctionNode(new NumberNode(degree), BinaryOperations.Multiply, new BinaryFunctionNode(f_dif, BinaryOperations.Multiply, Clone()));
                    }
                    //(a^f)' = ln(a)*a^f*f'
                    else if (operand1.IsNumber)
                    {
                        Node ln = new UnaryFunctionNode("ln", operand1.Clone());
                        //(a^x)' = ln(a)*a^x
                        if (operand2.IsVariable)
                            return new BinaryFunctionNode(ln, BinaryOperations.Multiply, Clone());
                        //(a^f)' = ln(a)*a^x*f'
                        else
                            return new BinaryFunctionNode(ln, BinaryOperations.Multiply, new BinaryFunctionNode(Clone(), BinaryOperations.Multiply, operand2.Differentiate()));
                    }
                    //f^g
                    else
                    {
                        Node f = operand1.Clone(),
                                 g = operand2.Clone(),
                                 f_dif = operand1.Differentiate(),
                                 g_dif = operand2.Differentiate(),
                                 left = new BinaryFunctionNode(g_dif, BinaryOperations.Multiply, new UnaryFunctionNode("ln", f)),
                                 right = new BinaryFunctionNode(g, BinaryOperations.Multiply, new BinaryFunctionNode(f_dif, BinaryOperations.Divide, f));
                        return new BinaryFunctionNode(Clone(), BinaryOperations.Multiply, new BinaryFunctionNode(left, BinaryOperations.Plus, right));
                    }
                    #endregion
                }
                default: throw new InvalidOperationException("Нераспознанный оператор.");
            }
        }

        public override Func<double, double> Functionalize()
        {
            switch (operation)
            {
                case BinaryOperations.Plus:
                return x => { return operand1.Functionalize()(x) + operand2.Functionalize()(x); };
                case BinaryOperations.Minus:
                return x => { return operand1.Functionalize()(x) - operand2.Functionalize()(x); };
                case BinaryOperations.Multiply:
                return x => { return operand1.Functionalize()(x) * operand2.Functionalize()(x); };
                case BinaryOperations.Divide:
                {
                    return x => { return operand1.Functionalize()(x) / operand2.Functionalize()(x); };
                }
                case BinaryOperations.Power:
                {
                    return x => { return Math.Pow(operand1.Functionalize()(x), operand2.Functionalize()(x)); };
                }
                default: throw new InvalidOperationException("Нераспознанный оператор.");
            }
        }

        public override TreeNode ToTree()
        {
            TreeNode node = new TreeNode(operation.ToStr());
            node.Nodes.Add(operand2.ToTree());
            node.Nodes.Add(operand1.ToTree());

            return node;
        }
    }

    class UnaryFunctionNode : Node
    {
        string func;
        Node argument;

        public UnaryFunctionNode(string op, string arg)
        {
            func = op;
            argument = CreateNewNode(arg);
        }

        public UnaryFunctionNode(string op, Node arg)
        {
            func = op;
            argument = arg;
        }

        public override Func<double, double> Functionalize()
        {
            switch (func)
            {
                case "sin":
                {
                    return x => { return Math.Sin(argument.Functionalize()(x)); };
                }
                case "cos":
                {
                    return x => { return Math.Cos(argument.Functionalize()(x)); };
                }
                case "tg":
                {
                    return x => { return Math.Tan(argument.Functionalize()(x)); };
                }
                case "ctg":
                {
                    return x => { return 1 / Math.Tan(argument.Functionalize()(x)); };
                }
                case "ln":
                {
                    return x => { return Math.Log(argument.Functionalize()(x)); };
                }
                default: { throw new InvalidOperationException("Операция не распознана."); }
            }
        }

        public override TreeNode ToTree()
        {
            TreeNode node = new TreeNode(func);
            node.Nodes.Add(argument.ToTree());

            return node;
        }

        public override Node Clone()
        {
            return new UnaryFunctionNode(func, argument.Clone());
        }

        public override Node Differentiate()
        {
            switch (func)
            {
                case "sin":
                {
                    if (argument is FunctionNode)
                    {
                        Node left = new FunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.Differentiate()),
                                 right = new UnaryFunctionNode("cos", argument.Clone());
                        return new FunctionNode(left, BinaryOperations.Multiply, right);
                    }
                    else if (argument is VariableNode)
                        return new FunctionNode("cos", argument.Clone());
                    else
                        return new NumberNode(0);
                }
                case "cos":
                {
                    Node left, right = new UnaryFunctionNode("sin", argument.Clone());
                    if (argument is FunctionNode)
                        left = new FunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.Differentiate());
                    else if (argument is VariableNode)
                        left = new NumberNode(-1);
                    else
                        return new NumberNode(0);

                    return new FunctionNode(left, BinaryOperations.Multiply, right);
                }
                case "tg":
                {
                    Node numerator, denominator = new FunctionNode(new UnaryFunctionNode("cos", argument.Clone()), BinaryOperations.Power, new NumberNode(2));
                    if (argument is FunctionNode)
                        numerator = argument.Differentiate();
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        return new NumberNode(0);

                    return new FunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                case "ctg":
                {
                    Node numerator, denominator = new FunctionNode(new UnaryFunctionNode("sin", argument.Clone()), BinaryOperations.Power, new NumberNode(2));
                    if (argument is FunctionNode)
                        numerator = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.Differentiate());
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        return new NumberNode(0);

                    return new FunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                case "ln":
                {
                    Node numerator, denominator = argument.Clone();
                    if (argument is FunctionNode)
                        numerator = argument.Differentiate();
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        return new NumberNode(0);

                    return new FunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                default: { throw new InvalidOperationException("Операция не распознана."); }
            }
        }
    }

    class VariableNode : Node
    {
        public new bool IsVariable { get { return true; } }

        public override Node Clone()
        {
            return new VariableNode();
        }

        public override Node Differentiate()
        {
            return new NumberNode(1);
        }

        public override Func<double, double> Functionalize()
        {
            return x => { return x; };
        }

        public override TreeNode ToTree()
        {
            return new TreeNode("x");
        }
    }

    class NumberNode : Node
    {
        double number;

        public static NumberNode operator --(NumberNode self)
        {
            --self.number;
            return self;
        }

        public static NumberNode operator -(NumberNode self)
        {
            self.number *= -1;
            return self;
        }

        public new bool IsNumber { get { return true; } }

        public double Number { get { return number; } }

        /// <summary>
        /// Создаёт экземпляр из строки.
        /// </summary>
        /// <exception cref="ArgumentException">Если строка не преобразуется в число.</exception>
        public NumberNode(string expr)
        {
            if (!double.TryParse(expr, out number))
                throw new ArgumentException("Невозможно преобразовать строку в число.");
        }

        public NumberNode(double num)
        {
            number = num;
        }

        public override Func<double, double> Functionalize()
        {
            return x => { return number; };
        }

        public override TreeNode ToTree()
        {
            return new TreeNode(number.ToString());
        }

        public override Node Clone()
        {
            return new NumberNode(number);
        }

        public override Node Differentiate()
        {
            return new NumberNode(0);
        }
    }

}
