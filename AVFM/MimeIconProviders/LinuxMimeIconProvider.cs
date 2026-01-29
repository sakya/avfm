using System.Collections.Generic;
using System.Threading.Tasks;


namespace AVFM.MimeIconProviders
{
    public class LinuxMimeIconProvider : MimeIconProviderBase 
    {
        private string m_IconSizeFolder = "16x16";
        private string m_DesktopEnvironment = null;
        private string m_IconTheme = null;

        public LinuxMimeIconProvider(IconSizes size) :
            base(size) 
        {
            switch(size) {
                case IconSizes.Size16x16:
                    m_IconSizeFolder = "16x16";
                    break;
                case IconSizes.Size32x32:
                    m_IconSizeFolder = "32x32";
                    break;                    
            }
        }

        public override async Task<string> GetMimeIcon(string mimeType, string filePath)
        {
            return await Task.Run( () => GetMimeIconPrimitive(mimeType, filePath));
        } // GetMimeIcon  

        private string GetMimeIconPrimitive(string mimeType, string filePath)
        {
            // https://specifications.freedesktop.org/shared-mime-info-spec/shared-mime-info-spec-0.21.html            
            string res = null;
            
            if (mimeType != null) {
                res = GetIconFromCache(mimeType);
                if (res != null)
                    return res;
            }
            
            if (m_DesktopEnvironment == null) {
                var de = Utils.Utils.GetProcessOutput("bash", "-c \"echo $XDG_CURRENT_DESKTOP\"");
                if (de.Contains("GNOME"))
                    m_DesktopEnvironment = "GNOME";
                else if (de.Contains("KDE"))
                    m_DesktopEnvironment = "KDE";
                else if (de.Contains("XFCE"))
                    m_DesktopEnvironment = "XFCE";
            }

            if (m_IconTheme == null) {
                switch (m_DesktopEnvironment) {
                    case "GNOME":
                        m_IconTheme = Utils.Utils.GetProcessOutput("gsettings", "get org.gnome.desktop.interface icon-theme");
                        break;
                    case "KDE":
                        m_IconTheme = Utils.Utils.GetProcessOutput("kreadconfig5", "--group Icons --key Theme");
                        break;
                    case "XFCE":
                        m_IconTheme = Utils.Utils.GetProcessOutput("xfconf-query", "-c xsettings -p /Net/IconThemeName");
                        break;                        
                }
                
                if (!string.IsNullOrEmpty(m_IconTheme)) {
                    m_IconTheme = m_IconTheme.Remove(0, 1);
                    m_IconTheme = m_IconTheme.Remove(m_IconTheme.Length - 1, 1);
                }
            }
                
            if (!string.IsNullOrEmpty(m_IconTheme)) {
                // Search icon name
                var iconName = Utils.Utils.GetProcessOutput("bash", $"-c \"cat /usr/share/mime/icons | grep '{mimeType}:'\"");
                if (string.IsNullOrEmpty(iconName))
                    iconName = Utils.Utils.GetProcessOutput("bash", $"-c \"cat /usr/share/mime/generic-icons | grep '{mimeType}:'\"");
                
                if (!string.IsNullOrEmpty(iconName)) {
                    int idx = iconName.IndexOf(":");
                    if (idx >= 0) {
                        var iconFileName = iconName.Substring(idx + 1);
                        iconFileName = Utils.Utils.GetProcessOutput("bash", $"-c \"find /usr/share/icons/{m_IconTheme}/{m_IconSizeFolder}/ -name '{iconFileName}.png'\"");

                        AddIconToCache(mimeType, iconFileName);
                        res = iconFileName;
                    }
                } else {
                    int idx = mimeType.IndexOf("/");
                    var iconFileName = mimeType.Substring(0, idx);
                    iconFileName = Utils.Utils.GetProcessOutput("bash", $"-c \"find /usr/share/icons/{m_IconTheme}/{m_IconSizeFolder}/ -name '{iconFileName}-x-generic.png'\"");

                    if (!string.IsNullOrEmpty(iconFileName)) {
                        AddIconToCache(mimeType, iconFileName);
                        res = iconFileName;
                    } else {
                        iconFileName = Utils.Utils.GetProcessOutput("bash", $"-c \"find /usr/share/icons/{m_IconTheme}/{m_IconSizeFolder}/ -name 'unknown.png'\"");
                        AddIconToCache(mimeType, iconFileName);
                        res = iconFileName;
                    }
                }
            }

            return res;
        } // GetMimeIconPrimitive
    }
}