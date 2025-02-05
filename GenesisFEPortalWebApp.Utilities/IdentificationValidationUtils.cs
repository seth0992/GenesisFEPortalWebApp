using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Utilities
{
    // GenesisFEPortalWebApp.Utilities/IdentificationValidationUtils.cs
    public static class IdentificationValidationUtils
    {
        // Tipos de identificación predefinidos
        public const string CEDULA_FISICA = "01";
        public const string CEDULA_JURIDICA = "02";
        public const string DIMEX = "03";
        public const string NITE = "04";

        /// <summary>
        /// Valida una identificación según su tipo
        /// </summary>
        /// <param name="identificationTypeId">Tipo de identificación</param>
        /// <param name="identification">Número de identificación</param>
        /// <returns>Resultado de la validación</returns>
        public static ValidationResult? ValidateIdentification(string identificationTypeId, string identification)
        {
            if (string.IsNullOrEmpty(identificationTypeId) || string.IsNullOrEmpty(identification))
                return new ValidationResult("La identificación es requerida");

            // Remover espacios y guiones
            identification = identification.Replace(" ", "").Replace("-", "");

            return identificationTypeId switch
            {
                CEDULA_FISICA => ValidateCedulaFisica(identification),
                CEDULA_JURIDICA => ValidateCedulaJuridica(identification),
                DIMEX => ValidateDIMEX(identification),
                NITE => ValidateNITE(identification),
                _ => ValidationResult.Success
            };
        }

        /// <summary>
        /// Valida una cédula física de Costa Rica
        /// </summary>
        public static ValidationResult? ValidateCedulaFisica(string identification)
        {
            // Formato: 9 dígitos numéricos
            if (!Regex.IsMatch(identification, @"^\d{9}$"))
                return new ValidationResult("Cédula física debe tener 9 dígitos numéricos");

            // Validar provincia (primer dígito entre 1 y 9)
            var provincia = int.Parse(identification[0].ToString());
            if (provincia < 1 || provincia > 9)
                return new ValidationResult("Provincia inválida en cédula física");

            // Validar que el número de secuencia no sea cero
            var secuencia = int.Parse(identification.Substring(1, 3));
            if (secuencia == 0)
                return new ValidationResult("Secuencia inválida en cédula física");

            return ValidationResult.Success;
                
        }

        /// <summary>
        /// Valida una cédula jurídica de Costa Rica
        /// </summary>
        public static ValidationResult? ValidateCedulaJuridica(string identification)
        {
            // Formato: 10 dígitos, comienza con 3
            if (!Regex.IsMatch(identification, @"^[2-3]\d{9}$"))
                return new ValidationResult("Cédula jurídica debe tener 10 dígitos y comenzar con 2 o 3");

            // Validar tipo de entidad (segundo dígito)
            var tipoEntidad = int.Parse(identification[1].ToString());
            if (tipoEntidad < 0 || tipoEntidad > 9)
                return new ValidationResult("Tipo de entidad inválido en cédula jurídica");

            // Validar número de secuencia
            var secuencia = int.Parse(identification.Substring(2, 3));
            if (secuencia == 0)
                return new ValidationResult("Secuencia inválida en cédula jurídica");

            return ValidationResult.Success;
        }

        /// <summary>
        /// Valida un DIMEX (Documento de Identificación Migratorio para Extranjeros)
        /// </summary>
        public static ValidationResult? ValidateDIMEX(string identification)
        {
            // Formato: 11 o 12 dígitos numéricos
            if (!Regex.IsMatch(identification, @"^\d{11,12}$"))
                return new ValidationResult("DIMEX debe tener 11 o 12 dígitos numéricos");

            // Validar que comience con 1
            if (!identification.StartsWith("1"))
                return new ValidationResult("DIMEX debe comenzar con 1");

            return ValidationResult.Success;
        }

        /// <summary>
        /// Valida un NITE (Número de Identificación Tributario Especial)
        /// </summary>
        public static ValidationResult? ValidateNITE(string identification)
        {
            // Formato: 10 dígitos, comienza con 10
            if (!Regex.IsMatch(identification, @"^10\d{8}$"))
                return new ValidationResult("NITE debe tener 10 dígitos y comenzar con 10");

            var secuencia = int.Parse(identification.Substring(2, 6));
            if (secuencia == 0)
                return new ValidationResult("Secuencia inválida en NITE");

            return ValidationResult.Success;
        }

        /// <summary>
        /// Formatea una identificación según su tipo
        /// </summary>
        public static string FormatIdentification(string identificationTypeId, string identification)
        {
            // Remover espacios y guiones
            identification = identification.Replace(" ", "").Replace("-", "");

            return identificationTypeId switch
            {
                CEDULA_FISICA => Regex.Replace(identification, @"(\d)(\d{4})(\d{4})", "$1-$2-$3"),
                CEDULA_JURIDICA => Regex.Replace(identification, @"(\d)(\d{3})(\d{6})", "$1-$2-$3"),
                DIMEX => Regex.Replace(identification, @"(\d{4})(\d{4})(\d+)", "$1-$2-$3"),
                NITE => Regex.Replace(identification, @"(\d{2})(\d{3})(\d{5})", "$1-$2-$3"),
                _ => identification
            };
        }
    }
}
