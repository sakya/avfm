using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class ItemThumbnailsResponse : APIResponse
  {
    [JsonProperty(PropertyName = "value")]
    public List<Resources.ThumbnailSet> Thumbnails { get; set; }

    public static ItemThumbnailsResponse Deserialize(string value)
    {
      return APIResponse.Deserialize<ItemThumbnailsResponse>(value);
    } // Deserialize
  } // ItemThumbnailsResponse
}
