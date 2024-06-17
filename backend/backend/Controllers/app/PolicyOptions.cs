namespace backend.Controllers.app;

/// <summary>
/// Provides predefined policy options for Vault capabilities.
/// </summary>
public class PolicyOptions
{
    /// <summary>
    /// Represents a policy with all capabilities including sudo.
    /// </summary>
    public static readonly string SudoPolicy =
        "capabilities = [\"create\", \"read\", \"update\", \"delete\", \"list\", \"sudo\"]";

    /// <summary>
    /// Represents a policy with create, read, update, delete, and list capabilities.
    /// </summary>
    public static readonly string CrudPolicy =
        "capabilities = [\"create\", \"read\", \"update\", \"delete\", \"list\"]";

    /// <summary>
    /// Represents a policy with read-only capability.
    /// </summary>
    public static readonly string ReadOnlyPolicy = "capabilities = [\"read\"]";
}
