using GenesisFEPortalWebApp.Models.Entities.Core;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer.Validations
{
    public class IdentificationValidator : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            var customer = (CustomerModel)validationContext.ObjectInstance;
            if (string.IsNullOrEmpty(customer.IdentificationTypeId) || string.IsNullOrEmpty(customer.Identification))
                return new ValidationResult("La identificación es requerida");

            return customer.IdentificationTypeId switch
            {
                "01" => ValidateCedulaFisica(customer.Identification),
                "02" => ValidateCedulaJuridica(customer.Identification),
                "03" => ValidateDIMEX(customer.Identification),
                "04" => ValidateNITE(customer.Identification),
                _ => ValidationResult.Success
            };
        }

        private ValidationResult? ValidateCedulaFisica(string identification)
        {
            // Formato: 9 dígitos sin guiones
            if (!Regex.IsMatch(identification, @"^\d{9}$"))
                return new ValidationResult("La cédula física debe tener 9 dígitos numéricos");

            return ValidationResult.Success;
        }

        private ValidationResult? ValidateCedulaJuridica(string identification)
        {
            // Formato: 10 dígitos, inicia con 3
            if (!Regex.IsMatch(identification, @"^3\d{9}$"))
                return new ValidationResult("La cédula jurídica debe tener 10 dígitos y comenzar con 3");

            return ValidationResult.Success;
        }

        private ValidationResult? ValidateDIMEX(string identification)
        {
            // Formato: 11 o 12 dígitos
            if (!Regex.IsMatch(identification, @"^\d{11,12}$"))
                return new ValidationResult("El DIMEX debe tener 11 o 12 dígitos numéricos");

            return ValidationResult.Success;
        }

        private ValidationResult? ValidateNITE(string identification)
        {
            // Formato: 10 dígitos, inicia con 10
            if (!Regex.IsMatch(identification, @"^10\d{8}$"))
                return new ValidationResult("El NITE debe tener 10 dígitos y comenzar con 10");

            return ValidationResult.Success;
        }
    }
}
