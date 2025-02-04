using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models.Customer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Validations
{
    public class IdentificationValidator : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            // Cambiamos el tipo de casting a CustomerFormModel
            var customer = (CustomerFormModel)validationContext.ObjectInstance;

            // Validamos que tengamos tanto el tipo como el número de identificación
            if (string.IsNullOrEmpty(customer.IdentificationTypeId) || string.IsNullOrEmpty(customer.Identification))
                return new ValidationResult("La identificación es requerida");

            // Aplicamos la validación según el tipo de identificación
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
