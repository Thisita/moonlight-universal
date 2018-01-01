using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonlight.Exception
{
    public class PairingInProgressException : PairingException
    {
        public PairingInProgressException()
        {
        }

        public PairingInProgressException(string message) : base(message)
        {
        }

        public PairingInProgressException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
