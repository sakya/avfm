using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class ListSharedFilesResponse : APIResponse
  {
    [JsonProperty(PropertyName = "value")]
    public List<Resources.Item> Value { get; set; }

    public static ListSharedFilesResponse Deserialize(string value)
    {
      return APIResponse.Deserialize<ListSharedFilesResponse>(value);
    } // Deserialize
  } // ListSharedFilesResponse
}
