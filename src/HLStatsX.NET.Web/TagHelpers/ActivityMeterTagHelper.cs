using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace HLStatsX.NET.Web.TagHelpers;

[HtmlTargetElement("activity-meter")]
public class ActivityMeterTagHelper : TagHelper
{
    public double Value { get; set; }
    public string? Title { get; set; }
    public string? Style { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "meter";
        output.TagMode = TagMode.StartTagAndEndTag;

        output.Attributes.SetAttribute("min", "0");
        output.Attributes.SetAttribute("max", "100");
        output.Attributes.SetAttribute("low", "25");
        output.Attributes.SetAttribute("high", "50");
        output.Attributes.SetAttribute("optimum", "75");
        output.Attributes.SetAttribute("value", Value.ToString("F2", CultureInfo.InvariantCulture));

        if (Title is not null)
            output.Attributes.SetAttribute("title", Title);
        if (Style is not null)
            output.Attributes.SetAttribute("style", Style);
    }
}
