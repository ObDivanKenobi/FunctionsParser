using System;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace FunctionsParser
{
    public class FunctionParser
    {
        string expression;
        Node root;
        Func<double, double> function;

        public Func<double, double> Function
        {
            get
            {
                if (function == null)
                    function = root.Functionalize();

                return function;
            }
        }

        public FunctionParser(string expr)
        {
            expression = expr.Trim();

            string pattern = @"\s+",
                   replacement = "";
            expression = Regex.Replace(expression, pattern, replacement);

            //подставляем "подразумевающееся" умножение
            pattern = $@"(\d+)(x|[(]|{Node.Functions})";
            replacement = "$1*$2";
            expression = Regex.Replace(expression, pattern, replacement);

            root = Node.CreateNewNode(expression);
        }

        FunctionParser(Node new_root)
        {
            root = new_root;
        }

        public TreeNode ToTree()
        {
            return root.ToTree();
        }

        public FunctionParser Differentiate()
        {
            return new FunctionParser(root.Differentiate());
        }
    }
}
