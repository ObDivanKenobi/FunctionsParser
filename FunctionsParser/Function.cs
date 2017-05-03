using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using FunctionsParserNodes;
using System.Linq;

namespace FunctionsParser
{
    public class FunctionParser
    {
        static Regex removeSpaces = new Regex(@"\s+"),
                     addMultiply = new Regex($@"(\d+)(\p{"{Ll}"}|\p{"{Lu}"}|[(]|{Node.Functions})"),
                     defineVariables = new Regex($@"((?<=(^|[{Node.operations}|(]))x(\d*)(?=([{Node.operations}|)]|\b)))", RegexOptions.Singleline); //|((?<=([(]))x(\d*)(?=([)]|\b)))

        string expression;
        Node root;
        Func<double> function;
        SortedDictionary<string, double> variables;
        List<FunctionParser> cachedDerivatives = new List<FunctionParser>();
        public double CachedValue { get; private set; }

        public List<string> Variables
        {
            get
            {
                return variables.Keys.ToList();
            }
        }

        /// <summary>
        /// Создание эклемпляра FunctionParser из строки с выражением.
        /// </summary>
        /// <param name="expr">Строка с выражением</param>
        /// <exception cref="ArgumentException">Если строка некорректна.</exception>
        public FunctionParser(string expr)
        {
            //удаление пробелов
            expression = expr.Trim();

            string replacement = "";
            expression = removeSpaces.Replace(expression, replacement);

            //подстановка "подразумевающегося" умножения
            replacement = "$1*$2";
            expression = addMultiply.Replace(expression, replacement);

            try { root = Node.CreateNewNode(expression); }
            catch (BracketsMismatchException ex)
            {
                throw new ArgumentException(ex.Message, ex);
            }

            //"опознание" переменных
            variables = new SortedDictionary<string, double>();
            root.DefineVariables(variables);
            CachedValue = double.NaN;
        }

        FunctionParser(Node new_root, SortedDictionary<string, double> vars, bool checkVariables = false)
        {
            root = new_root;
            variables = new SortedDictionary<string, double>(vars);
            CachedValue = double.NaN;

            if (checkVariables)
            {
                var variablesToCheck = variables.Keys.ToList();
                root.CheckVariables(variablesToCheck);

                foreach (var v in variablesToCheck)
                    variables.Remove(v);
            }
        }

        /// <summary>
        /// Преобразует дерево выражений в вид, пригодный для отображения в TreeView.
        /// </summary>
        /// <returns></returns>
        public TreeNode ToTree()
        {
            return root.ToTree();
        }

        public override string ToString()
        {
            return root.ToString();
        }

        /// <summary>
        /// Поиск производной функции одной переменной.
        /// </summary>
        /// <param name="rank">порядок производной</param>
        /// <returns>полученная производная в виде FunctionParser</returns>
        /// /// <exception cref="InvalidOperationException">при попытке применить для функции многих переменных</exception>
        public FunctionParser Differentiate(int rank = 1)
        {
            if (rank == 0)
                return new FunctionParser(root.Clone(), new SortedDictionary<string, double>(variables));
            if (variables.Keys.Count == 0)
                return new FunctionParser("0");
            if (variables.Keys.Count > 1)
                throw new NotImplementedException("Поиск полной производной функции многих переменных не поддерживается.");

            Node diff = Node.Optimize(root.Differentiate());
            for (int i = rank - 1; i > 0; ++i)
                diff = Node.Optimize(root.Differentiate());

            return new FunctionParser(diff, new SortedDictionary<string, double>(variables));
        }

        /// <summary>
        /// Поиск частной производной по variable.
        /// </summary>
        /// <param name="variable">переменная, по которой осуществляется дифференцирование</param>
        /// <returns></returns>
        public FunctionParser DifferentiateBy(string variable)
        {
            SortedDictionary<string, double> tmp = new SortedDictionary<string, double>(variables);
            Node rt = root.DifferentiateBy(variable);
            return new FunctionParser(rt, tmp);
        }

        /// <summary>
        /// Определяет набор переменных, содержащихся в выражении и записывает в поле variables.
        /// </summary>
        /// <param name="expr">выражение</param>
        void DefineVariables(string expr)
        {
            //string pattern = $@"(^|[{Node.operations}])x(\d*)($|[{Node.operations}])";
            var matches = defineVariables.Matches(expr);
            foreach (Match match in matches)
            {
                if (!variables.ContainsKey(match.Value))
                    variables.Add(match.Value, double.NaN);
            }
        }

        /// <summary>
        /// Вычисление значение выражения с заданными значениями переменных
        /// </summary>
        /// <param name="values">переменные</param>
        /// <returns>значение выражения</returns>
        /// <exception cref="ArgumentException">если число параметров не совпадает с числом переменных.</exception>
        public double Evaluate(params double[] values)
        {
            if (values.Length != variables.Count)
                throw new ArgumentException("Число параметров не совпадает с числом переменных!");

            for (int i = 0; i<values.Length; ++i)
                variables[variables.ElementAt(i).Key] = values[i];

            root.SetVariables(variables);

            if (function == null)
                function = root.Functionalize();

            CachedValue = function();
            return CachedValue;
        }

        /// <summary>
        /// Оптимизация дерева
        /// </summary>
        /// <returns>оптимизированное дерево</returns>
        public FunctionParser Optimize(bool checkVariables = false)
        {
            SortedDictionary<string, double> tmp = new SortedDictionary<string, double>(variables);
            Node rt = Node.Optimize(root);
            return new FunctionParser(rt, tmp, checkVariables);
        }
    }
}
