using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Exceptions
{
    /// <summary>
    /// Excepción para recursos no encontrados
    /// </summary>
    public class NotFoundException : ApiException
    {
        public NotFoundException(string message)
            : base(message, "NOT_FOUND", 404)
        {
        }

        public NotFoundException(string resource, object key)
            : base($"El recurso {resource} con id {key} no fue encontrado.", "NOT_FOUND", 404)
        {
        }
    }
}
