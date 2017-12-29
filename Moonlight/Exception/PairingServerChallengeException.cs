using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonlight.Exception
{
    public class PairingServerChallengeException : PairingException
    {
        public PairingServerChallengeException()
        {
        }

        public PairingServerChallengeException(string message) : base(message)
        {
        }

        public PairingServerChallengeException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
