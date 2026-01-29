using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class ChildrenResponse : APIResponse
  {
    [JsonProperty(PropertyName = "@odata.nextLink")]
    public string NextLink { get; set; }

    [JsonProperty(PropertyName = "value")]
    public List<Resources.Item> Children { get; set; }

    public static ChildrenResponse Deserialize(string value)
    {
      return APIResponse.Deserialize<ChildrenResponse>(value);
    } // Deserialize
  } // ChildrenResponse
}
