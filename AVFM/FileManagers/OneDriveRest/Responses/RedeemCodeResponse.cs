using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class RedeemCodeResponse : APIResponse
  {
    [JsonProperty(PropertyName = "access_token")]
    public string AccessToken { get; set; }

    [JsonProperty(PropertyName = "authentication_token")]
    public string AuthenticationToken { get; set; }

    [JsonProperty(PropertyName = "code")]
    public string Code { get; set; }

    [JsonProperty(PropertyName = "expires_in")]
    public int ExpiresIn { get; set; }

    [JsonProperty(PropertyName = "refresh_token")]
    public string RefreshToken { get; set; }

    [JsonProperty(PropertyName = "scope")]
    public string Scope { get; set; }

    [JsonProperty(PropertyName = "state")]
    public string State { get; set; }

    [JsonProperty(PropertyName = "token_type")]
    public string TokenType { get; set; }

    public static RedeemCodeResponse Deserialize(string value)
    {
      if (string.IsNullOrEmpty(value))
        return null;
                    
      return APIResponse.Deserialize<RedeemCodeResponse>(value);
    } // Deserialize
  }
}
