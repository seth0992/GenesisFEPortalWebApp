using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{

    /// <summary>
    /// Interfaz para el servicio de encriptación
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encripta un texto plano usando una clave específica
        /// </summary>
        string Encrypt(string plainText, string key);

        /// <summary>
        /// Desencripta un texto cifrado usando una clave específica
        /// </summary>
        string Decrypt(string cipherText, string key);
        /// <summary>
        /// Genera una clave segura para uso criptográfico
        /// </summary>
        string GenerateSecureKey();
    }

    /// <summary>
    /// Servicio para encriptar/desencriptar valores sensibles
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly string _masterKey;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
        {
            _masterKey = configuration["Security:MasterKey"]
                ?? throw new InvalidOperationException("Master key not configured");
            _logger = logger;
        }
        public string GenerateSecureKey()
        {
            // Generar una clave de 64 bytes (512 bits) para asegurar compatibilidad
            var keyBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(keyBytes);
            return Convert.ToBase64String(keyBytes);
        }
        private byte[] DeriveKey(string key)
        {
            // Asegurar que la clave derivada sea de 32 bytes (256 bits)
            using var deriveBytes = new Rfc2898DeriveBytes(
                key + _masterKey,
                new byte[] { 0x43, 0x87, 0x23, 0x72 },
                10000, // Aumentado el número de iteraciones para mayor seguridad
                HashAlgorithmName.SHA256);

            return deriveBytes.GetBytes(32); // 32 bytes = 256 bits
        }

        /// <summary>
        /// Encripta un valor usando AES
        /// </summary>
        public string Encrypt(string plainText, string key)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = DeriveKey(key);
                aes.GenerateIV();

                using var msEncrypt = new MemoryStream();
                msEncrypt.Write(aes.IV, 0, aes.IV.Length);

                using (var cryptoStream = new CryptoStream(msEncrypt,
                    aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(plainText);
                }

                return Convert.ToBase64String(msEncrypt.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting value");
                throw new SecurityException("Error encrypting value", ex);
            }
        }

        /// <summary>
        /// Desencripta un valor usando AES
        /// </summary>
        public string Decrypt(string cipherText, string key)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);

                using var aes = Aes.Create();
                aes.Key = DeriveKey(key);

                var iv = new byte[aes.IV.Length];
                Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
                aes.IV = iv;

                using var msDecrypt = new MemoryStream(
                    cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
                using var cryptoStream = new CryptoStream(msDecrypt,
                    aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var reader = new StreamReader(cryptoStream);

                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting value");
                throw new SecurityException("Error decrypting value", ex);
            }
        }

        //private byte[] DeriveKey(string key)
        //{
        //    using var deriveBytes = new Rfc2898DeriveBytes(
        //        key + _masterKey,
        //        new byte[] { 0x43, 0x87, 0x23, 0x72 },
        //        1000,
        //        HashAlgorithmName.SHA256);

        //    return deriveBytes.GetBytes(32);
        //}
    }
}
