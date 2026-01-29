using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest
{
  public partial class OneDriveAPI
  {
    public async Task<Resources.Permission> CreateSharingLink(string itemId, Facets.SharingLink.Scopes scope, Facets.SharingLink.SharingLinkType type)
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/items/{1}/action.createLink", m_BaseUrl, itemId);
      GenericResponse resp = await GetResponseAsync(HttpMethods.POSTJSON, url,
        new Dictionary<string, string>() { { "data", Utility.Serialization.Serialize(new { type = type, scope = scope }) } });
      string response = resp?.Value;

      return Resources.Permission.Deserialize(response);
    } // CreateSharingLink   

    public async Task<Responses.ListSharedFilesResponse> SharedWithMe()
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/view.sharedWithMe", m_BaseUrl);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;

      return Responses.ListSharedFilesResponse.Deserialize(response);
    } // SharedWithMe 
  }
}
