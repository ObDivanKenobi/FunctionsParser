using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace FunctionsParserNodes
{
    /// <summary>
    /// Перечисление бинарных операторов.
    /// </summary>
    enum BinaryOperations
    {
        Plus, Minus, Multiply, Divide, Power
    }

    /// <summary>
    /// Набор методов для работы с <see cref="BinaryOperations"/>
    /// </summary>
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

    /// <summary>
    /// Базовый класс узла дерева выражений.
    /// </summary>
    abstract class Node
    {
        #region Cтатические переменные
        static char[] plus_minus = { '-', '+' };
        static char[] mult_divide = { '*', '/' };
        static char power = '^';
        static string functions = "sin|cos|tg|ctg|ln";
        static Regex unaryFunctionTemplate = new Regex($"^({functions})[(].*[)]$", RegexOptions.Singleline),
                     unaryFunctionArgumentTemplate = new Regex("[(].*[)]$", RegexOptions.Singleline),
                     unaryFunctionOperationTemplate = new Regex($"^({functions})", RegexOptions.Singleline),
                     doubleTemplate = new Regex(@"^[+|-]?(\d+([.,]\d+)?|e|pi)$");

        internal static string operations = "-|+|*|/";
        public static string Functions { get { return functions; } }
        #endregion

        #region Свойства
        /// <summary>
        /// Является ли узел узлом-числом.
        /// </summary>
        public virtual bool IsNumber { get { return false; } }
        /// <summary>
        /// является ли узел узлом-переменной
        /// </summary>
        public virtual bool IsVariable { get { return false; } }
        #endregion

        protected Func<double> function;

        #region Абстрактные методы
        /// <summary>
        /// Поиск производной функции одного аргумента.
        /// </summary>
        /// <returns>Производная.</returns>
        /// <exception cref="InvalidOperationException">В случае попытки применения к функции многих переменных.</exception>
        public abstract Node Differentiate();
        /// <summary>
        /// Поиск частной производной по переменной <paramref name="variable"/>.
        /// </summary>
        /// <param name="variable">Переменная, по которой осуществляется дифференцирование.</param>
        /// <returns>Частная производная по </paramref name="variable"/></returns>
        public abstract Node DifferentiateBy(string variable);
        /// <summary>
        /// Создание дерева функциональных объектов из поддерева с вершиной в текущем узле.
        /// </summary>
        /// <returns></returns>
        public abstract Func<double> Functionalize();
        /// <summary>
        /// Представление узла в виде, пригодном для отображения в TreeView.
        /// </summary>
        /// <returns></returns>
        public abstract TreeNode ToTree();
        /// <summary>
        /// Создание копии текущего узла (поверхностное).
        /// </summary>
        /// <returns>Поверхностная копия.</returns>
        public abstract Node Clone();
        /// <summary>
        /// Соотнесение узлов-переменных со словарём переменных.
        /// </summary>
        /// <param name="vars">словарь переменных</param>
        public abstract void SetVariables(SortedDictionary<string, double> vars);
        /// <summary>
        /// Проверка оптимизированного дерева на предмет изчезнувших в процессе оптимизации переменных
        /// </summary>
        /// <param name="variables">список переменных неоптимизированного дерева</param>
        public abstract void CheckVariables(List<string> variables);
        /// <summary>
        /// Установка тригонометрических функций на использование градусов или радианов в качестве аргумента.
        /// </summary>
        /// <param name="useDegrees">Использовать ли градусы в качестве аргумента.</param>
        public abstract void SetTrigonometry(bool useDegrees);
        /// <summary>
        /// Проверка, является ли поддерево константой относительно переменной <paramref name="variable"/>.
        /// </summary>
        /// <param name="variable">переменная</param>
        /// <returns></returns>
        internal abstract bool IsConstRelatively(string variable);
        /// <summary>
        /// Создание строкового представления узла.
        /// </summary>
        public abstract override string ToString();
        #endregion

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #region статические методы
        public static bool operator == (Node node, double number)
        {
            return (node as NumberNode)?.Number == number;
        }

        public static bool operator != (Node node, double number)
        {
            return (node as NumberNode)?.Number != number;
        }

        public static BinaryFunctionNode operator - (Node a, Node b)
        {
            return new BinaryFunctionNode(a.Clone(), BinaryOperations.Minus, b.Clone());
        }

        public static BinaryFunctionNode operator - (Node a, int b)
        {
            return new BinaryFunctionNode(a.Clone(), BinaryOperations.Minus, new NumberNode(b));
        }

        public static BinaryFunctionNode operator * (Node a, Node b)
        {
            return new BinaryFunctionNode(a.Clone(), BinaryOperations.Multiply, b.Clone());
        }

        /// <summary>
        /// Возвращает новый объект, не изменяет исходный.
        /// Если self — не NumberNode, то объект-результат будет содержать ссылку на операнд.
        /// </summary>
        /// <param name="self">исходный объект</param>
        /// <returns>изменённая копия исходного объекта</returns>
        public static Node operator - (Node self)
        {
            if (self is NumberNode)
            {
                NumberNode nn = self.Clone() as NumberNode;
                return -nn;
            }

            return new BinaryFunctionNode(self, BinaryOperations.Multiply, new NumberNode(-1));
        }

        /// <summary>
        /// Возвращает новый объект, не изменяет исходный.
        /// Если self — не NumberNode, то объект-результат будет содержать ссылку на операнд.
        /// </summary>
        /// <param name="self">исходный объект</param>
        /// <returns>изменённая копия исходного объекта</returns>
        public static Node operator -- (Node self)
        {
            if (self is NumberNode)
            {
                NumberNode nn = self.Clone() as NumberNode;
                return --nn;
            }

            return new BinaryFunctionNode(self, BinaryOperations.Minus, new NumberNode(1));
        }

        /// <summary>
        /// Проверка, является ли выражение унарной функцией
        /// </summary>
        /// <param name="expr">выражение</param>
        /// <param name="operation">функция</param>
        /// <param name="argument">аргумент</param>
        static bool IsUnaryFunction(string expr, out string operation, out string argument)
        {
            argument = operation = string.Empty;
            if (unaryFunctionTemplate.IsMatch(expr))
            {
                argument = unaryFunctionArgumentTemplate.Match(expr).Value;
                operation = unaryFunctionOperationTemplate.Match(expr).Value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Поиск первой операции-разделителя, не находящейся внутри скобок.
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
                    if (brackets.Count == 0) throw new BracketsMismatchException("Не совпадает число открывающих и закрывающих скобок в выражении!");
                    brackets.Pop();
                }
                //если очередная операция сложения/вычитания не находится в скобках, то запомнить и выйти из процедуры
                else if (plus_minus.Contains(ch) && brackets.Count == 0)
                {
                    //для отслеживания унарного плюса и унарного минуса
                    if (i != 0)
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

        /// <summary>
        /// Создание нового поддерева
        /// </summary>
        /// <param name="expression">строка, содержащая выражение</param>
        /// <returns>Корень созданного поддерева</returns>
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

            //если найдена бинарная операция
            if (p_m_position != -1)
                dividerPosition = p_m_position;
            else if (m_d_position != -1)
                dividerPosition = m_d_position;
            else if (pow_position != -1)
                dividerPosition = pow_position;
            //если переданное выражение является числом
            else if (doubleTemplate.IsMatch(expression))
                return new NumberNode(expression);
            //если переданное выражение является унарной функцией
            else if (IsUnaryFunction(expression, out operation, out argument))
                return new UnaryFunctionNode(operation, argument);
            //если переданное выражение является переменной
            else
            {
                //опускаем унарный плюс
                if (expression[0] == '+')
                    return new VariableNode(expression.Remove(0, 1));

                //преобразуем унарный минус в (-1)*%переменная%
                if (expression[0] == '-')
                    return new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, new VariableNode(expression.Substring(1)));

                return new VariableNode(expression);
            }

            //разделить по бинарной операции
            string operand1 = expression.Substring(0, dividerPosition),
                    operand2 = expression.Substring(dividerPosition + 1);
            operation = expression.Substring(dividerPosition, 1);

            return new BinaryFunctionNode(operand1, operation, operand2);
        }

        /// <summary>
        /// Оптимизация поддерева.
        /// </summary>
        /// <param name="node">корень поддерева</param>
        /// <returns>Оптимизированное поддерево</returns>
        /// <exception cref="DivideByZeroException">если в выражении присутствует деление на ноль</exception>
        public static Node Optimize(Node n)
        {
            //Clone(), чтобы обеспечить атомарность — если что-то в процессе пойдёт не так, то дерево откатится, а не будет частично изменённым.
            Node node = n.Clone();
            if (node is UnaryFunctionNode)
            {
                UnaryFunctionNode tmp = node as UnaryFunctionNode;
                tmp.argument = Optimize(tmp.argument);
                if (tmp.argument.IsNumber)
                    node = new NumberNode(tmp.argument.Functionalize()());
                else
                    node = tmp;
            }
            else if (node is BinaryFunctionNode)
            {
                BinaryFunctionNode tmp = node.Clone() as BinaryFunctionNode;
                tmp.operand1 = Optimize(tmp.operand1);
                tmp.operand2 = Optimize(tmp.operand2);
                switch(tmp.operation)
                {
                    case BinaryOperations.Plus:
                    {
                        if (tmp.operand1.IsNumber && tmp.operand2.IsNumber)
                            node = new NumberNode(tmp.Functionalize()());
                        else if (tmp.operand1 == 0)
                            node = tmp.operand2;
                        else if (tmp.operand2 == 0)
                            node = tmp.operand1;
                        else
                            node = tmp;
                        break;
                    }
                    case BinaryOperations.Minus:
                    {
                        if (tmp.operand1.IsNumber && tmp.operand2.IsNumber)
                            node = new NumberNode(tmp.Functionalize()());
                        else if (tmp.operand1 == 0)
                            node = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, tmp.operand2);
                        else if (tmp.operand2 == 0)
                            node = tmp.operand1;
                        else
                            node = tmp;
                        break;
                    }
                    case BinaryOperations.Multiply:
                    {
                        if (tmp.operand1 == 0 || tmp.operand2 == 0)
                            node = new NumberNode(0);
                        else if (tmp.operand1 == 1)
                            node = tmp.operand2;
                        else if (tmp.operand2 == 1)
                            node = tmp.operand1;
                        else if (tmp.operand1.IsNumber && tmp.operand2.IsNumber)
                            node = new NumberNode(tmp.Functionalize()());
                        else
                            node = tmp;
                        break;
                    }
                    case BinaryOperations.Divide:
                    {
                        if (tmp.operand2 == 0)
                            throw new DivideByZeroException($"Обнаружено деление на ноль: \"{node.ToString()}\"");
                        else if (tmp.operand2 == 0)
                            node = new NumberNode(0);
                        else if (tmp.operand1.IsNumber && tmp.operand1.IsNumber)
                            node = new NumberNode(tmp.Functionalize()());
                        else
                            node = tmp;
                        break;
                    }
                    case BinaryOperations.Power:
                    {
                        if (tmp.operand1 == 0)
                            node = new NumberNode(0);
                        else if (tmp.operand1 == 1 || tmp.operand2 == 0)
                            node = new NumberNode(1);
                        else if (tmp.operand2 == 1)
                            node = tmp.operand1;
                        else if (tmp.operand1.IsNumber && tmp.operand2.IsNumber)
                            node = new NumberNode(tmp.Functionalize()());
                        else
                            node = tmp;
                        break;
                    }
                }

            }
            return node;
        }
        #endregion
    }

    /// <summary>
    /// Класс узла дерева выражений, представляющий бинарные функции. Наследник <see cref="Node"/>
    /// </summary>
    class BinaryFunctionNode : Node
    {
        internal Node operand1, operand2;
        internal BinaryOperations operation;

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

        /// <summary>
        /// Создание копии текущего узла (поверхностное).
        /// </summary>
        /// <returns>Поверхностная копия.</returns>
        public override Node Clone()
        {
            return new BinaryFunctionNode(operand1.Clone(), operation, operand2.Clone());
        }

        /// <summary>
        /// Поиск производной функции одного аргумента.
        /// </summary>
        /// <returns>Производная.</returns>
        /// <exception cref="InvalidOperationException">В случае попытки применения к функции многих переменных.</exception>
        public override Node Differentiate()
        {
            switch (operation)
            {
                case BinaryOperations.Plus:
                case BinaryOperations.Minus:
                return new BinaryFunctionNode(operand1.Differentiate(), operation, operand2.Differentiate());
                case BinaryOperations.Multiply:
                {
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
                    #region Divide
                    //(с)' = 0 !!!!!!!!
                    //(f/c)' = f'/c
                    if (operand2.IsNumber)
                        return new BinaryFunctionNode(operand1.Differentiate(), BinaryOperations.Divide, operand2.Clone());
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
                                 numerator = new BinaryFunctionNode(new BinaryFunctionNode(f_dif, BinaryOperations.Multiply, g), BinaryOperations.Minus, new BinaryFunctionNode(f, BinaryOperations.Multiply, g_dif)),
                                 denominator = new BinaryFunctionNode(g, BinaryOperations.Power, new NumberNode(2));
                        return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                    }
                    #endregion
                }
                case BinaryOperations.Power:
                {
                    #region Power
                    //(x^c)' = c*x^(c-1)
                    if (operand2.IsNumber && operand1.IsVariable)
                    {
                        BinaryFunctionNode tmp = Clone() as BinaryFunctionNode;
                        NumberNode degree = tmp.operand2 as NumberNode;
                        --degree;

                        return new BinaryFunctionNode(new NumberNode(degree.Number + 1), BinaryOperations.Multiply, tmp);
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
                    //f^g = f^g * (g'*ln(f) + g*f'/f) 
                    else
                    {
                        Node f = operand1.Clone(),
                                 g = operand2.Clone(),
                                 f_dif = operand1.Differentiate(),
                                 g_dif = operand2.Differentiate(),
                                 add1 = new BinaryFunctionNode(g_dif, BinaryOperations.Multiply, new UnaryFunctionNode("ln", f)),
                                 add2 = new BinaryFunctionNode(g, BinaryOperations.Multiply, new BinaryFunctionNode(f_dif, BinaryOperations.Divide, f));
                        return new BinaryFunctionNode(Clone(), BinaryOperations.Multiply, new BinaryFunctionNode(add1, BinaryOperations.Plus, add2));
                    }
                    #endregion
                }
                default: throw new InvalidOperationException("Нераспознанный оператор. Если вы видите эту ошибку, то автор накосячил в методе создания дерева.");
            } 
        }

        /// <summary>
        /// Поиск частной производной по переменной <paramref name="variable"/>.
        /// </summary>
        /// <param name="variable">Переменная, по которой осуществляется дифференцирование.</param>
        /// <returns>Частная производная.</returns>
        public override Node DifferentiateBy(string variable)
        {
            switch (operation)
            {
                case BinaryOperations.Plus:
                case BinaryOperations.Minus:
                    return new BinaryFunctionNode(operand1.DifferentiateBy(variable), operation, operand2.DifferentiateBy(variable));
                case BinaryOperations.Multiply:
                {
                    #region Multiply
                    //(cf)' = c*f'
                    if (operand1.IsConstRelatively(variable))
                        return new BinaryFunctionNode(operand1.Clone(), operation, operand2.DifferentiateBy(variable));
                    //(fc)' = (f')*c
                    else if (operand2.IsConstRelatively(variable))
                        return new BinaryFunctionNode(operand1.DifferentiateBy(variable), operation, operand2.Clone());
                    //(fg)' = f'g+fg'
                    else
                    {
                        Node f = operand1.Clone(),
                             g = operand2.Clone(),
                             f_dif = operand1.DifferentiateBy(variable),
                             g_dif = operand2.DifferentiateBy(variable);
                        return new BinaryFunctionNode(new BinaryFunctionNode(f_dif, BinaryOperations.Multiply, g), 
                                                      BinaryOperations.Plus, 
                                                      new BinaryFunctionNode(f, BinaryOperations.Multiply, g_dif)
                                                     );
                    }
                    #endregion
                }
                case BinaryOperations.Divide:
                {
                    #region Divide
                    //(с)' = 0
                    if (operand1.IsConstRelatively(variable) && operand2.IsConstRelatively(variable))
                        return new NumberNode(0);
                    //(f/c)' = f'/c
                    if (operand2.IsConstRelatively(variable))
                        return new BinaryFunctionNode(operand1.DifferentiateBy(variable), operation, operand2.Clone());
                    //(c/f)' = c*(1/f)' = c*(f^-1)' = -c*f^(-2)*f'
                    else if (operand1.IsConstRelatively(variable))
                    {
                        Node f = operand2.Clone(),
                             c = -operand1,
                             f_dif = operand2.DifferentiateBy(variable),
                             f_pow2 = new BinaryFunctionNode(f, BinaryOperations.Power, new NumberNode(-2));
                        return new BinaryFunctionNode(c, BinaryOperations.Multiply, new BinaryFunctionNode(f_pow2, BinaryOperations.Multiply, f_dif));
                    }
                    //(f/g)' = (f'g - fg') / g^2
                    else
                    {
                        Node f = operand1.Clone(),
                             g = operand2.Clone(),
                             f_dif = operand1.DifferentiateBy(variable),
                             g_dif = operand1.DifferentiateBy(variable),
                             numerator = new BinaryFunctionNode(new BinaryFunctionNode(f_dif, BinaryOperations.Multiply, g), BinaryOperations.Minus, new BinaryFunctionNode(f, BinaryOperations.Multiply, g_dif)), //f'g - fg'
                             denominator = new BinaryFunctionNode(g, BinaryOperations.Power, new NumberNode(2)); //g^2
                        return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                    }
                    #endregion
                }
                case BinaryOperations.Power:
                {
                    #region Power
                    //const^const = const
                    if (operand1.IsConstRelatively(variable) && operand2.IsConstRelatively(variable))
                        return new NumberNode(0);
                    //(f^const)' = const*f^(const-1)*f'
                    if (operand2.IsConstRelatively(variable))
                    {
                        Node coef = operand2.Clone(),
                             f = operand1.Clone(),
                             f_dif = operand1.DifferentiateBy(variable),
                             newPwr = new BinaryFunctionNode(f, BinaryOperations.Power, --coef);

                        //f'*(c*f^(c-1))
                        return new BinaryFunctionNode(f_dif, BinaryOperations.Multiply, new BinaryFunctionNode(operand2.Clone(), BinaryOperations.Multiply, newPwr));
                    }
                    //(const^f)' = ln(const)*const^f*f'
                    if (operand1.IsConstRelatively(variable))
                    {
                        Node ln = new UnaryFunctionNode("ln", operand1.Clone());
                        return new BinaryFunctionNode(ln, BinaryOperations.Multiply, new BinaryFunctionNode(Clone(), BinaryOperations.Multiply, operand2.DifferentiateBy(variable)));
                    }
                    //f^g = f^g * (g'*ln(f) + g*f'/f) 
                    else
                    {
                        Node f = operand1.Clone(),
                             g = operand2.Clone(),
                             f_dif = operand1.DifferentiateBy(variable),
                             g_dif = operand2.DifferentiateBy(variable),
                             add1 = new BinaryFunctionNode(g_dif, BinaryOperations.Multiply, new UnaryFunctionNode("ln", f)), //g'*ln(f)
                             add2 = new BinaryFunctionNode(g, BinaryOperations.Multiply, new BinaryFunctionNode(f_dif, BinaryOperations.Divide, f)); // g*f'/f
                        return new BinaryFunctionNode(Clone(), BinaryOperations.Multiply, new BinaryFunctionNode(add1, BinaryOperations.Plus, add2));
                    }
                    #endregion
                }
                default: throw new InvalidOperationException("Нераспознанный оператор. Этого просто не может быть, если автор не накосячил с методом создания дерева.");
            }
        }

        public override Func<double> Functionalize()
        {
            switch (operation)
            {
                case BinaryOperations.Plus:
                    return () => { return operand1.Functionalize()() + operand2.Functionalize()(); };
                case BinaryOperations.Minus:
                    return () => { return operand1.Functionalize()() - operand2.Functionalize()(); };
                case BinaryOperations.Multiply:
                    return () => { return operand1.Functionalize()() * operand2.Functionalize()(); };
                case BinaryOperations.Divide:
                    return () => { return operand1.Functionalize()() / operand2.Functionalize()(); };
                case BinaryOperations.Power:
                    return () => { return Math.Pow(operand1.Functionalize()(), operand2.Functionalize()()); };
                default: throw new InvalidOperationException("Нераспознанный оператор.");
            }
        }

        public override void SetVariables(SortedDictionary<string, double> vars)
        {
            operand1.SetVariables(vars);
            operand2.SetVariables(vars);
        }

        public override string ToString()
        {
            switch (operation)
            {
                case BinaryOperations.Plus:
                case BinaryOperations.Minus:
                    return $"({operand1.ToString()}{BinaryOperationsExtention.ToStr(operation)}{operand2.ToString()})";
                case BinaryOperations.Multiply:
                case BinaryOperations.Divide:
                    return $"{operand1.ToString()}{BinaryOperationsExtention.ToStr(operation)}{operand2.ToString()}";
                default:
                    return $"({operand1.ToString()})^({operand2.ToString()})";
            }
        }

        public override TreeNode ToTree()
        {
            TreeNode node = new TreeNode(operation.ToStr());
            node.Nodes.Add(operand2.ToTree());
            node.Nodes.Add(operand1.ToTree());

            return node;
        }

        public override void SetTrigonometry(bool useDegrees)
        {
            operand1.SetTrigonometry(useDegrees);
            operand2.SetTrigonometry(useDegrees);
        }

        public override void CheckVariables(List<string> variables)
        {
            operand1.CheckVariables(variables);
            operand2.CheckVariables(variables);
        }

        internal override bool IsConstRelatively(string variable)
        {
            return operand1.IsConstRelatively(variable) && operand2.IsConstRelatively(variable);
        }
    }

    /// <summary>
    /// Класс узла дерева выражений, представляющий унарные функции. Наследник <see cref="Node"/>
    /// </summary>
    class UnaryFunctionNode : Node
    {
        string func;
        internal Node argument;
        bool useDegrees = false;

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

        public override Func<double> Functionalize()
        {
            switch (func)
            {
                case "sin":
                {
                    if (useDegrees)
                        return () => { return Math.Sin(argument.Functionalize()() * Math.PI / 180.0); };

                    return () => { return Math.Sin(argument.Functionalize()()); };
                }
                case "cos":
                {
                    if (useDegrees)
                        return () => { return Math.Cos(argument.Functionalize()() * Math.PI / 180.0); };

                    return () => { return Math.Cos(argument.Functionalize()()); };
                }
                case "tg":
                {
                    if (useDegrees)
                        return () => { return Math.Tan(argument.Functionalize()() * Math.PI / 180.0); };

                    return () => { return Math.Tan(argument.Functionalize()()); };
                }
                case "ctg":
                {
                    if (useDegrees)
                        return () => { return 1 / Math.Tan(argument.Functionalize()() * Math.PI / 180.0); };

                    return () => { return 1 / Math.Tan(argument.Functionalize()()); };
                }
                case "ln":
                {
                    return () => { return Math.Log(argument.Functionalize()()); };
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
                    if (argument is NumberNode)
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        return new UnaryFunctionNode("cos", argument.Clone());
                    else
                    {
                        Node left = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.Differentiate()),
                                 right = new UnaryFunctionNode("cos", argument.Clone());
                        return new BinaryFunctionNode(left, BinaryOperations.Multiply, right);
                    }
                }
                case "cos":
                {
                    Node left, right = new UnaryFunctionNode("sin", argument.Clone());
                    if (argument is NumberNode)
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        left = new NumberNode(-1);
                    else
                        left = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.Differentiate());

                    return new BinaryFunctionNode(left, BinaryOperations.Multiply, right);
                }
                case "tg":
                {
                    Node numerator, denominator = new BinaryFunctionNode(new UnaryFunctionNode("cos", argument.Clone()), BinaryOperations.Power, new NumberNode(2));
                    if (argument is NumberNode)
                        return new NumberNode(0); 
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        numerator = argument.Differentiate();

                    return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                case "ctg":
                {
                    Node numerator, denominator = new BinaryFunctionNode(new UnaryFunctionNode("sin", argument.Clone()), BinaryOperations.Power, new NumberNode(2));
                    if (argument is NumberNode)
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        numerator = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.Differentiate());

                    return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                case "ln":
                {
                    Node numerator, denominator = argument.Clone();
                    if (argument is NumberNode)
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        numerator = argument.Differentiate();

                    return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                default: { throw new InvalidOperationException("Операция не распознана."); }
            }
        }

        public override Node DifferentiateBy(string variable)
        {
            switch (func)
            {
                case "sin":
                {
                    if (argument.IsConstRelatively(variable))
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        return new UnaryFunctionNode("cos", argument.Clone());
                    else
                    {
                        Node left = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, new UnaryFunctionNode("cos", argument.Clone())),
                             right = argument.DifferentiateBy(variable);
                        return new BinaryFunctionNode(left, BinaryOperations.Multiply, right);
                    }
                }
                case "cos":
                {
                    Node left, right = new UnaryFunctionNode("sin", argument.Clone());
                    if (argument.IsConstRelatively(variable))
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        left = new NumberNode(-1);
                    else
                        left = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.DifferentiateBy(variable));

                    return new BinaryFunctionNode(left, BinaryOperations.Multiply, right);
                }
                case "tg":
                {
                    Node numerator, denominator = new BinaryFunctionNode(new UnaryFunctionNode("cos", argument.Clone()), BinaryOperations.Power, new NumberNode(2));
                    if (argument.IsConstRelatively(variable))
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        numerator = argument.DifferentiateBy(variable);

                    return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                case "ctg":
                {
                    Node numerator, denominator = new BinaryFunctionNode(new UnaryFunctionNode("sin", argument.Clone()), BinaryOperations.Power, new NumberNode(2));
                    if (argument.IsConstRelatively(variable))
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        numerator = new BinaryFunctionNode(new NumberNode(-1), BinaryOperations.Multiply, argument.DifferentiateBy(variable));

                    return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                case "ln":
                {
                    Node numerator, denominator = argument.Clone();
                    if (argument.IsConstRelatively(variable))
                        return new NumberNode(0);
                    else if (argument is VariableNode)
                        numerator = new NumberNode(1);
                    else
                        numerator = argument.DifferentiateBy(variable);

                    return new BinaryFunctionNode(numerator, BinaryOperations.Divide, denominator);
                }
                default: { throw new InvalidOperationException("Операция не распознана."); }
            }
        }

        public override string ToString()
        {
            return $"{func}({argument.ToString()})";
        }

        public override void SetVariables(SortedDictionary<string, double> vars)
        {
            argument.SetVariables(vars);
        }

        public override void SetTrigonometry(bool useDegrees)
        {
            this.useDegrees = useDegrees;
            argument.SetTrigonometry(useDegrees);
        }

        public override void CheckVariables(List<string> variables)
        {
            argument.CheckVariables(variables);
        }

        internal override bool IsConstRelatively(string variable)
        {
            return argument.IsConstRelatively(variable);
        }
    }

    /// <summary>
    /// Класс узла дерева выражений, представляющий узел с переменной. Согласно логике является "листом" дерева. Наследник <see cref="Node"/>
    /// </summary>
    class VariableNode : Node
    {
        public override bool IsVariable { get { return true; } }
        public string variable { get; private set; }
        internal SortedDictionary<string, double> variables;

        /// <summary>
        /// Получение узла-переменной из строки
        /// </summary>
        /// <param name="expr">строка</param>
        public VariableNode(string expr)
        {
            variable = expr;
        }

        public override Node Clone()
        {
            return new VariableNode(variable);
        }

        public override Node Differentiate()
        {
            return new NumberNode(1);
        }

        public override Node DifferentiateBy(string variable)
        {
            return this.variable == variable ? new NumberNode(1) : new NumberNode(0);
        }

        public override Func<double> Functionalize()
        {
            return () => { return variables[variable]; };
        }

        public override TreeNode ToTree()
        {
            return new TreeNode($"{variable}");
        }

        public override string ToString()
        {
            return variable;
        }

        public override void SetVariables(SortedDictionary<string, double> vars)
        {
            if (!vars.ContainsKey(variable))
                throw new ParametersMismatchException("Некорректная переменная!");

            variables = vars;
        }

        public override void SetTrigonometry(bool useDegrees)
        {
            return;
        }

        /// <summary>
        /// Проверка оптимизированного дерева на предмет изчезнувших в процессе оптимизации переменных
        /// </summary>
        /// <param name="variables">список переменных неоптимизированного дерева</param>
        public override void CheckVariables(List<string> variables)
        {
            variables.Remove(variable);
        }

        internal override bool IsConstRelatively(string variable)
        {
            return this.variable != variable;
        }
    }

    /// <summary>
    /// Класс узла дерева выражений, представляющий узел с числом. Согласно логике является "листом" дерева. Наследник <see cref="Node"/>
    /// </summary>
    class NumberNode : Node
    {
        double number;

        /// <summary>
        /// Декремент. Изменяет исходный объект
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static NumberNode operator --(NumberNode self)
        {
            --self.number;
            return self;
        }

        /// <summary>
        /// Унарный минус. Изменяет исходный обьект
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static NumberNode operator -(NumberNode self)
        {
            self.number *= -1;
            return self;
        }

        /// <summary>
        /// Оператор сложения для <see cref="NumberNode"/>.
        /// </summary>
        public static NumberNode operator +(NumberNode op1, int op2)
        {
            return new NumberNode(op1.Number + op2);
        }

        public override bool IsNumber { get { return true; } }

        public double Number { get { return number; } }

        public NumberNode(string expr)
        {
            if (!double.TryParse(expr, out number))
            {
                switch (expr)
                {
                    case "e": { number = Math.E; break; }
                    case "-e": { number = -Math.E; break; }
                    case "pi": { number = Math.PI; break; }
                    case "-pi": { number = -Math.PI; break; }
                    default: throw new ArgumentException("Невозможно преобразовать строку в число.");
                }
            }
        }

        public NumberNode(double num)
        {
            number = num;
        }

        public override Func<double> Functionalize()
        {
            return () => { return number; };
        }

        public override TreeNode ToTree()
        {
            return new TreeNode(ToString());
        }

        public override Node Clone()
        {
            return new NumberNode(number);
        }

        public override Node Differentiate()
        {
            return new NumberNode(0);
        }

        public override Node DifferentiateBy(string variable)
        {
            return new NumberNode(0);
        }

        public override string ToString()
        {
            if (number == Math.E)
                return "e";
            else if (number == -Math.E)
                return "(-e)";
            if (number == Math.PI)
                return "pi";
            else if (number == -Math.PI)
                return "(-pi)";
            else if (number < 0)
                return $"({number.ToString()})";

            return number.ToString();
        }

        public override void SetVariables(SortedDictionary<string, double> vars)
        {
            return;
        }

        public override void SetTrigonometry(bool useDegrees)
        {
            return;
        }

        public override void CheckVariables(List<string> variables)
        {
            return;
        }

        internal override bool IsConstRelatively(string variable)
        {
            return true;
        }
    }
}