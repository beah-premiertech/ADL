using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Security;

namespace ADL;

public class AdBaseObject
{
    public string? Type { get; set; }
    public string? TypeIcon { get; set; }
    public string? TypeColor { get; set; }
    public string? Tag { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public string? FullPath { get; set; }
    public string? Domain { get; set; }
}
public class AdOu : AdBaseObject
{
    public List<AdOu> Children { get; set; } = new List<AdOu>();
}
public class AdObject : AdBaseObject
{
    public bool IsEnable { get; set; }
    public string EnableVisibility => CanBeEnable ? "Visible" : "Collapsed";
    public string DeleteVisibility { get; set; }
    public string ResetPasswordVisibility { get; set; }
    public string MembersVisibility { get; set; }
    public bool CanBeEnable { get; set; }
    public string? IsEnableIcon => IsEnable ? "\uE930" : "\uECE4";
    public string? EnableIcon => IsEnable ? "\uECE4" : "\uE930";
    public string? EnableText => IsEnable ? "Disable" : "Enable";
    public string? IsEnableColor => IsEnable ? "Green" : "Red";
}
public class AdNtDomain
{
    public string Domain { get; set; }
    public string NTDomain { get; set; }
}
public static class AdManager
{
    public static List<AdNtDomain> NtDomains = new List<AdNtDomain>();
    public static string[] Domains;
    public static event EventHandler OnReady;
    public static event EventHandler OuReady;
    public static List<AdObject> AdObjects = new List<AdObject>();
    private static List<AdBaseObject> AdRawOus = new List<AdBaseObject>();
    public static AdOu AdOU = new AdOu();
    public static List<AdOu> AdOus = new List<AdOu>();
    public static int ReadyCount = 0;

    private static List<Collection<PSObject>> DevicesResult = new List<Collection<PSObject>>();
    private static List<Collection<PSObject>> UsersResult = new List<Collection<PSObject>>();
    private static List<Collection<PSObject>> GroupsResult = new List<Collection<PSObject>>();

    public static async Task GetAllDataAsync(string UserDomain, string Password)
    {
        ReadyCount = 0;
        AdObjects.Clear();
        
        await GetAllOus(UserDomain, Password);
        new Thread(() => { GetAllOus(); }).Start();

        await GetAllDevicesAsync(UserDomain, Password);
        GetAllDevices();

        await GetAllUsersAsync(UserDomain, Password);
        GetAllUsers();

        await GetAllGroupsAsync(UserDomain, Password);
        GetAllGroups();
    }
    public static void GetDomains()
    {
        using (PowerShell ps = PowerShell.Create())
        {
            ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
            ps.Invoke();

            if (ps.HadErrors)
            {
                foreach (var error in ps.Streams.Error)
                {
                    Debug.WriteLine($"Error importing module: {error}");
                }
                return;
            }

            ps.Commands.Clear();
            ps.AddCommand("Get-ADForest");


            var results = ps.Invoke();
            foreach (var result in results)
            {
                if (result.Properties["Domains"]?.Value is IEnumerable domains)
                {
                    Domains = domains.Cast<string>().ToArray();
                    break;
                }
            }

            foreach (var Domain in Domains)
            {
                ps.Commands.Clear();
                ps.Commands.AddCommand("Get-ADDomain")
                  .AddParameter("Identity", Domain);

                var results2 = ps.Invoke();

                foreach (var NT in results2)
                {
                    var NtName = NT.Properties["NetBIOSName"]?.Value?.ToString();
                    if (NtName != null && NtName != "ROOT")
                    {
                        NtDomains.Add(new AdNtDomain { Domain = Domain, NTDomain = NtName });
                    }
                    break;
                }
            }
     
            if (ps.HadErrors)
            {
                foreach (var error in ps.Streams.Error)
                {
                    Debug.WriteLine($"Error executing PowerShell command: {error}");
                }
            }
        }
    }

