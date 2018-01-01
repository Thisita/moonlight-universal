using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonlight.Exception
{
    public class PairingException : System.Exception
    {
        public PairingException()
        {
        }

        public PairingException(string message) : base(message)
        {
        }

        public PairingException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
