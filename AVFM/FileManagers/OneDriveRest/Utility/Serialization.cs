using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Utility
{
  static class Serialization
  {
    public static string Serialize<T>(T obj)
    {
      if (obj == null)
        return string.Empty;
#if DEBUG
      Formatting opt = Formatting.Indented;
#else
            Formatting opt = Formatting.None;
#endif

      string res = JsonConvert.SerializeObject(obj, opt,
          new JsonSerializerSettings() {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>() { new Newtonsoft.Json.Converters.StringEnumConverter() }
          });
      return res;
    } // Serialize

    public static T Deserialize<T>(string value)
    {
      return JsonConvert.DeserializeObject<T>(value);
    } // Deserialize
  }
}
