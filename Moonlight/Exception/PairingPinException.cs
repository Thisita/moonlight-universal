using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonlight.Exception
{
    public class PairingPinException : PairingException
    {
        public PairingPinException()
        {
        }

        public PairingPinException(string message) : base(message)
        {
        }

        public PairingPinException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
