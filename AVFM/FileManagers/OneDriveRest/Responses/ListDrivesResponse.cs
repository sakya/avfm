using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class ListDrivesResponse : APIResponse
  {
    [JsonProperty(PropertyName = "value")]
    public List<Resources.Drive> Value { get; set; }

    public static ListDrivesResponse Deserialize(string value)
    {
      return APIResponse.Deserialize<ListDrivesResponse>(value);
    } // Deserialize
  } // ListDrivesResponse
}
