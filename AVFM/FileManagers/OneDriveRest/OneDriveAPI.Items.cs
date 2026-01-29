using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest
{
  public partial class OneDriveAPI
  {
    /// <summary>
    /// Copy a item
    /// </summary>
    /// <param name="itemId">The item id</param>
    /// <param name="name">The copy name</param>
    /// <param name="destinationId">The destination id</param>
    /// <returns>The url to monitor the copy operation. Use this url with <see cref="OperationStatus(string)"/> </returns>
    public async Task<string> Copy(string itemId, string name, string destinationId)
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/items/{1}/action.copy", m_BaseUrl, itemId);
      Resources.Item item = new Resources.Item()
      {
        ParentReference = new Resources.ItemReference() { Id = destinationId }
      };

      GenericResponse resp = await GetResponseAsync(HttpMethods.POSTJSON, url, new Dictionary<string, string>() { { "data", item.Serialize() } });
      return resp != null && resp.StatusCode == HttpStatusCode.Accepted ? resp.Location : string.Empty;
    } // Copy

    /// <summary>
    /// Get the status of an async operation
    /// </summary>
    /// <param name="monitorUrl">The monitor url</param>
    /// <returns>The operation status</returns>
    public async Task<Resources.AsyncOperationStatus> OperationStatus(string monitorUrl)
    {
      await EnsureTokenValid();

      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, monitorUrl, null);
      string response = resp?.Value;

      return resp != null && resp.StatusCode == HttpStatusCode.Accepted ?
        Resources.AsyncOperationStatus.Deserialize(response) :
        null;
    } // OperationStatus

    /// <summary>
    /// Create a new folder
    /// </summary>
    /// <param name="parentId">The parent Id</param>
    /// <param name="folderName">The new folder name</param>
    /// <returns>True on success</returns>
    public async Task<bool> Createfolder(string parentId, string folderName)
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drive/items/{1}/children", m_BaseUrl, parentId);

      Requests.CreateFolderRequest newFolder = new Requests.CreateFolderRequest() { Name = folderName, Folder = new Facets.Folder() };
      GenericResponse resp = await GetResponseAsync(HttpMethods.POSTJSON, url, new Dictionary<string, string>() { { "data", newFolder.Serialize() } });
      return resp != null && resp.StatusCode == HttpStatusCode.Created;
    } // Createfolder

    /// <summary>
    /// Delete an item
    /// </summary>
    /// <param name="itemId">The item Id</param>
    /// <returns>True on success</returns>
    public async Task<bool> Delete(string itemId)
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drive/items/{1}", m_BaseUrl, itemId);

      GenericResponse resp = await GetResponseAsync(HttpMethods.DELETE, url, null);
      return resp != null && resp.StatusCode == HttpStatusCode.NoContent;
    } // Delete

    public async Task<Resources.Item> ItemMetadata(string itemId)
    {
      return await ItemMetadata(itemId, string.Empty);
    } // ItemMetadata

    public async Task<Resources.Item> ItemMetadata(string itemId, string select)
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/items/{1}", m_BaseUrl, itemId);
      Dictionary<string, string> pars = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(select))
        pars["select"] = select;
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, pars);
      string response = resp?.Value;

      return Resources.Item.Deserialize(response);
    } // ItemMetadata

    public async Task<Resources.Item> ItemMetadataFromPath(string itemPath)
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drive/root:/{1}", m_BaseUrl, Uri.EscapeDataString(itemPath));
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;

      return Resources.Item.Deserialize(response);
    } // ItemMetadataFromPath

    public async Task<Responses.ChildrenResponse> ItemChildren(string itemId)
    {
      return await ItemChildren(itemId, string.Empty, string.Empty);
    } // ItemChildren

    /// <summary>
    /// Get the children of a item.
    /// https://dev.onedrive.com/odata/optional-query-parameters.htm
    /// </summary>
    /// <param name="itemId">The item id</param>
    /// <param name="select">The properties select string</param>
    /// <param name="orderBy">The order by string</param>
    /// <returns></returns>
    public async Task<Responses.ChildrenResponse> ItemChildren(string itemId, string select, string orderBy)
    {
      await EnsureTokenValid();
      string url = string.Format("{0}/drive/items/{1}/children", m_BaseUrl, itemId);

      Dictionary<string, string> pars = new Dictionary<string, string>();
      if (!string.IsNullOrEmpty(select))
        pars["select"] = select;
      if (!string.IsNullOrEmpty(orderBy))
        pars["orderby"] = orderBy;
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, pars);
      string response = resp?.Value;
      if (!string.IsNullOrEmpty(response))
        return Responses.ChildrenResponse.Deserialize(response);
      return null;
    } // ItemChildren

    public async Task<Responses.ChildrenResponse> ItemChildrenNext(string nextLink)
    {
      await EnsureTokenValid();
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, nextLink, null);
      string response = resp?.Value;

      return Responses.ChildrenResponse.Deserialize(response);
    } // ItemChildrenNext

    public async Task<Responses.ItemThumbnailsResponse> ItemThumbnails(string itemId)
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/items/{1}/thumbnails", m_BaseUrl, itemId);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);
      string response = resp?.Value;
      if (!string.IsNullOrEmpty(response))
        return Responses.ItemThumbnailsResponse.Deserialize(response);
      return null;
    } // Thumbnails

    public async Task<string> ItemDownloadUrl(string itemId)
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/items/{1}/content", m_BaseUrl, itemId);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, null);

      return resp?.Location;
    } // ItemDownloadUrl

    /// <summary>
    /// Update the metadata of a item.
    /// For best performance you shouldn't include existing values that haven't changed.
    /// </summary>
    /// <param name="item">The item to update</param>
    /// <returns>The updated item</returns>
    public async Task<Resources.Item> UpdateItem(Resources.Item item)
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/items/{1}", m_BaseUrl, item.Id);
      GenericResponse resp = await GetResponseAsync(HttpMethods.PATCH, url, new Dictionary<string, string>() { { "data", item.Serialize() } });
      string response = resp?.Value;

      return Resources.Item.Deserialize(response);
    } // UpdateItem

    public async Task<Resources.Item> MoveItem(string itemId, string newPartentId)
    {
      Resources.Item item = new Resources.Item()
      {
        Id = itemId,
        ParentReference = new Resources.ItemReference() { Id = newPartentId }
      };
      return await UpdateItem(item);
    } // MoveItem

    /// <summary>
    /// Search items. To search the root object pass an empty parentId
    /// https://dev.onedrive.com/odata/filtering.htm
    /// </summary>
    /// <param name="parentId">The parent item id</param>
    /// <param name="query">The search query</param>
    /// <returns>The matching items</returns>
    public async Task<Responses.SearchResponse> SearchItems(string parentId, string query)
    {
      await EnsureTokenValid();

      string url = !string.IsNullOrEmpty(parentId) ?
        string.Format("{0}/drive/items/{1}/view.search", m_BaseUrl, parentId) :
        string.Format("{0}/drive/root/view.search", m_BaseUrl);
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, url, new Dictionary<string, string>() { { "q", query } });
      string response = resp?.Value;

      return Responses.SearchResponse.Deserialize(response);
    } // SearchItems

    #region Upload
    public async Task<Responses.UploadResponse> UploadCreateSession(string parentId, string fileName)
    {
      await EnsureTokenValid();

      string url = string.Format("{0}/drive/items/{1}:/{2}:/upload.createSession", m_BaseUrl, parentId, Uri.EscapeDataString(fileName));
      GenericResponse resp = await GetResponseAsync(HttpMethods.POST, url, null);
      string response = resp?.Value;

      return Responses.UploadResponse.Deserialize(response);
    } // UploadCreateSession

    /// <summary>
    /// Upload a file fragment.
    /// Use a size multiple of 320 KiB (320 * 1024 bytes)
    /// </summary>
    /// <param name="uploadUrl">The upload url obtained by <see cref="UploadCreateSession(string, string)"/></param>
    /// <param name="data">The data to upload</param>
    /// <param name="dataOffset">The offset from the beginning of the file</param>
    /// <param name="dataLength">The data length</param>
    /// <param name="totalSize">The file total size</param>
    /// <returns></returns>
    public async Task<Responses.UploadResponse> UploadFragment(string uploadUrl, byte[] data, long dataOffset, long dataLength, long totalSize)
    {
      GenericResponse resp = await GetPutResponseAsync(uploadUrl, totalSize, data, dataOffset, dataLength);
      string response = resp?.Value;

      return Responses.UploadResponse.Deserialize(response);
    } // UploadFragment

    public async Task<Responses.UploadResponse> UploadStatus(string uploadUrl)
    {
      GenericResponse resp = await GetResponseAsync(HttpMethods.GET, uploadUrl, null);
      string response = resp?.Value;

      return Responses.UploadResponse.Deserialize(response);
    } // UploadStatus

    public async Task<bool> UploadCancel(string uploadUrl)
    {
      await EnsureTokenValid();

      GenericResponse resp = await GetResponseAsync(HttpMethods.DELETE, uploadUrl, null);
      return resp != null && resp.StatusCode == HttpStatusCode.NoContent;
    } // UploadCancel
    #endregion
  }
}
