using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moonlight.Exception
{
    public class AlreadyPairedException : PairingException
    {
        public AlreadyPairedException()
        {
        }

        public AlreadyPairedException(string message) : base(message)
        {
        }

        public AlreadyPairedException(string message, System.Exception innerException) : base(message, innerException)
        {
        }
    }
}
