#nullable enable
using Reqnroll.IDE.Common;
using System.ComponentModel.Composition;
using System.IO.Abstractions;

namespace Reqnroll.VisualStudio;

// We cannot directly use IFileSystem as dependency (with MEF), because there might be other extensions (e.g. SpecFlow)
// that also export an implementation of IFileSystem. We need to have a separate contract for "our" file system.


[Export(typeof(IFileSystemForIDE))]
public class FileSystemForVs : FileSystemForIDE
{
}
