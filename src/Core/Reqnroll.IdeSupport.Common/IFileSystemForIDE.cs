using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;

namespace Reqnroll.IdeSupport.Common
{
    public interface IFileSystemForIDE : IFileSystem
    {

    }

    public class FileSystemForIDE : IFileSystemForIDE
    {
        private readonly FileSystem _fileSystem = new();

        public IFile File => _fileSystem.File;
        public IDirectory Directory => _fileSystem.Directory;
        public IFileInfoFactory FileInfo => _fileSystem.FileInfo;
        public IFileStreamFactory FileStream => _fileSystem.FileStream;
        public IPath Path => _fileSystem.Path;
        public IDirectoryInfoFactory DirectoryInfo => _fileSystem.DirectoryInfo;
        public IDriveInfoFactory DriveInfo => _fileSystem.DriveInfo;
        public IFileSystemWatcherFactory FileSystemWatcher => _fileSystem.FileSystemWatcher;

        public IFileVersionInfoFactory FileVersionInfo => throw new NotImplementedException();
    }
}
