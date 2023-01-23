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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Documents.TreeView.Resources;

/// <summary>
/// Serialized resource element node base class
/// </summary>
public abstract class SerializedResourceElementNode : ResourceElementNode
{
    private object? _deserializedData;

    string? DeserializedStringValue => _deserializedData?.ToString();

    bool IsSerialized => _deserializedData is null;

    /// <inheritdoc/>
    protected override string ValueString => _deserializedData is null
        ? base.ValueString
        : ConvertObjectToString(_deserializedData)!;

    /// <inheritdoc/>
    protected override ImageReference GetIcon() => DsImages.UserDefinedDataType;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="treeNodeGroup">Treenode group</param>
    /// <param name="resourceElement">Resource element</param>
    protected SerializedResourceElementNode(ITreeNodeGroup treeNodeGroup, ResourceElement resourceElement, IDocumentTreeNodeDataContext context)
        : base(treeNodeGroup, resourceElement, context) =>
        Debug.Assert(resourceElement.ResourceData is BinaryResourceData);

    /// <inheritdoc/>
    public override void Initialize() => DeserializeIfPossible();

    void DeserializeIfPossible()
    {
        if (Context.DeserializeResources)
            Deserialize();
    }

    /// <inheritdoc/>
    protected override IEnumerable<ResourceData> GetDeserializedData()
    {
        var dd = _deserializedData;
        var re = ResourceElement;

        if (dd is not null)
            yield return new ResourceData(re.Name, _ => ResourceUtilities.StringToStream(ConvertObjectToString(dd)));
        else
            yield return new ResourceData(re.Name, _ => new MemoryStream(((BinaryResourceData)re.ResourceData).Data));
    }

    /// <summary>
    /// Called after it's been deserialized
    /// </summary>
    protected virtual void OnDeserialized()
    {
    }

    /// <summary>
    /// true if <see cref="Deserialize()"/> can execute
    /// </summary>
    public bool CanDeserialize => IsSerialized;

    /// <summary>
    /// Deserializes the data
    /// </summary>
    public void Deserialize()
    {
        if (!CanDeserialize)
            return;

        var serializedData = ((BinaryResourceData)ResourceElement.ResourceData).Data;
        var formatter = new BinaryFormatter();

        try
        {
#pragma warning disable SYSLIB0011
            _deserializedData = formatter.Deserialize(new MemoryStream(serializedData));
#pragma warning restore SYSLIB0011
        }
        catch
        {
            return;
        }

        if (_deserializedData is null)
            return;

        try
        {
            OnDeserialized();
        }
        catch
        {
            _deserializedData = null;
        }
    }

    string? ConvertObjectToString(object? obj)
    {
        if (obj is null)
            return null;

        return !Context.DeserializeResources
            ? obj.ToString()
            : SerializationUtilities.ConvertObjectToString(obj);
    }

    /// <inheritdoc/>
    public override void UpdateData(ResourceElement newResElem)
    {
        base.UpdateData(newResElem);
        _deserializedData = null;
        DeserializeIfPossible();
    }

    /// <inheritdoc/>
    public override string? ToString(CancellationToken token, bool canDecompile) => IsSerialized ? null : DeserializedStringValue;
}