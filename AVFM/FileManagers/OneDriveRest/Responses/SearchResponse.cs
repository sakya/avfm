using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class SearchResponse : APIResponse
  {
    [JsonProperty(PropertyName = "@odata.approximateCount")]
    public int ApproximateCount { get; set; }

    [JsonProperty(PropertyName = "@odata.nextLink")]
    public string NextLink { get; set; }

    [JsonProperty(PropertyName = "value")]
    public List<Resources.Item> Items { get; set; }

    public static SearchResponse Deserialize(string value)
    {
      return APIResponse.Deserialize<SearchResponse>(value);
    } // Deserialize
  } // SearchResponse
}
