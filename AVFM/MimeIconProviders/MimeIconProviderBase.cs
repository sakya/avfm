using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace AVFM.MimeIconProviders
{
    public abstract class MimeIconProviderBase 
    {
        public enum IconSizes
        {
            None,
            Size16x16,
            Size32x32
        }

        protected IconSizes m_IconSize = IconSizes.None;
        private Mutex m_IconsCacheMutex = new Mutex();
        private Dictionary<string, string> m_IconsCache = new Dictionary<string, string>();

        public MimeIconProviderBase(IconSizes size) 
        {
            m_IconSize = size;
        }

        protected string GetIconFromCache(string mimeType)
        {
            m_IconsCacheMutex.WaitOne();
            string res;
            if (m_IconsCache.TryGetValue(mimeType, out res)) {
                m_IconsCacheMutex.ReleaseMutex();
                return res;
            }
            m_IconsCacheMutex.ReleaseMutex();
            return null;
        } // GetIconFromCache

        protected void AddIconToCache(string mimeType, string icon)
        {
            m_IconsCacheMutex.WaitOne();
            m_IconsCache[mimeType] = icon;
            m_IconsCacheMutex.ReleaseMutex();
        } // AddIconToCache

        public abstract Task<string> GetMimeIcon(string mimeType, string filePath);
    } // MimeIconProviderBase

    public static class MimeIconProviderFactory 
    {
        public static MimeIconProviderBase GetMimeIconProvider(MimeIconProviderBase.IconSizes iconSize)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                return new LinuxMimeIconProvider(iconSize);
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                return new WindowsMimeIconProvider(iconSize);

            return null;
        } // GetMimeIconProvider
    } // MimeIconProviderFactory
}