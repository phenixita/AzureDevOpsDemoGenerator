using Bunit;
using Microsoft.AspNetCore.Components;
using VstsDemoBuilder.Blazor.Components;
using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Tests.Components;

public sealed class TemplateComponentsTests : TestContext
{
    [Fact]
    public void TemplateCard_Displays_Template_Metadata_And_Truncated_Description()
    {
        var template = new TemplateCatalogItem
        {
            Name = "Adventure Works",
            Description = new string('a', 180),
            ImageUrl = "/Templates/TemplateImages/adventure.png",
            Tags = ["Agile", "Web"]
        };

        var cut = RenderComponent<TemplateCard>(parameters => parameters
            .Add(component => component.Template, template));

        Assert.Contains("Adventure Works", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Agile", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Web", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("img", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("...", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(new string('a', 180), cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TemplateCard_When_Selected_Has_Selected_Visual_State()
    {
        var template = new TemplateCatalogItem
        {
            Name = "Contoso",
            Description = "Template description"
        };

        var cut = RenderComponent<TemplateCard>(parameters => parameters
            .Add(component => component.Template, template)
            .Add(component => component.IsSelected, true));

        var cardButton = cut.Find("button");
        Assert.Contains("template-card-selected", cardButton.ClassList);
    }

    [Fact]
    public void TemplateCard_Click_Invokes_Callback()
    {
        var template = new TemplateCatalogItem
        {
            Name = "Contoso",
            Description = "Template description"
        };

        var wasClicked = false;

        var cut = RenderComponent<TemplateCard>(parameters => parameters
            .Add(component => component.Template, template)
            .Add(component => component.OnClick, EventCallback.Factory.Create(this, () => wasClicked = true)));

        cut.Find("button").Click();

        Assert.True(wasClicked);
    }

    [Fact]
    public void TemplateGroupTabs_Renders_Groups_And_Highlights_Active()
    {
        var groups = new List<string> { "Featured", "Private", "Industry" };

        var cut = RenderComponent<TemplateGroupTabs>(parameters => parameters
            .Add(component => component.GroupNames, groups)
            .Add(component => component.ActiveGroup, "Private"));

        Assert.Contains("Featured", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Private", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Industry", cut.Markup, StringComparison.OrdinalIgnoreCase);

        var activeButton = cut.Find("button[data-group='Private']");
        Assert.Contains("active", activeButton.ClassList);
    }

    [Fact]
    public void TemplateGroupTabs_Click_Invokes_Group_Changed()
    {
        var groups = new List<string> { "Featured", "Private" };
        string? clickedGroup = null;

        var cut = RenderComponent<TemplateGroupTabs>(parameters => parameters
            .Add(component => component.GroupNames, groups)
            .Add(component => component.ActiveGroup, "Featured")
            .Add(component => component.OnGroupChanged, EventCallback.Factory.Create<string>(this, value => clickedGroup = value)));

        cut.Find("button[data-group='Private']").Click();

        Assert.Equal("Private", clickedGroup);
    }

    [Fact]
    public void TemplatePreviewPane_Shows_Placeholder_When_Template_Is_Null()
    {
        var cut = RenderComponent<TemplatePreviewPane>(parameters => parameters
            .Add(component => component.Template, (TemplateCatalogItem?)null));

        Assert.Contains("Select a template", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TemplatePreviewPane_Renders_Full_Details_And_Message_When_Template_Present()
    {
        var template = new TemplateCatalogItem
        {
            Name = "Adventure Works",
            Description = "<p><strong>Full description</strong> with <a href='https://example.com'>details</a>.</p>",
            MessageHtml = "<p>Important setup note.</p>",
            ImageUrl = "/Templates/TemplateImages/adventure.png"
        };

        var cut = RenderComponent<TemplatePreviewPane>(parameters => parameters
            .Add(component => component.Template, template));

        Assert.Contains("Adventure Works", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<strong>Full description</strong>", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Template note", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Important setup note", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
