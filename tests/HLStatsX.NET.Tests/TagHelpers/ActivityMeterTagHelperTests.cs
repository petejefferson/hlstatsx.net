using System.Globalization;
using HLStatsX.NET.Web.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HLStatsX.NET.Tests.TagHelpers;

public class ActivityMeterTagHelperTests
{
    private static (TagHelperContext ctx, TagHelperOutput output) Build(string tagName = "activity-meter")
    {
        var ctx = new TagHelperContext(
            tagName,
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());

        var output = new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        return (ctx, output);
    }

    [Fact]
    public void Process_SetsTagNameToMeter()
    {
        var helper = new ActivityMeterTagHelper { Value = 50 };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        output.TagName.Should().Be("meter");
    }

    [Fact]
    public void Process_SetsAllFiveRequiredAttributes()
    {
        var helper = new ActivityMeterTagHelper { Value = 75 };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        output.Attributes["min"].Value.Should().Be("0");
        output.Attributes["max"].Value.Should().Be("100");
        output.Attributes["low"].Value.Should().Be("25");
        output.Attributes["high"].Value.Should().Be("50");
        output.Attributes["optimum"].Value.Should().Be("75");
    }

    [Fact]
    public void Process_SetsValueAsF2FormattedString()
    {
        var helper = new ActivityMeterTagHelper { Value = 33.333 };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        output.Attributes["value"].Value.Should().Be("33.33");
    }

    [Fact]
    public void Process_SetsValueWithInvariantCulture()
    {
        var helper = new ActivityMeterTagHelper { Value = 12.5 };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        // Must use a period, not a comma, regardless of system locale
        output.Attributes["value"].Value.Should().Be(
            (12.5).ToString("F2", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Process_IncludesTitleAttribute_WhenProvided()
    {
        var helper = new ActivityMeterTagHelper { Value = 60, Title = "Kill ratio" };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        output.Attributes["title"].Value.Should().Be("Kill ratio");
    }

    [Fact]
    public void Process_OmitsTitleAttribute_WhenNotProvided()
    {
        var helper = new ActivityMeterTagHelper { Value = 60 };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        output.Attributes.ContainsName("title").Should().BeFalse();
    }

    [Fact]
    public void Process_IncludesStyleAttribute_WhenProvided()
    {
        var helper = new ActivityMeterTagHelper { Value = 40, Style = "width:80px" };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        output.Attributes["style"].Value.Should().Be("width:80px");
    }

    [Fact]
    public void Process_OmitsStyleAttribute_WhenNotProvided()
    {
        var helper = new ActivityMeterTagHelper { Value = 40 };
        var (ctx, output) = Build();

        helper.Process(ctx, output);

        output.Attributes.ContainsName("style").Should().BeFalse();
    }
}
