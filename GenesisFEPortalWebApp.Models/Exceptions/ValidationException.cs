using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Exceptions
{
    /// <summary>
    /// Excepción para errores de validación
    /// </summary>
    public class ValidationException : ApiException
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException(string message)
            : base(message, "VALIDATION_ERROR", 400)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(IDictionary<string, string[]> errors)
            : base("Se encontraron uno o más errores de validación.", "VALIDATION_ERROR", 400)
        {
            Errors = errors;
        }
    }
}
