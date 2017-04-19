using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsParserNodes
{
    [Serializable]
    public class ParametersMismatchException : Exception
    {
        public ParametersMismatchException() { }

        public ParametersMismatchException(string message) : base(message) { }

        public ParametersMismatchException(string message, Exception inner) : base(message, inner) { }

        protected ParametersMismatchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class BracketsMismatchException : Exception
    {
        public BracketsMismatchException() { }

        public BracketsMismatchException(string message) : base(message) { }

        public BracketsMismatchException(string message, Exception inner) : base(message, inner) { }

        protected BracketsMismatchException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
