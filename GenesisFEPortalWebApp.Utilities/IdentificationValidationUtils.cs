using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Utilities
{
    public static class IdentificationValidationUtils
    {
        // Tipos de identificación predefinidos
        public const string CEDULA_FISICA = "01";
        public const string CEDULA_JURIDICA = "02";
        public const string DIMEX = "03";
        public const string NITE = "04";

        public static ValidationResult? ValidateIdentification(string identificationTypeId, string identification)
        {
            if (string.IsNullOrEmpty(identificationTypeId) || string.IsNullOrEmpty(identification))
                return new ValidationResult("La identificación es requerida");

            return identificationTypeId switch
            {
                CEDULA_FISICA => ValidateCedulaFisica(identification),
                CEDULA_JURIDICA => ValidateCedulaJuridica(identification),
                DIMEX => ValidateDIMEX(identification),
                NITE => ValidateNITE(identification),
                _ => ValidationResult.Success
            };
        }

        public static ValidationResult? ValidateCedulaFisica(string identification)
        {
            // Formato: 9 dígitos numéricos
            if (!Regex.IsMatch(identification, @"^\d{9}$"))
                return new ValidationResult("Cédula física debe tener 9 dígitos numéricos");

            // Validación de checksum (algoritmo de validación específico de Costa Rica)
            return ValidateCedulaChecksum(identification)
                ? ValidationResult.Success
                : new ValidationResult("Cédula física no válida");
        }

        public static ValidationResult? ValidateCedulaJuridica(string identification)
        {
            // Formato: 10 dígitos, comienza con 3
            if (!Regex.IsMatch(identification, @"^3\d{9}$"))
                return new ValidationResult("Cédula jurídica debe tener 10 dígitos y comenzar con 3");

            return ValidationResult.Success;
        }

        public static ValidationResult? ValidateDIMEX(string identification)
        {
            // Formato: 11 o 12 dígitos numéricos
            if (!Regex.IsMatch(identification, @"^\d{11,12}$"))
                return new ValidationResult("DIMEX debe tener 11 o 12 dígitos numéricos");

            return ValidationResult.Success;
        }

        public static ValidationResult? ValidateNITE(string identification)
        {
            // Formato: 10 dígitos, comienza con 10
            if (!Regex.IsMatch(identification, @"^10\d{8}$"))
                return new ValidationResult("NITE debe tener 10 dígitos y comenzar con 10");

            return ValidationResult.Success;
        }

        // Algoritmo de validación de checksum para cédula física costarricense
        private static bool ValidateCedulaChecksum(string identification)
        {
            int[] multiplicadores = { 2, 1, 2, 1, 2, 1, 2, 1, 2 };
            int suma = 0;

            for (int i = 0; i < 9; i++)
            {
                int digito = int.Parse(identification[i].ToString());
                int resultado = digito * multiplicadores[i];
                suma += resultado > 9 ? resultado - 9 : resultado;
            }

            int checksum = (10 - (suma % 10)) % 10;
            return checksum == int.Parse(identification[8].ToString());
        }
    }
}
