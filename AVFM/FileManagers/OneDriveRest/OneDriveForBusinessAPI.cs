using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest
{
  public partial class OneDriveForBusinessAPI : OneDriveAPI
  {
    public OneDriveForBusinessAPI(string clientId, string clientSecret, string accessToken, string refreshToken) :
      base(clientId, clientSecret, accessToken, refreshToken)
    {

    }

    /// <summary>
    /// Get the authorization code url.
    /// Upon successful authentication and authorization of your application, the web browser will be redirected to your redirect URL with additional parameters added to the URL.
    /// https://login.live.com/oauth20_authorize.srf?code=df6aa589-1080-b241-b410-c4dff65dbf7c
    /// </summary>
    /// <param name="scopes">The scopes to request</param>
    /// <returns>The url</returns>
    public override string GetAuthorizationCodeUrl(List<Scopes> scopes)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("https://login.microsoftonline.com/common/oauth2/authorize?response_type=code&client_id={0}", ClientID);
      sb.Append("&response_type=code");
      sb.AppendFormat("&redirect_uri={0}", LoginRedirectUrl);

      return sb.ToString();
    } // GetAuthorizationCodeUrl

    /// <summary>
    /// Get the access token from the authorization code
    /// </summary>
    /// <param name="authCode">The authorization code</param>
    /// <returns>True on success</returns>
    public override async Task<bool> RedeemCode(string authCode)
    {
      string url = "https://login.microsoftonline.com/common/oauth2/token";

      GenericResponse resp = await GetResponseAsync(HttpMethods.POST, url,
                                               new Dictionary<string, string>() {
                                                  {"client_id", ClientID},
                                                  {"client_secret", ClientSecret},
                                                  {"redirect_uri", LoginRedirectUrl},
                                                  {"code", authCode},
                                                  {"grant_type", "authorization_code"},
                                                  {"resource", "https://api.office.com/discovery/"},
                                                 });
      string response = resp?.Value;
      Responses.RedeemCodeResponse r = Responses.RedeemCodeResponse.Deserialize(response);
      if (r != null) {
        AccessToken = r.AccessToken;
        AccessTokenExpirationDateTime = DateTime.Now.AddSeconds(r.ExpiresIn);
        RefreshToken = r.RefreshToken;
      }
      return !string.IsNullOrEmpty(AccessToken);
    } // RedeemCode
  }
}
