using System;
using System.Collections.Generic;
using System.Text;

namespace NFun.TypeInferenceCalculator.Errors
{
    public class TicException:Exception
    {
        public TicException(string message) :base(message)
        {
            
        }
    }
}
