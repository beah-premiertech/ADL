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
        internal static string FormatPath(string Path, string Name)
        {
            // Replace commas with backslashes and remove "CN="
            var partialFormatting = Path.Replace(",", @"\").Replace("CN=", string.Empty);

            // Extract the domain components
            var domainComponents = partialFormatting.Split(new[] { @"\DC=" }, StringSplitOptions.None);
            var rawDomain = string.Join(".", domainComponents.Skip(1));

            // Extract the organizational units
            var organizationalUnits = domainComponents[0].Split(new[] { @"\" }, StringSplitOptions.None)
                                                         .Where(part => part.StartsWith("OU="))
                                                         .Select(part => part.Replace("OU=", string.Empty))
                                                         .ToArray().Reverse();

            // Combine the domain components and organizational units
            var finalPath = rawDomain + "\\" + string.Join("\\", organizationalUnits);

            return finalPath;
        }



    }
}
