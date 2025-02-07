using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Exceptions
{
    public class AccountLockedException : ApiException
    {
        public AccountLockedException(string message)
            : base(message, "ACCOUNT_LOCKED", 423)
        {
        }
    }
}
