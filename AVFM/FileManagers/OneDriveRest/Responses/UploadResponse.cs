using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDriveRest.Responses
{
  public class UploadResponse : APIResponse
  {
    public class Range
    {
      public Range()
      {
        Start = 0;
        End = 0;
      }

      public long Start { get; set; }
      public long End { get; set; }
    } // Range

    [JsonProperty(PropertyName = "uploadUrl")]
    public string UploadUrl { get; set; }

    [JsonProperty(PropertyName = "expirationDateTime")]
    public DateTime? ExpirationDateTime { get; set; }

    [JsonProperty(PropertyName = "nextExpectedRanges")]
    public List<string> NextExpectedRanges { get; set; }

    public Range GetRange(int index)
    {
      Range res = null;
      if (NextExpectedRanges.Count > index) {
        string[] parts = NextExpectedRanges[index].Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2) {
          res = new Range();
          long number = 0;
          if (long.TryParse(parts[0], out number))
            res.Start = number;
          if (long.TryParse(parts[1], out number))
            res.End = number;
        }
      }
      return res;
    } // GetRange
    public static UploadResponse Deserialize(string value)
    {
      return APIResponse.Deserialize<UploadResponse>(value);
    } // Deserialize
  } // UploadResponse
}
