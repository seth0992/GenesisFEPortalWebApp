using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Core
{
    public interface IHasTenant
    {
        long TenantId { get; set; }
    }
}
