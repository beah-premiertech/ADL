using System.Diagnostics;
using System.Management.Automation;

namespace ADL.Class;

public static class AdAction
{
    #region Objects management
    public static void Enable(string UserDomain, string Password, AdObject Object)
    {
        var BaseObject = AdDataCollections.AdObjects.Find(x => x.Name == Object.Name);
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
        var BaseObject = AdDataCollections.AdObjects.Find(x => x.Name == Object.Name);
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
        var BaseObject = AdDataCollections.AdObjects.Find(x => x.Name == Object.Name);
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
            AdDataCollections.AdObjects.Remove(BaseObject);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception in GetAllDevices: {ex.Message}");
        }
    }
    public static void Move(string UserDomain, string Password, AdObject Object, string Destination)
    {
        var BaseObject = AdDataCollections.AdObjects.Find(x => x.Name == Object.Name);
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
                ps.Commands.Clear();
                ps.AddCommand("Move-ADObject")
                  .AddParameter("Identity", BaseObject.FullPath)
                  .AddParameter("TargetPath", Destination)
                  .AddParameter("Server", BaseObject.Domain)
                  .AddParameter("Credential", credentials);
                ps.Invoke();
            }
        }
        catch { }
    }
    #endregion

    #region Devices management
    public static string? AddDevice(string UserDomain, string Password, string Domain, string DeviceName, string DevicePath)
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
                return "AD Module import error";
            }

            var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));

            Debug.WriteLine($"Adding device {DeviceName} to {DevicePath} on {Domain}");
            ps.Commands.Clear();
            ps.AddCommand("New-ADComputer")
              .AddParameter("Name", DeviceName)
              .AddParameter("SamAccountName", DeviceName)
              .AddParameter("Path", DevicePath)
              .AddParameter("OperatingSystem", "Windows")
              .AddParameter("Server", Domain)
              .AddParameter("Confirm", false)
              .AddParameter("Credential", credentials)
              .AddParameter("ErrorAction", "Stop");

            try
            {
                ps.Invoke();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return null;


        }
    }
    #endregion

    #region Users management
    public static string ResetPassword(string UserDomain, string Password, AdObject User, string NewPassword, bool ChangeAtNextLogon)
    {
        string Status = string.Empty;
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
                return "AD Module Import Error";
            }

            var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));

            ps.Commands.Clear();
            ps.AddCommand("Set-ADAccountPassword")
              .AddParameter("Identity", User.FullPath)
              .AddParameter("Reset", true)
              .AddParameter("NewPassword", AdCommon.ToSecureString(NewPassword))
              .AddParameter("Server", User.Domain)
              .AddParameter("Confirm", false)
              .AddParameter("Credential", credentials);
            try
            {
                ps.Invoke();
            }
            catch (Exception ex)
            {
                Status = ex.Message;
            }



            if (string.IsNullOrEmpty(Status))
            {
                ps.Commands.Clear();
                ps.AddCommand("Set-ADUser")
                  .AddParameter("Identity", User.FullPath)
                  .AddParameter("ChangePasswordAtLogon", ChangeAtNextLogon)
                  .AddParameter("Server", User.Domain)
                  .AddParameter("Credential", credentials);

                try
                {
                    ps.Invoke();
                }
                catch (Exception ex)
                {
                    Status = ex.Message;
                }
            }
        }
        return Status;
    }
    #endregion

    #region Groups mangement
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

                var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));

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
    public static List<AdBaseObject> Members(string UserDomain, string Password, AdObject Group)
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

                var credentials = new PSCredential(UserDomain, AdCommon.ToSecureString(Password));


                ps.Commands.Clear();
                Debug.WriteLine(Group.FullPath);
                ps.AddCommand("Get-ADGroupMember")
                  .AddParameter("Identity", Group.FullPath)
                  .AddParameter("Server", Group.Domain)
                  .AddParameter("Credential", credentials);

                try
                {
                    var results = ps.Invoke();

                    foreach (var result in results)
                    {
                        var AdName = result.Properties["Name"]?.Value?.ToString();
                        var FormatedPath = AdCommon.FormatPath(result.Properties["DistinguishedName"]?.Value?.ToString(),AdName);

                        var device = new AdBaseObject
                        {
                            Name = AdName,
                            Type = result.Properties["objectClass"]?.Value?.ToString(),
                            Path = FormatedPath,
                            Domain = FormatedPath.Split('\\').First(),
                            FullPath = result.Properties["DistinguishedName"]?.Value?.ToString()
                        };
                        if (device.Type == "user")
                        { device.TypeIcon = "\uE77B"; device.TypeColor = "#29ab85"; }
                        else
                        { device.TypeIcon = "\uEA6C"; device.TypeColor = "#158fd7"; }

                        Members.Add(device);
                    }
                }
                catch (Exception ex) { Debug.WriteLine(ex.Message); }

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
    #endregion

}
