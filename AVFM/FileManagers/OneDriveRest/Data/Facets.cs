using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Facets
{
  public abstract class FacetBase
  {
    public enum Scopes
    {
      [EnumMember(Value = "anonymous")]
      Anonymous,
      [EnumMember(Value = "organization")]
      Organization,
      [EnumMember(Value = "users")]
      Users
    } // Scopes

    protected static T Deserialize<T>(string value)
    {
      return Utility.Serialization.Deserialize<T>(value);
    } // Deserialize

    protected static string Serialize<T>(T obj)
    {
      return Utility.Serialization.Serialize(obj);
    } // Serialize
  } // FacetBase

  public class Folder : FacetBase
  {
    [JsonProperty(PropertyName = "childCount")]
    public long ChildCount { get; set; }
  } // FolderFacet

  public class Hashes : FacetBase
  {
    [JsonProperty(PropertyName = "sha1Hash")]
    public string Sha1Hash { get; set; }

    [JsonProperty(PropertyName = "crc32Hash")]
    public string Crc32Hash { get; set; }

    [JsonProperty(PropertyName = "quickXorHash")]
    public string QuickXorHash { get; set; }
  } // HashesFacet

  public class File : FacetBase
  {
    [JsonProperty(PropertyName = "mimeType")]
    public string MimeType { get; set; }

    [JsonProperty(PropertyName = "hashes")]
    public Hashes Hashes { get; set; }

    [JsonProperty(PropertyName = "processingMetadata")]
    public bool ProcessingMetadata { get; set; }
  } // FileFacet

  public class FileSystemInfo : FacetBase
  {
    [JsonProperty(PropertyName = "createdDateTime")]
    public DateTime CreatedDateTime { get; set; }

    [JsonProperty(PropertyName = "lastModifiedDateTime")]
    public DateTime LastModifiedDateTime { get; set; }
  } // FileSystemInfoFacet

  public class Audio : FacetBase
  {
    [JsonProperty(PropertyName = "album")]
    public string Album { get; set; }

    [JsonProperty(PropertyName = "albumArtist")]
    public string AlbumArtist { get; set; }

    [JsonProperty(PropertyName = "artist")]
    public string Artist { get; set; }

    [JsonProperty(PropertyName = "bitrate")]
    public int Bitrate { get; set; }

    [JsonProperty(PropertyName = "composers")]
    public string Composers { get; set; }

    [JsonProperty(PropertyName = "copyright")]
    public string Copyright { get; set; }

    [JsonProperty(PropertyName = "disc")]
    public int Disc { get; set; }

    [JsonProperty(PropertyName = "discCount")]
    public int DiscCount { get; set; }

    [JsonProperty(PropertyName = "duration")]
    public long Duration { get; set; }

    [JsonProperty(PropertyName = "genre")]
    public string Genre { get; set; }

    [JsonProperty(PropertyName = "hasDrm")]
    public bool HasDrm { get; set; }

    [JsonProperty(PropertyName = "isVariableBitrate")]
    public bool IsVariableBitrate { get; set; }

    [JsonProperty(PropertyName = "title")]
    public string Title { get; set; }

    [JsonProperty(PropertyName = "track")]
    public int Track { get; set; }

    [JsonProperty(PropertyName = "trackCount")]
    public int TrackCount { get; set; }

    [JsonProperty(PropertyName = "year")]
    public int Year { get; set; }
  } // AudioFacet

  public class Deleted : FacetBase
  {
    [JsonProperty(PropertyName = "state")]
    public string State { get; set; }
  } // DeletedFacet

  public class Image : FacetBase
  {
    [JsonProperty(PropertyName = "width")]
    public int Width { get; set; }

    [JsonProperty(PropertyName = "height")]
    public int Height { get; set; }
  } // ImageFacet

  public class Location : FacetBase
  {
    [JsonProperty(PropertyName = "altitude")]
    public double Altitude { get; set; }

    [JsonProperty(PropertyName = "latitude")]
    public double Latitude { get; set; }

    [JsonProperty(PropertyName = "longitude")]
    public double Longitude { get; set; }
  } // LocationFacet

  public class Package : FacetBase
  {
    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }
  } // PackageFacet

  public class Photo : FacetBase
  {
    [JsonProperty(PropertyName = "takenDateTime")]
    public DateTime TakenDateTime { get; set; }

    [JsonProperty(PropertyName = "cameraMake")]
    public string CameraMake { get; set; }

    [JsonProperty(PropertyName = "cameraModel")]
    public string CameraModel { get; set; }

    [JsonProperty(PropertyName = "fNumber")]
    public double FNumber { get; set; }

    [JsonProperty(PropertyName = "exposureDenominator")]
    public double ExposureDenominator { get; set; }

    [JsonProperty(PropertyName = "exposureNumerator")]
    public double ExposureNumerator { get; set; }

    [JsonProperty(PropertyName = "focalLength")]
    public double FocalLength { get; set; }

    [JsonProperty(PropertyName = "iso")]
    public long Iso { get; set; }
  } // PhotoFacet

  public class Quota : FacetBase
  {
    public enum States
    {
      [EnumMember(Value = "normal")]
      Normal,
      [EnumMember(Value = "nearing")]
      Nearing,
      [EnumMember(Value = "critical")]
      Critical,
      [EnumMember(Value = "exceeded")]
      Exceeded
    } // States 

    [JsonProperty(PropertyName = "total")]
    public long Total { get; set; }

    [JsonProperty(PropertyName = "used")]
    public long Used { get; set; }

    [JsonProperty(PropertyName = "remaining")]
    public long Remaining { get; set; }

    [JsonProperty(PropertyName = "deleted")]
    public long Deleted { get; set; }

    [JsonProperty(PropertyName = "state")]
    public States State { get; set; }
  } // QuotaFacet

  public class RemoteItem : FacetBase
  {
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "parentReference")]
    public Resources.ItemReference ParentReference { get; set; }

    [JsonProperty(PropertyName = "folder")]
    public Folder Folder { get; set; }

    [JsonProperty(PropertyName = "file")]
    public File File { get; set; }

    [JsonProperty(PropertyName = "fileSystemInfo")]
    public FileSystemInfo FileSystemInfo { get; set; }

    [JsonProperty(PropertyName = "size")]
    public long Size { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }
  } // RemoteItemFacet

  public class SearchResult : FacetBase
  {
    [JsonProperty(PropertyName = "onClickTelemetryUrl")]
    public string OnClickTelemetryUrl { get; set; }
  } // SearchResultFacet

  public class Shared : FacetBase
  {
    [JsonProperty(PropertyName = "owner")]
    public Resources.IdentitySet Owner { get; set; }

    [JsonProperty(PropertyName = "scope")]
    public Scopes Scope { get; set; }
  } // SharedFacet

  public class SharepointIds : FacetBase
  {
    [JsonProperty(PropertyName = "siteId")]
    public string SiteId { get; set; }

    [JsonProperty(PropertyName = "webId")]
    public string WebId { get; set; }

    [JsonProperty(PropertyName = "listId")]
    public string ListId { get; set; }

    [JsonProperty(PropertyName = "listItemId")]
    public long ListItemId { get; set; }

    [JsonProperty(PropertyName = "listItemUniqueId")]
    public string ListItemUniqueId { get; set; }
  } // SharepointIdsFacet

  public class SharingInvitation : FacetBase
  {
    [JsonProperty(PropertyName = "email")]
    public string Email { get; set; }

    [JsonProperty(PropertyName = "signInRequired")]
    public bool SignInRequired { get; set; }

    [JsonProperty(PropertyName = "invitedBy")]
    public Resources.IdentitySet InvitedBy { get; set; }
  } // SharingInvitationFacet

  public class SharingLink : FacetBase
  {
    public enum SharingLinkType
    {
      [EnumMember(Value = "view")]
      View,
      [EnumMember(Value = "edit")]
      Edit,
      [EnumMember(Value = "embed")]
      Embed
    } // SharingLinkType

    [JsonProperty(PropertyName = "application")]
    public Resources.Identity Application { get; set; }

    [JsonProperty(PropertyName = "type")]
    public SharingLinkType Type { get; set; }

    [JsonProperty(PropertyName = "scope")]
    public Scopes Scope { get; set; }

    [JsonProperty(PropertyName = "webHtml")]
    public string WebHtml { get; set; }

    [JsonProperty(PropertyName = "webUrl")]
    public string WebUrl { get; set; }
  } // SharingLinkFacet

  public class SpecialFolder : FacetBase
  {
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }
  } // SpecialFolderFacet

  public class Video : FacetBase
  {
    [JsonProperty(PropertyName = "bitrate")]
    public long Bitrate { get; set; }

    [JsonProperty(PropertyName = "duration")]
    public long Duration { get; set; }

    [JsonProperty(PropertyName = "height")]
    public int Height { get; set; }

    [JsonProperty(PropertyName = "width")]
    public int Width { get; set; }
  } // VideoFacet
}
