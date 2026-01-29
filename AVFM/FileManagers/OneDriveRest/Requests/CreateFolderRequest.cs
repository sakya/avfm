using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Requests
{
  public class CreateFolderRequest : APIRequest
  {
    [JsonProperty(PropertyName = "name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "folder")]
    public Facets.Folder Folder { get; set; }

    public string Serialize()
    {
      return APIRequest.Serialize(this);
    } // Deserialize
  } // CreateFolderRequest
}
