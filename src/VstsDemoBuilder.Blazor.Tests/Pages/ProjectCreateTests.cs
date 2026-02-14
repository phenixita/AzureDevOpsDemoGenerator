using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VstsDemoBuilder.Blazor.Components.Pages;
using VstsDemoBuilder.Blazor.Models;
using VstsDemoBuilder.Blazor.Services;
using VstsDemoBuilder.Blazor.Session;

namespace VstsDemoBuilder.Blazor.Tests;

public class ProjectCreateTests : TestContext
{
    private readonly Mock<ITemplateCatalogService> _mockCatalogService;
    private readonly Mock<IProvisioningService> _mockProvisioningService;
    private readonly HttpContextAccessor _httpContextAccessor;

    public ProjectCreateTests()
    {
        _mockCatalogService = new Mock<ITemplateCatalogService>();
        _mockProvisioningService = new Mock<IProvisioningService>();
        _httpContextAccessor = new HttpContextAccessor();

        Services.AddSingleton(_mockCatalogService.Object);
        Services.AddSingleton(_mockProvisioningService.Object);
        Services.AddSingleton<IHttpContextAccessor>(_httpContextAccessor);

        _mockCatalogService
            .Setup(x => x.GetTemplateGroupsAsync())
            .ReturnsAsync(new List<TemplateCatalogGroup>());

        _mockCatalogService
            .Setup(x => x.GetAllTemplatesAsync())
            .ReturnsAsync(new List<TemplateCatalogItem>());

        _mockCatalogService
            .Setup(x => x.GetTemplateParametersAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<TemplateParameterDefinition>());
    }

    [Fact]
    public void Should_Display_Required_Form_Fields_On_Load()
    {
        SetSession();

        var cut = RenderComponent<ProjectCreate>();

        Assert.NotNull(cut.Find("#template"));
        Assert.NotNull(cut.Find("#projectName"));
        Assert.NotNull(cut.Find("#organization"));
        Assert.NotNull(cut.Find("button[type='submit']"));
    }

