/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Metadata;

sealed class ModuleIdProvider : IModuleIdProvider
{
    private readonly Lazy<IModuleIdFactoryProvider, IModuleIdFactoryProviderMetadata>[] _moduleIdFactoryProviders;
    private readonly ConditionalWeakTable<ModuleDef, StrongBox<ModuleId>> _moduleDictionary;
    private readonly ConditionalWeakTable<ModuleDef, StrongBox<ModuleId>>.CreateValueCallback _callbackCreateCore;
    private IModuleIdFactory[]? _factories;

    public ModuleIdProvider(IEnumerable<Lazy<IModuleIdFactoryProvider, IModuleIdFactoryProviderMetadata>> moduleIdFactoryProviders)
    {
        _moduleIdFactoryProviders = moduleIdFactoryProviders.OrderBy(a => a.Metadata.Order).ToArray();
        _moduleDictionary = new ConditionalWeakTable<ModuleDef, StrongBox<ModuleId>>();
        _callbackCreateCore = CreateCore;
    }

    public ModuleId Create(ModuleDef? module)
    {
        if (module is null)
            return new ModuleId();

        var res = _moduleDictionary.GetValue(module, _callbackCreateCore).Value;
        // Don't cache dynamic modules. The reason is that their ModuleIds could change,
        // see CorDebug's DbgEngineImpl.UpdateDynamicModuleIds()
        return res.IsDynamic
            ? CreateCore(module).Value
            : res;
    }

    StrongBox<ModuleId> CreateCore(ModuleDef module)
    {
        _factories ??= _moduleIdFactoryProviders
            .Select(provider => provider.Value.Create())
            .Where(factory => factory is not null)
            .ToArray()!;

        foreach (var factory in _factories)
        {
            var id = factory.Create(module);

            if (id is not null)
                return new StrongBox<ModuleId>(id.Value);
        }

        return new StrongBox<ModuleId>(ModuleId.CreateFromFile(module));
    }
}