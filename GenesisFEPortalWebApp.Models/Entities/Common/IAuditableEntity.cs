using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Common
{
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        //long? CreatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
        //long? UpdatedBy { get; set; }
    }

}
