using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Kafo.Web.TagHelpers;

[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (ViewContext.HttpContext.Items["CspNonce"] is string nonce &&
            !string.IsNullOrWhiteSpace(nonce))
        {
            output.Attributes.SetAttribute("nonce", nonce);
        }
    }
}
