// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.Versioning;
using System.Windows.Markup;

namespace RomsCollect.Helpers;

/// <summary>
/// XAML markup extension resolving a localization key through
/// <see cref="App.Localization"/>, e.g. <c>Text="{loc Menu.Tools}"</c>.
/// Keeps every user-facing string out of the XAML/C# source, per the
/// i18n constraint.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
[SupportedOSPlatform("windows")]
public sealed class LocalizedStringExtension : MarkupExtension
{
    public LocalizedStringExtension()
    {
        Key = string.Empty;
    }

    public LocalizedStringExtension(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) => App.Localization[Key];
}
