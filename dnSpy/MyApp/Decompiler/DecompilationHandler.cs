using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace MyApp.Views;

public class DecompilationHandler
{
    private readonly IDecompiler _decompiler;
    private readonly IDecompilerOutput _output;
    private readonly DecompilationContext _decompilationContext;

    public DecompilationHandler(IDecompiler decompiler, IDecompilerOutput output, DecompilationContext decompilationContext)
    {
        _decompiler = decompiler;
        _output = output;
        _decompilationContext = decompilationContext;
    }

    public void Decompile(TreeNodeData treeNodeData)
    {
        switch (treeNodeData)
        {
            // case NodeType.Unknown:
            //     DecompileUnknown(node);
            //     break;

            case AssemblyDocumentNode assemblyDocumentNode:
                _decompiler.Decompile(assemblyDocumentNode.Document.AssemblyDef!, _output, _decompilationContext);
                break;

            case ModuleDocumentNode moduleDocumentNode:
                _decompiler.Decompile(moduleDocumentNode.Document.ModuleDef!, _output, _decompilationContext);
                break;

            case TypeNode typeNode:
                _decompiler.Decompile(typeNode.TypeDef, _output, _decompilationContext);
                break;

            case MethodNode methodNode:
                _decompiler.Decompile(methodNode.MethodDef, _output, _decompilationContext);
                break;

            case FieldNode fieldNode:
                _decompiler.Decompile(fieldNode.FieldDef, _output, _decompilationContext);
                break;

            case PropertyNode propertyNode:
                _decompiler.Decompile(propertyNode.PropertyDef, _output, _decompilationContext);
                break;

            case EventNode eventNode:
                _decompiler.Decompile(eventNode.EventDef, _output, _decompilationContext);
                break;

            case AssemblyReferenceNode assemblyReferenceNode:
                Decompile(assemblyReferenceNode);
                break;

            case BaseTypeFolderNode baseTypeFolderNode:
                Decompile(baseTypeFolderNode);
                break;

            case BaseTypeNode baseTypeNode:
                Decompile(baseTypeNode);
                break;

            case DerivedTypeNode derivedTypeNode:
                Decompile(derivedTypeNode);
                break;

            case DerivedTypesFolderNode derivedTypesFolderNode:
                Decompile(derivedTypesFolderNode);
                break;

            case ModuleReferenceNode moduleReferenceNode:
                Decompile(moduleReferenceNode);
                break;

            case NamespaceNode namespaceNode:
                Decompile(namespaceNode);
                break;

            case PEDocumentNode peDocumentNode:
                Decompile(peDocumentNode);
                break;

            case ReferencesFolderNode referencesFolderNode:
                Decompile(referencesFolderNode);
                break;

            case ResourcesFolderNode resourcesFolderNode:
                Decompile(resourcesFolderNode);
                break;

            case ResourceNode resourceNode:
                Decompile(resourceNode);
                break;

            case ResourceElementNode resourceElementNode:
                Decompile(resourceElementNode);
                break;

            // case ResourceElementSetNode resourceElementSetNode:
            //     Decompile(resourceElementSetNode);
            //     break;

            case UnknownDocumentNode unknownDocumentNode:
                Decompile(unknownDocumentNode);
                break;

            case MessageNode messageNode:
                Decompile(messageNode);
                break;

            default:
                Debug.Fail("Unknown NodeType");
                break;
            // goto case NodeType.Unknown;
        }
    }

    void Decompile(AssemblyReferenceNode node) => _decompiler.WriteCommentLine(_output, NameUtilities.CleanName(node.AssemblyRef.ToString()!));

    void Decompile(BaseTypeFolderNode node)
    {
        foreach (var child in GetChildren(node).OfType<BaseTypeNode>())
            Decompile(child);
    }

    void Decompile(BaseTypeNode node) => _decompiler.WriteCommentLine(_output, NameUtilities.CleanName(node.TypeDefOrRef.ReflectionFullName));

    void Decompile(DerivedTypeNode node) => _decompiler.WriteCommentLine(_output, NameUtilities.CleanName(node.TypeDef.ReflectionFullName));

    void Decompile(DerivedTypesFolderNode node)
    {
        foreach (var child in GetChildren(node).OfType<DerivedTypeNode>())
            Decompile(child);
    }

    void Decompile(ModuleReferenceNode node) => _decompiler.WriteCommentLine(_output, NameUtilities.CleanName(node.ModuleRef.ToString()!));

    void Decompile(NamespaceNode node)
    {
        var children = GetChildren(node).OfType<TypeNode>().Select(a => a.TypeDef).ToArray();
        _decompiler.DecompileNamespace(node.Name, children, _output, _decompilationContext);
    }

    void Decompile(PEDocumentNode node)
    {
        _decompiler.WriteCommentLine(_output, node.Document.Filename);
        var peImage = node.Document.PEImage;

        if (peImage is not null)
        {
            var timestampLine = "Timestamp: ";
            uint ts = peImage.ImageNTHeaders.FileHeader.TimeDateStamp;

            if ((int)ts > 0)
            {
                var date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(ts).ToLocalTime();
                var dateString = date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat);
                timestampLine += $"{ts:X8} ({dateString})";
            }
            else
                timestampLine += $"&lt;Unknown&gt; ({ts:X8})";

            _decompiler.WriteCommentLine(_output, timestampLine);
        }
    }

    void Decompile(ReferencesFolderNode node)
    {
        foreach (var child in GetChildren(node))
        {
            if (child is AssemblyReferenceNode)
                Decompile((AssemblyReferenceNode)child);
            else if (child is ModuleReferenceNode)
                Decompile((ModuleReferenceNode)child);
            else
                DecompileUnknown(child);
        }
    }

    void Decompile(ResourcesFolderNode node)
    {
        foreach (var child in GetChildren(node))
        {
            if (child is ResourceNode)
                Decompile((ResourceNode)child);
            else
                Decompile(child);
        }
    }

    void Decompile(ResourceNode node)
    {
        if (node is ResourceElementSetNode)
            Decompile((ResourceElementSetNode)node);
        else
            node.WriteShort(_output, _decompiler, _decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));
    }

    void Decompile(ResourceElementNode node) =>
        node.WriteShort(_output, _decompiler, _decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));

    void Decompile(ResourceElementSetNode node)
    {
        node.WriteShort(_output, _decompiler, _decompiler.Settings.GetBoolean(DecompilerOptionConstants.ShowTokenAndRvaComments_GUID));

        foreach (var child in GetChildren(node))
        {
            if (child is ResourceElementNode)
                Decompile((ResourceElementNode)child);
            else
                Decompile(child);
        }
    }

    void Decompile(UnknownDocumentNode node) => _decompiler.WriteCommentLine(_output, node.Document.Filename);

    void Decompile(MessageNode node) => _decompiler.WriteCommentLine(_output, node.Message);

    void DecompileUnknown(DocumentTreeNodeData node)
    {
        // if (node is IDecompileSelf decompileSelf && _decompileNodeContext is not null)
        // {
        //     if (decompileSelf.Decompile(_decompileNodeContext))
        //         return;
        // }

        _decompiler.WriteCommentLine(_output, NameUtilities.CleanName(node.ToString(_decompiler)));
    }

    DocumentTreeNodeData[] GetChildren(DocumentTreeNodeData node)
    {
        if (node.TreeNode is null) return Array.Empty<DocumentTreeNodeData>();

        node.TreeNode.EnsureChildrenLoaded();
        return node.TreeNode.DataChildren.OfType<DocumentTreeNodeData>().ToArray();
    }
}