using Microsoft.AspNetCore.Components;
using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Components;

public partial class TemplateSelectionModal
{
    private string? _activeGroupName;

    [Parameter]
    public bool IsOpen { get; set; }

    [Parameter]
    public IReadOnlyList<TemplateCatalogGroup> Groups { get; set; } = [];

    [Parameter]
    public TemplateCatalogItem? PendingSelection { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback<TemplateCatalogItem> OnTemplateClicked { get; set; }

    [Parameter]
    public EventCallback<TemplateCatalogItem> OnConfirm { get; set; }

    private IReadOnlyList<TemplateCatalogItem> VisibleTemplates
    {
        get
        {
            if (Groups.Count == 0)
            {
                return [];
            }

            var active = Groups.FirstOrDefault(group => string.Equals(group.GroupName, _activeGroupName, StringComparison.OrdinalIgnoreCase));
            return active?.Templates ?? [];
        }
    }

    private TemplateCatalogItem? PreviewTemplate => PendingSelection ?? VisibleTemplates.FirstOrDefault();

    protected override void OnParametersSet()
    {
        if (Groups.Count == 0)
        {
            _activeGroupName = null;
            return;
        }

        if (PendingSelection is not null)
        {
            var selectedGroup = Groups.FirstOrDefault(group =>
                group.Templates.Any(template => string.Equals(template.TemplateFolder, PendingSelection.TemplateFolder, StringComparison.OrdinalIgnoreCase)));

            if (selectedGroup is not null)
            {
                _activeGroupName = selectedGroup.GroupName;
                return;
            }
        }

        var hasActiveGroup = Groups.Any(group => string.Equals(group.GroupName, _activeGroupName, StringComparison.OrdinalIgnoreCase));
        if (!hasActiveGroup)
        {
            _activeGroupName = Groups[0].GroupName;
        }
    }

    private bool IsActiveGroup(TemplateCatalogGroup group)
    {
        return string.Equals(group.GroupName, _activeGroupName, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSelected(TemplateCatalogItem template)
    {
        return PendingSelection is not null &&
               string.Equals(PendingSelection.TemplateFolder, template.TemplateFolder, StringComparison.OrdinalIgnoreCase);
    }

    private void SelectGroup(string groupName)
    {
        _activeGroupName = groupName;
    }

    private Task HandleCloseAsync()
    {
        return OnClose.InvokeAsync();
    }

    private Task HandleTemplateClickedAsync(TemplateCatalogItem template)
    {
        return OnTemplateClicked.InvokeAsync(template);
    }

    private Task HandleConfirmAsync()
    {
        if (PendingSelection is null)
        {
            return Task.CompletedTask;
        }

        return OnConfirm.InvokeAsync(PendingSelection);
    }
}
