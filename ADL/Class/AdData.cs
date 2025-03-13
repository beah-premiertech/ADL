using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;

namespace ADL.Class;


public static class AdDataCollections
{
    public static event EventHandler OnReady;
    public static event EventHandler OuReady;
    public static int ReadyCount = 0;

    public static List<AdNtDomain> NtDomains = new List<AdNtDomain>();
    public static string[] Domains;
    public static List<AdObject> AdObjects = new List<AdObject>();
    public static AdOu AdOU = new AdOu();
    public static List<AdOu> AdOus = new List<AdOu>();

    private static List<AdBaseObject> AdRawOus = new List<AdBaseObject>();
    private static List<Collection<PSObject>> DevicesResult { get; } = new List<Collection<PSObject>>();
    private static List<Collection<PSObject>> UsersResult { get; } = new List<Collection<PSObject>>();
    private static List<Collection<PSObject>> GroupsResult { get; } = new List<Collection<PSObject>>();

    #region Get data
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

            if (Domains == null || Domains.Length < 1)
            {
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

        }

    }

    public static async void GetAllData(string UserDomain, string Password)
    {
        ReadyCount = 0;
        AdObjects.Clear();
        DevicesResult.Clear();
        UsersResult.Clear();
        GroupsResult.Clear();
        AdOus.Clear();
        AdRawOus.Clear();

        await GetAllOus(UserDomain, Password);
        new Thread(() => { GetAllOus(); }).Start();

        await GetAllDevicesAsync(UserDomain, Password);
        GetAllDevices();

        await GetAllUsersAsync(UserDomain, Password);
        GetAllUsers();

        await GetAllGroupsAsync(UserDomain, Password);
        GetAllGroups();
    }
    #endregion

    #region Powershell get data
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

                var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADComputer")
                  .AddParameter("Filter", $"OperatingSystem -notlike \"*Server*\"")
                  .AddParameter("Property", "OperatingSystem")
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

                var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADUser")
                      .AddParameter("Filter", "*")
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

                var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADGroup")
                  .AddParameter("Filter", "*")
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
    private static async Task GetAllOus(string UserDomain, string Password)
    {
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

                var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));

                foreach (var Domain in Domains)
                {
                    AdOU.Children.Add(new AdOu { Name = Domain, Domain = Domain, Path = Domain, FullPath = $"DC={Domain.Replace(".", ",DC=")}", TypeIcon = "\uE774", Type = "Collapsed" });
                    ps.Commands.Clear();
                    ps.AddCommand("Get-ADOrganizationalUnit")
                      .AddParameter("Filter", "Name -ne 'Domain Controllers'")
                      .AddParameter("Server", Domain)
                      .AddParameter("Credential", credentials);

                    var results = ps.Invoke();

                    foreach (var result in results)
                    {
                        var AdName = result.Properties["Name"]?.Value?.ToString();
                        var FormatedPath = AdCommon.FormatPath(result.Properties["DistinguishedName"]?.Value?.ToString(), AdName);

                        var ou = new AdBaseObject
                        {
                            Name = AdName,
                            Path = FormatedPath,
                            Type = "Visible",
                            Domain = FormatedPath.Split('\\').First(),
                            FullPath = result.Properties["DistinguishedName"]?.Value?.ToString(),
                            Tag = "\uE710",
                            TypeIcon = "\uE8B7",
                            TypeColor = "Add to favorites"
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

    #endregion

    #region Parse data
    private static void GetAllDevices()
    {
        foreach (var results in DevicesResult)
        {
            new Thread(() =>
            {
                foreach (var result in results)
                {
                    var AdName = result.Properties["Name"]?.Value?.ToString();
                    var FormatedPath = AdCommon.FormatPath(result.Properties["DistinguishedName"]?.Value?.ToString(), AdName);

                    var device = new AdObject
                    {
                        Name = AdName,
                        Path = FormatedPath,
                        Domain = FormatedPath.Split('\\').First(),
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
    private static void GetAllUsers()
    {
        foreach (var results in UsersResult)
        {
            new Thread(() =>
            {
                foreach (var result in results)
                {
                    var AdName = result.Properties["Name"]?.Value?.ToString();
                    var FormatedPath = AdCommon.FormatPath(result.Properties["DistinguishedName"]?.Value?.ToString(), AdName);

                    var user = new AdObject
                    {
                        Name = AdName,
                        Path = FormatedPath,
                        Domain = FormatedPath.Split('\\').First(),
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
    private static void GetAllGroups()
    {
        foreach (var results in GroupsResult)
        {
            new Thread(() =>
            {
                foreach (var result in results)
                {
                    var AdName = result.Properties["Name"]?.Value?.ToString();
                    var FormatedPath = AdCommon.FormatPath(result.Properties["DistinguishedName"]?.Value?.ToString(),AdName);

                    var ou = new AdBaseObject
                    {
                        Name = AdName,
                        Path = FormatedPath,
                        Type = "Visible",
                        Domain = FormatedPath.Split('\\').First(),
                        FullPath = result.Properties["DistinguishedName"]?.Value?.ToString(),
                        Tag = "\uE710",
                        TypeIcon = "\uE8B7",
                        TypeColor = "Add to favorites"
                    };
                    AdRawOus.Add(ou);
                }
                ReadyCount++;
                TriguerReadyEvent();

            }).Start();
        }
    }
    private static void GetAllOus()
    {
        foreach (var ou in AdRawOus.OrderBy(ad => ad.Path?.Length ?? 0).ToList())
        {
            var AdOu = new AdOu
            {
                Name = ou.Name,
                Path = ou.Path,
                Domain = ou.Domain,
                FullPath = ou.FullPath,
                TypeIcon = ou.TypeIcon,
                Tag = ou.Tag,
                Type = ou.Type,
                TypeColor = ou.TypeColor
            };
            var Parent = AdOU.Children.Find(x => x.Domain.ToLower().Replace(" ", string.Empty) == ou.Domain.ToLower().Replace(" ", string.Empty));
            if (Parent != null)
            {
                var NoDomainPath = ou.Path.Replace(ou.Domain, string.Empty).TrimStart('\\');
                var PathArray = NoDomainPath.Split(@"\").ToArray();
                AppendOu(Parent, PathArray, AdOu);
            }
        }
        AdOus.Add(AdOU);
        AdOus.Add(new AdOu { Name = "Favorites", Domain = "Favorites", FullPath = "Favorites", Path = "Favorites", TypeIcon = "\uE734", Type = "Collapsed" });
        OuReady.Invoke(null, null);
    }

    #endregion

    #region Data management and event handlers
    private static void AppendOu(AdOu parent, string[] pathArray, AdOu adOuToAdd)
    {
        if (pathArray.Length == 0)
            return;

        var currentNodeName = pathArray[0];

        // Find or create the child
        var child = parent.Children.FirstOrDefault(c => c.Name.Equals(currentNodeName, StringComparison.OrdinalIgnoreCase));
        if (child == null)
        {
            // Dynamically create the missing parent node
            var path = $"{currentNodeName}\\{parent.Path}";
            child = new AdOu
            {
                Name = currentNodeName,
                Path = path,
                Domain = parent.Domain,
                FullPath = $"OU={path.Replace("\\",",OU=")},DC={parent.Domain.Replace(".",",DC=")}".Replace($"{parent.Domain},",string.Empty).Replace(",OU=DC=",",DC="),
                TypeIcon = "\uE8B7",
                Type = "Visible",
                TypeColor = "Add to favorites",
                Tag = "\uE710"
            };
            if (child.Name.Length > 2)
            {
                parent.Children.Add(child);
            }
        }

        // Recursively process the remaining path
        if (pathArray.Length > 1)
        {
            AppendOu(child, pathArray.Skip(1).ToArray(), adOuToAdd);
        }
        else
        {
            // Add the final OU to the child
            child.Children.Add(adOuToAdd);
        }
    }

    private static void TriguerReadyEvent()
    {
        if (ReadyCount >= (Domains.Length) * 3)
        {
            ReadyCount = 0;
            OnReady?.Invoke(null, EventArgs.Empty);
        }
    }
    #endregion
}
