using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class RecentlyUsedFilesResponse : APIResponse
  {
    [JsonProperty(PropertyName = "value")]
    public List<Resources.Item> Value { get; set; }

    public static RecentlyUsedFilesResponse Deserialize(string value)
    {
      return APIResponse.Deserialize<RecentlyUsedFilesResponse>(value);
    } // Deserialize
  } // RecentlyUsedFilesResponse
}
