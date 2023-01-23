using System.Runtime.CompilerServices;

// Needed so it can safely add this asm to the MEF AggregateCatalog without using a string reference (weak reference)
[assembly: InternalsVisibleTo("dnSpy")]
[assembly: InternalsVisibleTo("dnSpy.Roslyn.CSharp.EditorFeatures")]
[assembly: InternalsVisibleTo("dnSpy.Roslyn.VisualBasic.EditorFeatures")]
