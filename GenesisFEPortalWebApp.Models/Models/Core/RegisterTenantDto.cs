using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Core
{
    public class RegisterTenantDto
    {
        // Datos de la empresa
        [Required(ErrorMessage = "El nombre de la empresa es requerido")]
        [StringLength(255, ErrorMessage = "El nombre no puede exceder los 255 caracteres")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La identificación es requerida")]
        [StringLength(50, ErrorMessage = "La identificación no puede exceder los 50 caracteres")]
        public string Identification { get; set; } = string.Empty;

        [Required(ErrorMessage = "El tipo de identificación es requerido")]
        public string IdentificationTypeId { get; set; } = string.Empty;

        // Datos del usuario administrador
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es requerido")]
        [StringLength(100, ErrorMessage = "El apellido no puede exceder los 100 caracteres")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es requerido")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [StringLength(255, ErrorMessage = "El correo no puede exceder los 255 caracteres")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "La contraseña debe contener al menos una mayúscula, una minúscula, un número y un carácter especial")]
        public string Password { get; set; } = string.Empty;

        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
