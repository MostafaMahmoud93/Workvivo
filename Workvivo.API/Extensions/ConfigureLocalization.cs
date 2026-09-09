using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace Workvivo.API.Extensions;

public static class ConfigureLocalization
{
    /// <summary>
    /// Arabic and English. Arabic uses the Gregorian date format on purpose - the
    /// product shows Gregorian dates in both languages, and ar-AE would otherwise
    /// format some of them as Hijri.
    /// </summary>
    public static readonly CultureInfo[] SupportedCultures =
    [
        new("ar-AE")
        {
            NumberFormat = new NumberFormatInfo { NegativeSign = "-" },
            DateTimeFormat = new CultureInfo("en-US", useUserOverride: false).DateTimeFormat,
        },
        new("en-US"),
    ];

    public static IServiceCollection AddWorkvivoLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.DefaultRequestCulture = new RequestCulture("en-US");
            options.SupportedCultures = SupportedCultures;
            options.SupportedUICultures = SupportedCultures;

            // Accept-Language only. The template also had a hand-rolled middleware
            // doing the same job; the built-in provider already parses quality values
            // correctly, so the custom one is gone.
            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider(),
            ];

            // "ar" from a browser should resolve to ar-AE rather than falling back to
            // the default culture.
            options.FallBackToParentCultures = true;
            options.FallBackToParentUICultures = true;
        });

        return services;
    }
}
