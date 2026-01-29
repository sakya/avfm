using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OneDriveRest.Utility
{
  class ResponseCache
  {
    SemaphoreSlim m_Semaphore = new SemaphoreSlim(1);
    List<string> m_Keys = null;
    Dictionary<string, string> m_Cache = null;

    public ResponseCache(int capacity)
    {
      if (capacity < 0)
        capacity = 100;
      Capacity = capacity;
      m_Keys = new List<string>(Capacity);
      m_Cache = new Dictionary<string, string>(Capacity);
    }

    public int Capacity
    {
      get;
      private set;
    }

    public async Task<bool> Add(string url, string response)
    {
      await m_Semaphore.WaitAsync();

      while (m_Keys.Count >= Capacity) {
        string k = m_Keys[0];
        m_Cache.Remove(k);
        m_Keys.RemoveAt(0);
      }

      m_Keys.Add(url);
      m_Cache[url] = response;

      m_Semaphore.Release();
      return true;
    }

    public async Task<string> Get(string url)
    {
      await m_Semaphore.WaitAsync();
      string res = string.Empty;
      if (m_Cache.TryGetValue(url, out res)) {
        m_Keys.Remove(url);
        m_Keys.Add(url);
      }
      m_Semaphore.Release();

      return res;
    }

    public async Task<bool> Clear()
    {
      await m_Semaphore.WaitAsync();
      m_Keys.Clear();
      m_Cache.Clear();
      m_Semaphore.Release();
      return true;
    }
  } // ResponseCache
}
