using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Requests
{
  public abstract class APIRequest
  {
    protected static T Deserialize<T>(string value)
    {
      return Utility.Serialization.Deserialize<T>(value);
    } // Deserialize

    protected static string Serialize<T>(T obj)
    {
      return Utility.Serialization.Serialize(obj);
    } // Serialize
  } // APIRequest
}