    private static async Task GetAllDevicesAsync(string UserDomain, string Password)
    {
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADComputer")
                      .AddParameter("Filter", "DistinguishedName -notlike \"Domain Control\"")
                      .AddParameter("Server", Domain)
                      .AddParameter("Credential", credentials);

                    DevicesResult.Add(ps.Invoke());

                    if (ps.HadErrors)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            Debug.WriteLine($"Error executing PowerShell command: {error}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }
    }
    private static void GetAllDevices()
    {
        foreach (var results in DevicesResult)
        {
            new Thread(() =>
            {
                foreach (var result in results)
                {
                    var AdName = result.Properties["Name"]?.Value?.ToString();
                    var PartialFormating = result.Properties["DistinguishedName"]?.Value?.ToString().Replace(",", @"\").Replace("CN=", string.Empty).Substring(AdName.Length + 1).Replace("OU=", string.Empty);
                    var RawDomain = PartialFormating.Replace(PartialFormating.Split(@"\DC=").First(), string.Empty);
                    RawDomain = RawDomain.Substring(4);
                    PartialFormating = PartialFormating.Split(@"\DC=").First();
                    var FormatedPath = $@"{PartialFormating}\{RawDomain.Replace(@"\DC=", ".")}";

                    var device = new AdObject
                    {
                        Name = AdName,
                        Path = FormatedPath,
                        Domain = FormatedPath.Split('\\').Last(),
                        Type = "Device",
                        TypeIcon = "\uEA6C",
                        DeleteVisibility = "Visible",
                        ResetPasswordVisibility = "Collapsed",
                        MembersVisibility = "Collapsed",
                        TypeColor = "#158fd7",
                        IsEnable = bool.Parse(result.Properties["Enabled"]?.Value.ToString() ?? "false"),
                        CanBeEnable = true,
                        FullPath = result.Properties["DistinguishedName"]?.Value?.ToString()
                    };

                    AdObjects.Add(device);
                }
                ReadyCount++;
                TriguerReadyEvent();
            }).Start();
        }
    }
    
    private static async Task GetAllUsersAsync(string UserDomain, string Password)
    {
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADUser")
                      .AddParameter("Filter", "DistinguishedName -notlike \"Domain Control\"")
                      .AddParameter("Server", Domain)
                      .AddParameter("Credential", credentials);

                    UsersResult.Add(ps.Invoke());

                    if (ps.HadErrors)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            Debug.WriteLine($"Error executing PowerShell command: {error}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllUsers: {ex.Message}");
        }

    }
    private static void GetAllUsers()
    {
        foreach (var results in UsersResult)
        {
            new Thread(() =>
            {
                foreach (var result in results)
                {
                    var AdName = result.Properties["Name"]?.Value?.ToString();
                    var PartialFormating = result.Properties["DistinguishedName"]?.Value?.ToString().Replace(",", @"\").Replace("CN=", string.Empty).Substring(AdName.Length + 1).Replace("OU=", string.Empty);
                    var RawDomain = PartialFormating.Replace(PartialFormating.Split(@"\DC=").First(), string.Empty);
                    RawDomain = RawDomain.Substring(4);
                    PartialFormating = PartialFormating.Split(@"\DC=").First();
                    var FormatedPath = $@"{PartialFormating}\{RawDomain.Replace(@"\DC=", ".")}";

                    var user = new AdObject
                    {
                        Name = AdName,
                        Path = FormatedPath,
                        Domain = FormatedPath.Split('\\').Last(),
                        Type = "User",
                        TypeIcon = "\uE77B",
                        TypeColor = "#29ab85",
                        DeleteVisibility = "Collapsed",
                        ResetPasswordVisibility = "Visible",
                        MembersVisibility = "Collapsed",
                        IsEnable = bool.Parse(result.Properties["Enabled"]?.Value.ToString() ?? "false"),
                        CanBeEnable = true,
                        FullPath = result.Properties["DistinguishedName"]?.Value?.ToString()
                    };

                    AdObjects.Add(user);
                }
                ReadyCount++;
                TriguerReadyEvent();

            }).Start();
        }
    }

    private static async Task GetAllGroupsAsync(string UserDomain, string Password)
    {
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADGroup")
                  .AddParameter("Filter", "DistinguishedName -notlike \"Domain Control\"")
                  .AddParameter("Server", Domain)
                  .AddParameter("Credential", credentials);

                    GroupsResult.Add(ps.Invoke());

                    if (ps.HadErrors)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            Debug.WriteLine($"Error executing PowerShell command: {error}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllUsers: {ex.Message}");
        }

    }
    private static void GetAllGroups()
    {
        foreach (var results in GroupsResult)
        {
            new Thread(() =>
            {
                foreach (var result in results)
                {
                    var AdName = result.Properties["Name"]?.Value?.ToString();
                    var PartialFormating = result.Properties["DistinguishedName"]?.Value?.ToString().Replace(",", @"\").Replace("CN=", string.Empty).Substring(AdName.Length + 1).Replace("OU=", string.Empty);
                    var RawDomain = PartialFormating.Replace(PartialFormating.Split(@"\DC=").First(), string.Empty);
                    RawDomain = RawDomain.Substring(4);
                    PartialFormating = PartialFormating.Split(@"\DC=").First();
                    var FormatedPath = $@"{PartialFormating}\{RawDomain.Replace(@"\DC=", ".")}";

                    var group = new AdObject
                    {
                        Name = AdName,
                        Path = FormatedPath,
                        Domain = FormatedPath.Split('\\').Last(),
                        Type = "Group",
                        TypeIcon = "\uE902",
                        TypeColor = "#9250eb",
                        DeleteVisibility = "Collapsed",
                        ResetPasswordVisibility = "Collapsed",
                        MembersVisibility = "Visible",
                        CanBeEnable = false,
                        FullPath = result.Properties["DistinguishedName"]?.Value?.ToString()
                    };
                    AdObjects.Add(group);
                }
                ReadyCount++;
                TriguerReadyEvent();

            }).Start();
        }
    }

    private static async Task GetAllOus(string UserDomain, string Password)
    {
        AdRawOus.Clear();
        AdOus.Clear();
        AdOU = new AdOu { Name = "All", Domain = "All", FullPath = "All", Path = "All", TypeIcon = "\uE8A9", Type = "Collapsed" };

        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    AdOU.Children.Add(new AdOu { Name = Domain, Domain = Domain, Path = Domain, FullPath = $"DC={Domain.Replace(".", ",DC=")}", TypeIcon = "\uE774", Type = "Collapsed" });
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADOrganizationalUnit")
                      .AddParameter("Filter", "DistinguishedName -notlike \"Domain Control\"")
                      .AddParameter("Server", Domain)
                      .AddParameter("Credential", credentials);

                    var results = ps.Invoke();

                    foreach (var result in results)
                    {
                        var AdName = result.Properties["Name"]?.Value?.ToString();
                        var PartialFormating = result.Properties["DistinguishedName"]?.Value?.ToString().Replace(",", @"\").Replace("CN=", string.Empty).Substring(AdName.Length + 1).Replace("OU=", string.Empty);
                        var RawDomain = PartialFormating.Replace(PartialFormating.Split(@"\DC=").First(), string.Empty);
                        RawDomain = RawDomain.Substring(4);
                        PartialFormating = PartialFormating.Split(@"\DC=").First();
                        var FormatedPath = $@"{PartialFormating}\{RawDomain.Replace(@"\DC=", ".")}";

                        var ou = new AdBaseObject
                        {
                            Name = AdName,
                            Path = FormatedPath,
                            Type = "Visible",
                            Domain = FormatedPath.Split('\\').Last(),
                            FullPath = result.Properties["DistinguishedName"]?.Value?.ToString(),
                            TypeIcon = "\uE8B7"
                        };
                        AdRawOus.Add(ou);
                    }

                    if (ps.HadErrors)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            Debug.WriteLine($"Error executing PowerShell command: {error}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }

    }
    private static void GetAllOus()
    {
        foreach (var ou in AdRawOus)
        {
            var AdOu = new AdOu
            {
                Name = ou.Name,
                Path = ou.Path,
                Domain = ou.Domain,
                FullPath = ou.FullPath,
                TypeIcon = ou.TypeIcon,
                Tag = "\uE710",
                Type = ou.Type,
                TypeColor = "Add to favorites"
            };
            var Parent = AdOU.Children.Find(x => x.Domain.ToLower().Replace(" ", string.Empty) == ou.Domain.ToLower().Replace(" ", string.Empty));
            if (Parent != null)
            {
                var NoDomainPath = ou.Path.Replace(ou.Domain, string.Empty).TrimEnd('\\');
                var PathArray = NoDomainPath.Split(@"\").Reverse().ToArray();
                AppendOu(Parent, PathArray, AdOu);
            }
        }
        AdOus.Add(AdOU);
        AdOus.Add(new AdOu { Name = "Favorites", Domain = "Favorites", FullPath = "Favorites", Path = "Favorites", TypeIcon = "\uE734", Type = "Collapsed" });
        OuReady.Invoke(null, null);
    }
    private static void AppendOu(AdOu Parent, string[] PathArray, AdOu AdOu)
    {
        if (PathArray.Length == 1)
        {
            Parent.Children.Add(AdOu);
        }
        else
        {
            var Child = Parent.Children.Find(x => x.Name == PathArray[0]);
            if (Child == null)
            {
                Child = new AdOu { Name = PathArray[0] };
                Parent.Children.Add(Child);
            }
            var NextOu = PathArray.Skip(1).ToArray();
            AppendOu(Child, NextOu, AdOu);
        }
    }

    public static void Enable(string UserDomain, string Password, AdObject Object)
    {
        var BaseObject = AdObjects.Find(x => x.Name == Object.Name);
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                ps.Commands.Clear();
                ps.AddCommand("Enable-ADAccount")
                  .AddParameter("Identity", BaseObject.FullPath)
                  .AddParameter("Server", BaseObject.Domain)
                  .AddParameter("Credential", credentials);

                ps.Invoke();


                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error executing PowerShell command: {error}");
                    }
                }
            }
            BaseObject.IsEnable = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }
    }
    public static void Disable(string UserDomain, string Password, AdObject Object)
    {
        var BaseObject = AdObjects.Find(x => x.Name == Object.Name);
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                ps.Commands.Clear();
                ps.AddCommand("Disable-ADAccount")
                  .AddParameter("Identity", BaseObject.FullPath)
                  .AddParameter("Server", BaseObject.Domain)
                  .AddParameter("Credential", credentials);

                ps.Invoke();


                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error executing PowerShell command: {error}");
                    }
                }
            }
            BaseObject.IsEnable = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }
    }
    public static void Delete(string UserDomain, string Password, AdObject Object)
    {
        var BaseObject = AdObjects.Find(x => x.Name == Object.Name);
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                ps.Commands.Clear();
                ps.AddCommand("Remove-ADObject")
                  .AddParameter("Identity", BaseObject.FullPath)
                  .AddParameter("Server", BaseObject.Domain)
                  .AddParameter("Confirm", false)
                  .AddParameter("Credential", credentials);

                ps.Invoke();


                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error executing PowerShell command: {error}");
                    }
                }
            }
            AdObjects.Remove(BaseObject);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }
    }
    public static void AddRemoveMembers(string UserDomain, string Password, AdObject Object, string ResourcePath, bool Remove = false)
    {
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));

                ps.Commands.Clear();
                ps.AddCommand(Remove ? "Remove-ADGroupMember" : "Add-ADGroupMember")
                  .AddParameter("Identity", Object.FullPath)
                  .AddParameter("Members", ResourcePath)
                  .AddParameter("Server", Object.Domain)
                  .AddParameter("Confirm", false)
                  .AddParameter("Credential", credentials);

                ps.Invoke();


                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error executing PowerShell command: {error}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }
    }

    public static List<AdBaseObject> Members(string UserDomain, string Password, AdObject Object)
    {
        List<AdBaseObject> Members = new List<AdBaseObject>();
        try
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("Import-Module ActiveDirectory -ErrorAction Stop");
                ps.Invoke();

                if (ps.HadErrors)
                {
                    foreach (var error in ps.Streams.Error)
                    {
                        Debug.WriteLine($"Error importing module: {error}");
                    }
                    return null;
                }

                var credentials = new PSCredential(UserDomain, ToSecureString(Password));


                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADGroupMember")
                      .AddParameter("Identity", Object.FullPath)
                      .AddParameter("Server", Object.Domain)
                      .AddParameter("Credential", credentials);

                    var results = ps.Invoke();

                    foreach (var result in results)
                    {
                        var AdName = result.Properties["Name"]?.Value?.ToString();
                        var PartialFormating = result.Properties["DistinguishedName"]?.Value?.ToString().Replace(",", @"\").Replace("CN=", string.Empty).Substring(AdName.Length + 1).Replace("OU=", string.Empty);
                        var RawDomain = PartialFormating.Replace(PartialFormating.Split(@"\DC=").First(), string.Empty);
                        RawDomain = RawDomain.Substring(4);
                        PartialFormating = PartialFormating.Split(@"\DC=").First();
                        var FormatedPath = $@"{PartialFormating}\{RawDomain.Replace(@"\DC=", ".")}";

                        var device = new AdBaseObject
                        {
                            Name = AdName,
                            Type = result.Properties["objectClass"]?.Value?.ToString(),
                            Path = FormatedPath,
                            Domain = FormatedPath.Split('\\').Last(),
                            FullPath = result.Properties["DistinguishedName"]?.Value?.ToString()
                        };
                    if (device.Type == "user")
                    { device.TypeIcon = "\uE77B"; device.TypeColor = "#29ab85"; }
                    else
                    { device.TypeIcon = "\uEA6C"; device.TypeColor = "#158fd7"; }

                    Members.Add(device);
                    }

                    if (ps.HadErrors)
                    {
                        foreach (var error in ps.Streams.Error)
                        {
                            Debug.WriteLine($"Error executing PowerShell command: {error}");
                        }
                    }
                }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }
        return Members;
    }
    private static void TriguerReadyEvent()
    {
        if (ReadyCount >= ((Domains.Length - 1) * 3))
        {
            ReadyCount = 0;
            OnReady?.Invoke(null, EventArgs.Empty);
        }
    }

    private static SecureString ToSecureString(string pwd)
    {
        var securePassword = new SecureString();
        foreach (char c in pwd)
        {
            securePassword.AppendChar(c);
        }
        return securePassword;
    }
}
