using OneDriveRest.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest
{
  public partial class OneDriveAPI
  {
    protected string m_BaseUrl = "https://api.onedrive.com/v1.0";
    public const string LoginRedirectUrl = "https://login.live.com/oauth20_desktop.srf";
    
    public enum HttpMethods
    {
      GET,
      POST,
      POSTJSON,
      DELETE,
      PUT,
      PATCH
    }    

    class ResponsePrimitive
    {
      public HttpWebResponse Response { get; set; }
      public Resources.Error Error { get; set; }
    } // ResponsePrimitive

    protected class GenericResponse
    {
      public string Value { get; set; }
      public HttpStatusCode StatusCode { get; set; }
      public Resources.Error Error { get; set; }
      public string Location { get; set; }
    } // GenericResponse

    public enum Scopes
    {
      Signin,
      OfflineAccess,
      OneDriveReadonly,
      OneDriveReadWrite,
      OneDriveAppFolder
    }

    public enum SpecialFolders
    {
      [EnumMember(Value = "documents")]
      Documents,
      [EnumMember(Value = "photos")]
      Photos,
      [EnumMember(Value = "cameraroll")]
      CameraRoll,
      [EnumMember(Value = "approot")]
      AppRoot,
      [EnumMember(Value = "music")]
      Music,
    }

    public OneDriveAPI(string clientId, string clientSecret, string accessToken, string refreshToken)
    {
      ClientID = clientId;
      ClientSecret = clientSecret;
      AccessToken = accessToken;
      RefreshToken = refreshToken;
    } // OneDriveAPI

    public string ClientID
    {
      get;
      protected set;
    }

    public string ClientSecret
    {
      get;
      protected set;
    }

    public DateTime AccessTokenExpirationDateTime
    {
      get;
      protected set;
    }

    public string AccessToken
    {
      get;
      protected set;
    }

    public string RefreshToken
    {
      get;
      protected set;
    }

    #region Public operations

    #endregion

    #region Private operations
    protected string GetScope(Scopes scope)
    {
      switch (scope) {
        case Scopes.Signin:
          return "wl.signin";
        case Scopes.OfflineAccess:
          return "wl.offline_access";
        case Scopes.OneDriveReadonly:
          return "onedrive.readonly";
        case Scopes.OneDriveReadWrite:
          return "onedrive.readwrite";
        case Scopes.OneDriveAppFolder:
          return "onedrive.appfolder";
      }
      return string.Empty;
    } // GetScope

    private async Task<GenericResponse> GetPutResponseAsync(string url, long totalSize, byte[] chunkData, long chunkOffset, long chunkLength)
    {
      return await GetResponseAsync(HttpMethods.PUT, url, null, totalSize, chunkData, chunkOffset, chunkLength);
    } // GetPutResponseAsync

    protected async Task<GenericResponse> GetResponseAsync(HttpMethods method, string url, Dictionary<string, string> parameters)
    {
      return await GetResponseAsync(method, url, parameters, 0, null, 0, 0);
    } // GetResponseAsync

    protected async Task<GenericResponse> GetResponseAsync(HttpMethods method, string url, Dictionary<string, string> parameters, 
      long putTotalSize, byte[] putData, long putDataOffset, long putDataLength)
    {
      // For GET requests put the parameters in the url:
      if (method == HttpMethods.GET && parameters != null && parameters.Count > 0) {
        StringBuilder sb = new StringBuilder();
        sb.Append(url);
        sb.Append("?");
        int idx = 0;
        foreach (KeyValuePair<string, string> kvp in parameters) {
          if (idx > 0)
            sb.Append("&");
          sb.AppendFormat("{0}={1}", WebUtility.UrlEncode(kvp.Key), WebUtility.UrlEncode(kvp.Value));
          idx++;
        }
        url = sb.ToString();
        parameters.Clear();
      }

      try {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.AllowAutoRedirect = false;
        ResponsePrimitive response = await GetResponseAsync(request, method, parameters, putTotalSize, putData, putDataOffset, putDataLength);
        if (response != null) {
          if (response.Error != null)
            return new GenericResponse() { Error = response.Error };

          if (response.Response != null) {
            using (response.Response) {
              using (Stream responseStream = response.Response.GetResponseStream()) {
                using (StreamReader sr = new StreamReader(responseStream)) {
                  string sRes = sr.ReadToEnd();
                  string location = response.Response.Headers[HttpResponseHeader.Location];
                  return new GenericResponse()
                  {
                    Value = sRes,
                    StatusCode = response.Response.StatusCode,
                    Location = location,
                  };
                }
              }
            }
          }
        }
      } catch (Exception ex) {
        System.Diagnostics.Debug.WriteLine(string.Format("Request failed\r\n{0}\r\n{1}", url, ex.Message));
        return new GenericResponse() {
          Error = new Resources.Error() { Code = ex.HResult.ToString(), Message = ex.Message }
        };
      }
      return null;
    } // GetResponseAsync

    private async Task<ResponsePrimitive> GetResponseAsync(HttpWebRequest request, HttpMethods method, Dictionary<string, string> postData, 
      long putTotalSize, byte[] putData, long putDataOffset, long putDataLength)
    {
      try {
        string strMethod = method.ToString();
        if (strMethod.StartsWith("POST"))
          request.Method = "POST";
        else
          request.Method = strMethod;

        //request.IfModifiedSince = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(AccessToken))
          request.Headers["Authorization"] = string.Format("bearer {0}", AccessToken);

        if (method == HttpMethods.POST) {
          if (postData != null) {
            StringBuilder sb = new StringBuilder();
            foreach (string key in postData.Keys) {
              if (sb.Length > 0)
                sb.Append("&");
              sb.AppendFormat("{0}={1}", WebUtility.UrlEncode(key), WebUtility.UrlEncode(postData[key]));
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(sb.ToString());
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            using (Stream stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null)) {
              stream.Write(byteArray, 0, byteArray.Length);
            }
          } else {
            request.ContentLength = 0;
          }
        } else if (method == HttpMethods.POSTJSON || method == HttpMethods.PATCH) {
          if (postData != null) {
            request.ContentType = "application/json;charset=UTF-8";
            int length = 0;
            foreach (string key in postData.Keys) {
              byte[] byteArray = Encoding.UTF8.GetBytes(postData[key]);
              using (Stream stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null)) {
                stream.Write(byteArray, 0, postData[key].Length);
                length += postData[key].Length;
              }
            }
            //request.ContentLength = length;
          } else {
            request.ContentLength = 0;
          }
        } else if (method == HttpMethods.PUT) {
          if (putData != null) {
            request.ContentLength = putDataLength;
            request.Headers[HttpRequestHeader.ContentRange] = string.Format("bytes {0}-{1}/{2}", putDataOffset, putDataOffset + putDataLength - 1, putTotalSize);
            using (Stream stream = await Task.Factory.FromAsync<Stream>(request.BeginGetRequestStream, request.EndGetRequestStream, null)) {
              stream.Write(putData, 0, (int)putDataLength);
            }
          }
        }
        HttpWebResponse res = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
        return new ResponsePrimitive() { Response = res };
      } catch (WebException ex) {
        if (ex.Response != null) {
          using (Stream rs = ex.Response.GetResponseStream()) {
            using (StreamReader reader = new StreamReader(rs)) {
              string err = reader.ReadToEnd();
              System.Diagnostics.Debug.WriteLine(string.Format("Error in <{0}>: {1}", "GetResponseAsync", err));
              System.Diagnostics.Debug.WriteLine(ex.Message);

              return new ResponsePrimitive() { Error = Resources.Error.Deserialize(err) };
            }
          }
        }
      } catch (Exception ex) {
        Exception exception = ex;
        while (exception.InnerException != null)
          exception = exception.InnerException;
        System.Diagnostics.Debug.WriteLine(string.Format("Error in <{0}>: {1}", "GetResponseAsync", exception.Message));

        return new ResponsePrimitive() {
          Error = new Resources.Error() { Code = exception.HResult.ToString(), Message = exception.Message }
        };
      }
      return null;
    } // GetResponseAsync

    private async Task<bool> EnsureTokenValid()
    {
      if (!string.IsNullOrEmpty(AccessToken) && (AccessTokenExpirationDateTime == DateTime.MinValue || DateTime.Now > AccessTokenExpirationDateTime))
        return await RefreshAccessToken();
      return true;
    } // EnsureTokenValid

    #endregion
  }
}
