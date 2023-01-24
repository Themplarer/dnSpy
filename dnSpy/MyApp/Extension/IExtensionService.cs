using System.Collections.Generic;
using dnSpy.Contracts.Extension;
using dnSpy.Extension;

namespace MyApp.Extension;

interface IExtensionService
{
    IEnumerable<IExtension> Extensions { get; }

    IEnumerable<LoadedExtension> LoadedExtensions { get; }
}