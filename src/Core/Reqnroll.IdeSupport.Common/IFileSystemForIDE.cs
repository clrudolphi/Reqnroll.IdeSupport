using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;

namespace Reqnroll.IdeSupport.Common
{
    /// <summary>IFileSystemForIDE</summary>
    public interface IFileSystemForIDE : IFileSystem
    {

    }

    /// <summary>FileSystemForIDE</summary>
    public class FileSystemForIDE : IFileSystemForIDE
    {
        private readonly FileSystem _fileSystem = new();

        /// <summary>Gets or sets the file.</summary>
        public IFile File => _fileSystem.File;
        /// <summary>Gets or sets the directory.</summary>
        public IDirectory Directory => _fileSystem.Directory;
        /// <summary>Gets or sets the file info.</summary>
        public IFileInfoFactory FileInfo => _fileSystem.FileInfo;
        /// <summary>Gets or sets the file stream.</summary>
        public IFileStreamFactory FileStream => _fileSystem.FileStream;
        /// <summary>Gets or sets the path.</summary>
        public IPath Path => _fileSystem.Path;
        /// <summary>Gets or sets the directory info.</summary>
        public IDirectoryInfoFactory DirectoryInfo => _fileSystem.DirectoryInfo;
        /// <summary>Gets or sets the drive info.</summary>
        public IDriveInfoFactory DriveInfo => _fileSystem.DriveInfo;
        /// <summary>Gets or sets the file system watcher.</summary>
        public IFileSystemWatcherFactory FileSystemWatcher => _fileSystem.FileSystemWatcher;

        /// <summary>Gets or sets the file version info.</summary>
        public IFileVersionInfoFactory FileVersionInfo => throw new NotImplementedException();
    }
}
