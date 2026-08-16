using Microsoft.AspNetCore.Components;

namespace WS.WEB.Shared.Field;

public class FieldBase : ComponentBase
{
    [Parameter] public string? Name { get; set; }
    [Parameter] public string? Description { get; set; }
    //[Parameter] public string? Placeholder { get; set; }

    //[Parameter] public string? CssIcon { get; set; }
    //[Parameter] public string? CssClass { get; set; }
    //[Parameter] public string? Style { get; set; }

    //[Parameter] public bool Disabled { get; set; }
    //[Parameter] public bool ReadOnly { get; set; }
    //[Parameter] public bool Required { get; set; }
    //[Parameter] public bool Visible { get; set; } = true;

    //[Parameter] public string? CustomInfo { get; set; }
    //[Parameter] public string? CustomWarning { get; set; }
}
