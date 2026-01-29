using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest
{
  public partial class OneDriveAPI
  {
    public async Task<Resources.Drive> GetDefaultDrive()
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drive", m_BaseUrl);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);

      string response = resp?.Value;
      return Resources.Drive.Deserialize(response);
    } // GetDefaultDrive

    public async Task<Responses.ListDrivesResponse> ListDrives()
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drives", m_BaseUrl);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;

      return Responses.ListDrivesResponse.Deserialize(response);
    } // ListDrives

    public async Task<Resources.Drive> GetDrive(string driveId)
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drives/{1}", m_BaseUrl, driveId);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;

      return Resources.Drive.Deserialize(response);
    } // GetDrive

    public async Task<Responses.ListSharedFilesResponse> ListSharedFiles()
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drive/shared", m_BaseUrl);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;

      return Responses.ListSharedFilesResponse.Deserialize(response);
    } // ListSharedFiles

    public async Task<Responses.RecentlyUsedFilesResponse> RecentlyUsedFiles()
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drive/view.recent", m_BaseUrl);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;

      return Responses.RecentlyUsedFilesResponse.Deserialize(response);
    } // RecentlyUsedFiles

    public async Task<Responses.ChildrenResponse> DriveChildren(string driveId)
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drives/{1}/root/children", m_BaseUrl, driveId);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;

      return Responses.ChildrenResponse.Deserialize(response);
    } // DriveChildren
  }
}
