namespace backend.Controllers.example;

public class PolicyOptions
{
    public static readonly string SudoPolicy =
        "capabilities = [\"create\", \"read\", \"update\", \"delete\", \"list\", \"sudo\"]";

    public static readonly string
        CRUDPolicy = "capabilities = [\"create\", \"read\", \"update\", \"delete\", \"list\"]";

    public static readonly string ReadOnlyPolicy = "capabilities = [\"read\"]";
}