using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace WS.WEB.Shared.Field;

public class FieldBase : ComponentBase
{
    [Parameter] public string? Name { get; set; }
    [Parameter] public string? Description { get; set; }
    [Parameter] public string? Tooltip { get; set; }

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }

    [Parameter] public string? Icon { get; set; }
    [Parameter] public string? Image { get; set; }
    [Parameter] public string? ImageStyle { get; set; }

    [Parameter] public Size Size { get; set; } = Size.Medium;

    [Parameter] public EventCallback OnClick { get; set; }
}