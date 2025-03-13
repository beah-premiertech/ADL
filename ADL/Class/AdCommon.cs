using System.Security;
using System.Xml.Linq;

namespace ADL.Class
{
    internal class AdCommon
    {
        internal static SecureString ToSecureString(string pwd)
        {
            var securePassword = new SecureString();
            foreach (char c in pwd)
            {
                securePassword.AppendChar(c);
            }
            return securePassword;
        }
        internal static string FormatPath(string Path,string Name)
        {
            var PartialFormating = Path.Replace(",", @"\").Replace("CN=", string.Empty).Substring(Name.Length + 1).Replace("OU=", string.Empty);
            var RawDomain = PartialFormating.Replace(PartialFormating.Split(@"\DC=").First(), string.Empty);
            RawDomain = RawDomain.Substring(4);
            PartialFormating = PartialFormating.Split(@"\DC=").First();
            var ArrayPath = $@"{PartialFormating}\{RawDomain.Replace(@"\DC=", ".")}".Split('\\');
            string FinalPath = string.Empty;
            foreach (var Part in ArrayPath.Reverse())
            {
                FinalPath += $"\\{Part}";
            }

            return FinalPath.Substring(1);
        }
    }
}
