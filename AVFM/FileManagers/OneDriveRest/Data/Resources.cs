using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Resources
{
  public abstract class ResourceBase
  {
    [JsonProperty(PropertyName = "@odata.type")]
    public string DataType { get; set; }

    protected static T Deserialize<T>(string value)
    {
      if (string.IsNullOrEmpty(value))
        return default(T);
      return Utility.Serialization.Deserialize<T>(value);
    } // Deserialize

    protected static string Serialize<T>(T obj)
    {
      return Utility.Serialization.Serialize(obj);
    } // Serialize
  } // ResourceBase

  public class Delta : ResourceBase
  {
    [JsonProperty(PropertyName = "@odata.nextLink")]
    public string NextLink { get; set; }

    [JsonProperty(PropertyName = "@odata.deltaLink")]
    public string DeltaLink { get; set; }

    [JsonProperty(PropertyName = "@delta.token")]
    public string Token { get; set; }

    [JsonProperty(PropertyName = "value")]
    public List<Item> Value { get; set; }
  } // Delta

  public class Drive : ResourceBase
  {
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "driveType")]
    public string DriveType { get; set; }

    [JsonProperty(PropertyName = "owner")]
    public IdentitySet Owner { get; set; }

    [JsonProperty(PropertyName = "quota")]
    public Facets.Quota Quota { get; set; }

    public static Drive Deserialize(string value)
    {
      return Deserialize<Drive>(value);
    } // Deserialize
  } // Drive

  public class Error : ResourceBase
  {
    public enum Codes
    {
      None,

      [EnumMember(Value = "accessDenied")]
      AccessDenied,
      [EnumMember(Value = "activityLimitReached")]
      ActivityLimitReached,
      [EnumMember(Value = "generalException")]
      GeneralException,
      [EnumMember(Value = "invalidRange")]
      InvalidRange,
      [EnumMember(Value = "invalidRequest")]
      InvalidRequest,
      [EnumMember(Value = "itemNotFound")]
      ItemNotFound,
      [EnumMember(Value = "malwareDetected")]
      MalwareDetected,
      [EnumMember(Value = "nameAlreadyExists")]
      NameAlreadyExists,
      [EnumMember(Value = "notAllowed")]
      NotAllowed,
      [EnumMember(Value = "notSupported")]
      NotSupported,
      [EnumMember(Value = "resourceModified")]
      ResourceModified,
      [EnumMember(Value = "resyncRequired")]
      ResyncRequired,
      [EnumMember(Value = "serviceNotAvailable")]
      ServiceNotAvailable,
      [EnumMember(Value = "quotaLimitReached")]
      QuotaLimitReached,
      [EnumMember(Value = "unauthenticated")]
      Unauthenticated,
    } // Codes

    [JsonProperty(PropertyName = "code")]
    public string Code { get; set; }

    [JsonIgnore]
    public Codes? KnownCode
    {
      get
      {
        Codes res;
        if (Enum.TryParse<Codes>(Code, out res))
          return res;
        return null;
      }
    }

    [JsonProperty(PropertyName = "message")]
    public string Message { get; set; }

    [JsonProperty(PropertyName = "innererror")]
    public Error Innererror { get; set; }

    public static Error Deserialize(string value)
    {
      return Deserialize<Error>(value);
    } // Deserialize
  } // Error

  public class Identity : ResourceBase
  {
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "displayName")]
    public string DisplayName { get; set; }

    [JsonProperty(PropertyName = "thumbnails")]
    public ThumbnailSet Thumbnails { get; set; }
  } // Identity

  public class IdentitySet : ResourceBase
  {
    [JsonProperty(PropertyName = "user")]
    public Identity User { get; set; }

    [JsonProperty(PropertyName = "application")]
    public Identity Application { get; set; }

    [JsonProperty(PropertyName = "device")]
    public Identity Device { get; set; }


    [JsonProperty(PropertyName = "organization")]
    public Identity Organization { get; set; }
  } // IdentitySet

  public class Item : ResourceBase
  {
    [JsonProperty(PropertyName = "@content.downloadUrl")]
    public string DownloadUrl { get; set; }

    [JsonProperty(PropertyName = "@content.sourceUrl")]
    public string SourceUrl { get; set; }

    [JsonProperty(PropertyName = "@content.conflictBehavior")]
    public string ConflictBehavior { get; set; }

    [JsonProperty(PropertyName = "createdBy")]
    public IdentitySet CreatedBy { get; set; }

    [JsonProperty(PropertyName = "createdDateTime")]
    public DateTime? CreatedDateTime { get; set; }

    [JsonProperty(PropertyName = "cTag")]
    public string CTag { get; set; }

    [JsonProperty(PropertyName = "eTag")]
    public string ETag { get; set; }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "lastModifiedBy")]
    public IdentitySet LastModifiedBy { get; set; }

    [JsonProperty(PropertyName = "lastModifiedDateTime")]
    public DateTime? LastModifiedDateTime { get; set; }

    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "size")]
    public long? Size { get; set; }

    [JsonProperty(PropertyName = "image")]
    public Image Image { get; set; }

    [JsonProperty(PropertyName = "audio")]
    public Facets.Audio Audio { get; set; }

    [JsonProperty(PropertyName = "file")]
    public Facets.File File { get; set; }

    [JsonProperty(PropertyName = "webUrl")]
    public string WebUrl { get; set; }

    [JsonProperty(PropertyName = "parentReference")]
    public ItemReference ParentReference { get; set; }

    [JsonProperty(PropertyName = "folder")]
    public Folder Folder { get; set; }

    [JsonProperty(PropertyName = "specialFolder")]
    public Facets.SpecialFolder SpecialFolder { get; set; }

    public static Item Deserialize(string value)
    {
      return Deserialize<Item>(value);
    } // Deserialize

    public string Serialize()
    {
      return Serialize(this);
    } // Serialize
  } // Item

  public class ItemReference : ResourceBase
  {
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "driveId")]
    public string DriveId { get; set; }

    [JsonProperty(PropertyName = "path")]
    public string Path { get; set; }
  } // ItemReference

  public class Permission : ResourceBase
  {
    public enum Roles
    {
      [EnumMember(Value = "read")]
      Read,
      [EnumMember(Value = "write")]
      Write,
      [EnumMember(Value = "sp.owner")]
      SpOwner,
      [EnumMember(Value = "sp.member")]
      SpMember
    } // Roles

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "role")]
    public Roles Role { get; set; }

    [JsonProperty(PropertyName = "link")]
    public Facets.SharingLink Link { get; set; }

    [JsonProperty(PropertyName = "grantedTo")]
    public IdentitySet GrantedTo { get; set; }

    [JsonProperty(PropertyName = "invitation")]
    public Facets.SharingInvitation Invitation { get; set; }

    [JsonProperty(PropertyName = "inheritedFrom")]
    public ItemReference InheritedFrom { get; set; }

    [JsonProperty(PropertyName = "shareId")]
    public string ShareId { get; set; }

    public static Permission Deserialize(string value)
    {
      return Deserialize<Permission>(value);
    } // Deserialize

  } // Permission

  public class Folder : ResourceBase
  {
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "childCount")]
    public long ChildCount { get; set; }
  } // Folder

  public class Quota : ResourceBase
  {
    [JsonProperty(PropertyName = "state")]
    public string State { get; set; }

    [JsonProperty(PropertyName = "deleted")]
    public long Deleted { get; set; }

    [JsonProperty(PropertyName = "remaining")]
    public long Remaining { get; set; }

    [JsonProperty(PropertyName = "total")]
    public long Total { get; set; }

    [JsonProperty(PropertyName = "used")]
    public long Used { get; set; }
  } // Quota

  public class Link : ResourceBase
  {
    [JsonProperty(PropertyName = "type")]
    public string Type { get; set; }

    [JsonProperty(PropertyName = "webUrl")]
    public string WebUrl { get; set; }

    [JsonProperty(PropertyName = "application")]
    public Application Application { get; set; }
  } // Link

  public class Thumbnail : ResourceBase
  {
    [JsonProperty(PropertyName = "height")]
    public int Height { get; set; }

    [JsonProperty(PropertyName = "width")]
    public int Width { get; set; }

    [JsonProperty(PropertyName = "url")]
    public string Url { get; set; }
  } // Thumbnail

  public class ThumbnailSet : ResourceBase
  {
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    [JsonProperty(PropertyName = "small")]
    public Thumbnail Small { get; set; }

    [JsonProperty(PropertyName = "medium")]
    public Thumbnail Medium { get; set; }

    [JsonProperty(PropertyName = "large")]
    public Thumbnail Large { get; set; }

    [JsonProperty(PropertyName = "smallSquare")]
    public Thumbnail SmallSquare { get; set; }

    [JsonProperty(PropertyName = "mediumSquare")]
    public Thumbnail MediumSquare { get; set; }

    [JsonProperty(PropertyName = "largeSquare")]
    public Thumbnail LargeSquare { get; set; }
  } // ThumbnailSet

  public class User : ResourceBase
  {
    [JsonProperty(PropertyName = "displayName")]
    public string DisplayName { get; set; }

    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
  } // User

  public class Application : User
  {

  } // Application

  public class Image : ResourceBase
  {
    [JsonProperty(PropertyName = "height")]
    public int Height { get; set; }

    [JsonProperty(PropertyName = "width")]
    public int Width { get; set; }
  } // Image

  public class Owner : ResourceBase
  {
    [JsonProperty(PropertyName = "user")]
    public User User { get; set; }
  } // Owner

  public class AsyncOperationStatus : ResourceBase
  {
    public enum OperationStatus
    {
      [EnumMember(Value = "notStarted")]
      NotStarted,
      [EnumMember(Value = "inProgress")]
      InProgress,
      [EnumMember(Value = "completed")]
      Completed,
      [EnumMember(Value = "updating")]
      Updating,
      [EnumMember(Value = "failed")]
      Failed,
      [EnumMember(Value = "deletePending")]
      DeletePending,
      [EnumMember(Value = "deleteFailed")]
      DeleteFailed,
      [EnumMember(Value = "waiting")]
      Waiting,
    }

    [JsonProperty(PropertyName = "operation")]
    public string Operation { get; set; }

    [JsonProperty(PropertyName = "percentageComplete")]
    public double PercentageComplete { get; set; }

    [JsonProperty(PropertyName = "status")]
    public OperationStatus Status { get; set; }

    public static AsyncOperationStatus Deserialize(string value)
    {
      return Deserialize<AsyncOperationStatus>(value);
    } // Deserialize
  } // AsyncOperationStatus
}
