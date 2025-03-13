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
public class AdOu : AdBaseObject
{
    public List<AdOu> Children { get; set; } = new List<AdOu>();
}
public class AdNtDomain
{
    public string Domain { get; set; }
    public string NTDomain { get; set; }
}
