using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest
{
  public partial class OneDriveAPI
  {
    /// <summary>
    /// Get the authorization code url.
    /// Upon successful authentication and authorization of your application, the web browser will be redirected to your redirect URL with additional parameters added to the URL.
    /// https://login.live.com/oauth20_authorize.srf?code=df6aa589-1080-b241-b410-c4dff65dbf7c
    /// </summary>
    /// <param name="scopes">The scopes to request</param>
    /// <returns>The url</returns>
    public virtual string GetAuthorizationCodeUrl(List<Scopes> scopes)
    {
      StringBuilder sb = new StringBuilder();
      sb.AppendFormat("https://login.live.com/oauth20_authorize.srf?client_id={0}&scope=", ClientID);

      int idx = 0;
      foreach (Scopes scope in scopes) {
        if (idx > 0)
          sb.Append(" ");
        sb.Append(GetScope(scope));
        idx++;
      }
      sb.Append("&response_type=code");
      sb.AppendFormat("&redirect_uri={0}", LoginRedirectUrl);

      return sb.ToString();
    } // GetAuthorizationCodeUrl

    /// <summary>
    /// Get the access token from the authorization code
    /// </summary>
    /// <param name="authCode">The authorization code</param>
    /// <returns>True on success</returns>
    public virtual async Task<bool> RedeemCode(string authCode)
    {
      string url = "https://login.live.com/oauth20_token.srf";

      GenericResponse resp = await GetResponseAsync(HttpMethods.POST, url,
                                               new Dictionary<string, string>() {
                                                  {"client_id", ClientID},
                                                  {"client_secret", ClientSecret},
                                                  {"redirect_uri", LoginRedirectUrl},
                                                  {"code", authCode},
                                                  {"grant_type", "authorization_code"},
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

    /// <summary>
    /// Refresh the access token
    /// </summary>
    /// <returns>True on success</returns>
    public async Task<bool> RefreshAccessToken()
    {
      if (string.IsNullOrEmpty(RefreshToken))
        return false;

      string url = "https://login.live.com/oauth20_token.srf";

      GenericResponse resp = await GetResponseAsync(HttpMethods.POST, url,
                                               new Dictionary<string, string>() {
                                                  {"client_id", ClientID},
                                                  {"client_secret", ClientSecret},
                                                  {"refresh_token", RefreshToken},
                                                  {"grant_type", "refresh_token"},
                                                 });

      string response = resp?.Value;
      Responses.RedeemCodeResponse r = Responses.RedeemCodeResponse.Deserialize(response);
      if (r != null) {
        AccessToken = r.AccessToken;
        AccessTokenExpirationDateTime = DateTime.Now.AddSeconds(r.ExpiresIn);
        RefreshToken = r.RefreshToken;
      }
      return !string.IsNullOrEmpty(AccessToken);
    } // RefreshAccessToken

    public async Task<bool> Logout()
    {
      string url = "https://login.live.com/oauth20_logout.srf";

      GenericResponse resp =await GetResponseAsync(HttpMethods.GET, url,
                                               new Dictionary<string, string>() {
                                                  {"client_id", ClientID},                                                 
                                                  {"redirect_uri", LoginRedirectUrl}
                                                 });
      string response = resp?.Value;
      AccessToken = string.Empty;
      AccessTokenExpirationDateTime = DateTime.MinValue;
      RefreshToken = string.Empty;

      return true;
    } // Logout
  }
}
