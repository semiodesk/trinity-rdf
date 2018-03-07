using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semiodesk.Trinity.Exceptions
{
    public class StoreProviderMissingException : Exception
    {
        public StoreProviderMissingException(string message)
            : base(message)
        {
            
        }

        public StoreProviderMissingException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
