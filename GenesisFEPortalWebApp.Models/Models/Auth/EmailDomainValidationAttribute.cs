using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Auth
{
    public class EmailDomainValidationAttribute : ValidationAttribute
    {
        private readonly string[] _allowedDomains = { "gmail.com", "outlook.com", "yahoo.com" };

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var email = value as string;
            if (string.IsNullOrEmpty(email)) return ValidationResult.Success;

            var domain = email.Split('@').LastOrDefault();
            return _allowedDomains.Contains(domain)
                ? ValidationResult.Success
                : new ValidationResult("Dominio de correo no permitido");
        }
    }
}
