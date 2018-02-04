using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTSampleWithWTS.Interfaces
{
    public interface IAuthenticationService
    {
        string Token { get; }

        void InitializeService(string subscriptionKey);
    }
}
