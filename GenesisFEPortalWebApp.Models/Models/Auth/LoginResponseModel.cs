using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Auth
{
    public class LoginResponseModel
    {
        [JsonProperty("token")]  // Asegura el mapeo correcto con el JSON
        public string Token { get; set; }

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonProperty("tokenExpired")]
        public long TokenExpired { get; set; }

        [JsonProperty("user")]
        public UserDto User { get; set; }
    }
}
