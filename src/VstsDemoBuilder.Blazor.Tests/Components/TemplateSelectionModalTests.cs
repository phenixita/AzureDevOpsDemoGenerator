using Bunit;
using Microsoft.AspNetCore.Components;
using VstsDemoBuilder.Blazor.Components;
using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Tests.Components;

public class TemplateSelectionModalTests : TestContext
{
    [Fact]
    public void Should_Not_Render_When_Closed()
    {
        var cut = RenderComponent<TemplateSelectionModal>(parameters => parameters
            .Add(x => x.IsOpen, false)
            .Add(x => x.Groups, CreateGroups()));

        Assert.Empty(cut.FindAll(".template-selection-modal-backdrop"));
    }

    [Fact]
    public void Should_Render_Header_And_Disabled_Confirm_Without_Pending_Selection()
    {
        var cut = RenderComponent<TemplateSelectionModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.Groups, CreateGroups())
            .Add(x => x.PendingSelection, null));

        Assert.Contains("Choose template", cut.Markup, StringComparison.OrdinalIgnoreCase);

        var confirmButton = cut.Find("button[data-testid='confirm-template']");
        Assert.True(confirmButton.HasAttribute("disabled"));
    }

    [Fact]
    public void Should_Invoke_OnClose_When_Backdrop_Clicked()
    {
        var closeCalls = 0;

        var cut = RenderComponent<TemplateSelectionModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.Groups, CreateGroups())
            .Add(x => x.OnClose, EventCallback.Factory.Create(this, () => closeCalls++)));

        cut.Find(".template-selection-modal-backdrop").Click();

        Assert.Equal(1, closeCalls);
    }

    [Fact]
    public void Should_Invoke_OnTemplateClicked_When_Template_Card_Clicked()
    {
        TemplateCatalogItem? clicked = null;

        var groups = CreateGroups();

        var cut = RenderComponent<TemplateSelectionModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.Groups, groups)
            .Add(x => x.OnTemplateClicked, EventCallback.Factory.Create<TemplateCatalogItem>(this, item => clicked = item)));

        cut.Find("button[data-template-folder='template-a']").Click();

        Assert.NotNull(clicked);
        Assert.Equal("template-a", clicked!.TemplateFolder);
    }

    [Fact]
    public void Should_Invoke_OnConfirm_When_Selection_Is_Pending()
    {
        TemplateCatalogItem? confirmed = null;
        var pending = CreateGroups().First().Templates.First();

        var cut = RenderComponent<TemplateSelectionModal>(parameters => parameters
            .Add(x => x.IsOpen, true)
            .Add(x => x.Groups, CreateGroups())
            .Add(x => x.PendingSelection, pending)
            .Add(x => x.OnConfirm, EventCallback.Factory.Create<TemplateCatalogItem>(this, item => confirmed = item)));

        cut.Find("button[data-testid='confirm-template']").Click();

        Assert.NotNull(confirmed);
        Assert.Equal(pending.TemplateFolder, confirmed!.TemplateFolder);
    }

    private static IReadOnlyList<TemplateCatalogGroup> CreateGroups()
    {
        return
        [
            new TemplateCatalogGroup
            {
                GroupName = "Popular",
                Templates =
                [
                    new TemplateCatalogItem
                    {
                        Name = "Template A",
                        TemplateFolder = "template-a",
                        Description = "A template"
                    },
                    new TemplateCatalogItem
                    {
                        Name = "Template B",
                        TemplateFolder = "template-b",
                        Description = "B template"
                    }
                ]
            },
            new TemplateCatalogGroup
            {
                GroupName = "Engineering",
                Templates =
                [
                    new TemplateCatalogItem
                    {
                        Name = "Template C",
                        TemplateFolder = "template-c",
                        Description = "C template"
                    }
                ]
            }
        ];
    }
}