    [Fact]
    public void Should_Open_Template_Modal_When_Choose_Template_Clicked()
    {
        SetSession();
        SeedTemplates(new TemplateCatalogItem { Name = "Template A", TemplateFolder = "template-a" });

        var cut = RenderComponent<ProjectCreate>();

        OpenTemplateModal(cut);

        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll(".template-selection-modal-backdrop"));
            Assert.Contains("Choose template", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Should_Block_Submission_When_Organization_Not_Selected()
    {
        SetSession();
        SeedTemplates(new TemplateCatalogItem { Name = "Template A", TemplateFolder = "template-a" });

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "template-a");
        cut.Find("#projectName").Change("Test Project");

        cut.Find("form").Submit();

        _mockProvisioningService.Verify(
            x => x.StartProjectProvisioningAsync(
                It.IsAny<ProjectCreateFormModel>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        cut.WaitForAssertion(() =>
            Assert.Contains("Please select an organization", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Show_Validation_Error_For_Invalid_Project_Name()
    {
        SetSession();
        SeedTemplates(new TemplateCatalogItem { Name = "Template A", TemplateFolder = "template-a" });

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "template-a");
        cut.Find("#organization").Change("org-alpha");
        cut.Find("#projectName").Change("CON");

        cut.Find("form").Submit();

        _mockProvisioningService.Verify(
            x => x.StartProjectProvisioningAsync(
                It.IsAny<ProjectCreateFormModel>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        cut.WaitForAssertion(() =>
            Assert.Contains("reserved name", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Display_Dynamic_Parameters_When_Template_Selected()
    {
        SetSession();

        var template = new TemplateCatalogItem
        {
            Name = "Test Template",
            TemplateFolder = "test-template"
        };

        SeedTemplates(template);

        _mockCatalogService
            .Setup(x => x.GetTemplateParametersAsync("test-template"))
            .ReturnsAsync(new List<TemplateParameterDefinition>
            {
                new() { FieldName = "ApiKey", Label = "API Key" },
                new() { FieldName = "Region", Label = "Region" }
            });

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "test-template");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Template Parameters", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("API Key", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Region", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Should_Update_Template_Summary_After_Confirmation()
    {
        SetSession();

        var selectedTemplate = new TemplateCatalogItem
        {
            Name = "Architecture Hub",
            TemplateFolder = "architecture-hub",
            Description = "Architecture planning and review workflows",
            ImageUrl = "https://example.org/template-architecture.png"
        };

        SeedTemplates(selectedTemplate);

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "architecture-hub");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Architecture Hub", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Architecture planning and review workflows", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("https://example.org/template-architecture.png", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Should_Display_Info_Message_When_Template_Has_Message_Html()
    {
        SetSession();

        var templateWithMessage = new TemplateCatalogItem
        {
            Name = "Contoso Secure",
            TemplateFolder = "contoso-secure",
            MessageHtml = "<strong>Important:</strong> Install required extensions first."
        };

        SeedTemplates(templateWithMessage);

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "contoso-secure");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Important:", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Install required extensions first", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(cut.FindAll(".alert.alert-info"));
        });
    }

    [Fact]
    public void Should_Start_Provisioning_And_Display_Tracking_Id()
    {
        SetSession();
        SeedTemplates(new TemplateCatalogItem { Name = "Valid Template", TemplateFolder = "valid-template" });

        var trackingId = "test-123";
        _mockProvisioningService
            .Setup(x => x.StartProjectProvisioningAsync(
                It.IsAny<ProjectCreateFormModel>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(trackingId);

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "valid-template");
        cut.Find("#projectName").Change("Valid Project Name");
        cut.Find("#organization").Change("org-alpha");

        cut.Find("form").Submit();

        _mockProvisioningService.Verify(
            x => x.StartProjectProvisioningAsync(
                It.Is<ProjectCreateFormModel>(model =>
                    model.ProjectName == "Valid Project Name" &&
                    model.TemplateFolder == "valid-template" &&
                    model.Organization == "org-alpha"),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(trackingId, cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Provisioning Started Successfully", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Should_Show_Error_When_Session_Expired()
    {
        SetSession(accessToken: string.Empty);
        SeedTemplates(new TemplateCatalogItem { Name = "Valid Template", TemplateFolder = "valid-template" });

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "valid-template");
        cut.Find("#projectName").Change("Valid Project Name");
        cut.Find("#organization").Change("org-alpha");

        cut.Find("form").Submit();

        _mockProvisioningService.Verify(
            x => x.StartProjectProvisioningAsync(
                It.IsAny<ProjectCreateFormModel>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);

        cut.WaitForAssertion(() =>
            Assert.Contains("Session expired", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Should_Clear_Previously_Entered_Parameter_Values_When_Confirming_Different_Template()
    {
        SetSession();
        SeedTemplates(
            new TemplateCatalogItem { Name = "Template A", TemplateFolder = "template-a" },
            new TemplateCatalogItem { Name = "Template B", TemplateFolder = "template-b" });

        _mockCatalogService
            .Setup(x => x.GetTemplateParametersAsync("template-a"))
            .ReturnsAsync(new List<TemplateParameterDefinition>
            {
                new() { FieldName = "ApiKey", Label = "API Key" }
            });

        _mockCatalogService
            .Setup(x => x.GetTemplateParametersAsync("template-b"))
            .ReturnsAsync(new List<TemplateParameterDefinition>
            {
                new() { FieldName = "Region", Label = "Region" }
            });

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "template-a");
        cut.WaitForAssertion(() => Assert.Contains("API Key", cut.Markup, StringComparison.OrdinalIgnoreCase));

        var apiKeyInput = cut.FindAll("input[type='text']").Last();
        apiKeyInput.Change("secret-value-from-template-a");

        ConfirmTemplateSelection(cut, "template-b");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Region", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("API Key", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret-value-from-template-a", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void Should_Not_Change_Confirmed_Template_When_Modal_Closed_Without_Confirm()
    {
        SetSession();
        SeedTemplates(
            new TemplateCatalogItem { Name = "Template A", TemplateFolder = "template-a", Description = "A description" },
            new TemplateCatalogItem { Name = "Template B", TemplateFolder = "template-b", Description = "B description" });

        var cut = RenderComponent<ProjectCreate>();

        ConfirmTemplateSelection(cut, "template-a");

        OpenTemplateModal(cut);
        SelectTemplateInModal(cut, "template-b");
        cut.Find("button.template-selection-modal-close").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Template A", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("A description", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("B description", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(cut.FindAll(".template-selection-modal-backdrop"));
        });
    }

    private void SeedTemplates(params TemplateCatalogItem[] templates)
    {
        var templateList = templates.ToList();

        _mockCatalogService
            .Setup(x => x.GetTemplateGroupsAsync())
            .ReturnsAsync(new List<TemplateCatalogGroup>
            {
                new()
                {
                    GroupName = "Default",
                    Templates = templateList
                }
            });

        _mockCatalogService
            .Setup(x => x.GetAllTemplatesAsync())
            .ReturnsAsync(templateList);
    }

    private static void OpenTemplateModal(IRenderedComponent<ProjectCreate> cut)
    {
        cut.Find("#template").Click();
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll(".template-selection-modal-backdrop")));
    }

    private static void SelectTemplateInModal(IRenderedComponent<ProjectCreate> cut, string templateFolder)
    {
        cut.Find($"button[data-template-folder='{templateFolder}']").Click();
    }

    private static void ConfirmTemplate(IRenderedComponent<ProjectCreate> cut)
    {
        cut.Find("button[data-testid='confirm-template']").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".template-selection-modal-backdrop")));
    }

    private static void ConfirmTemplateSelection(IRenderedComponent<ProjectCreate> cut, string templateFolder)
    {
        OpenTemplateModal(cut);
        SelectTemplateInModal(cut, templateFolder);
        ConfirmTemplate(cut);
    }

    private void SetSession(
        string accessToken = "token-123",
        string email = "test@example.com",
        string displayName = "Test User",
        IReadOnlyList<string>? organizations = null)
    {
        var httpContext = new DefaultHttpContext();
        var session = new TestSession();

        httpContext.Session = session;
        session.SetString(SessionKeys.AccessToken, accessToken);
        session.SetString(SessionKeys.Email, email);
        session.SetString(SessionKeys.DisplayName, displayName);
        session.SetStringList(SessionKeys.Organizations, organizations ?? new List<string> { "org-alpha" });

        _httpContextAccessor.HttpContext = httpContext;
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public IEnumerable<string> Keys => _store.Keys;

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value!);
        }

        public void Set(string key, byte[] value)
        {
            _store[key] = value;
        }

        public void Remove(string key)
        {
            _store.Remove(key);
        }

        public void Clear()
        {
            _store.Clear();
        }
    }
}
