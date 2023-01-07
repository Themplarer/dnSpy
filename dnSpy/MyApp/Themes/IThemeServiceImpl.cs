using System.Collections.Generic;
using dnSpy.Contracts.Themes;

namespace MyApp.Themes;

internal interface IThemeServiceImpl : IThemeService
{
    IEnumerable<ITheme> AllThemes { get; }

    IEnumerable<ITheme> VisibleThemes { get; }

    new ITheme Theme { get; set; }
}