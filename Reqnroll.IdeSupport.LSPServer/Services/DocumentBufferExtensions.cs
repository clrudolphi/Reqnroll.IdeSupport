using Reqnroll.IdeSupport.LSPServer.Document;
using Reqnroll.IDESupport.LSPServer.Core.Document;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reqnroll.IdeSupport.LSPServer.Services;

public static class DocumentBufferExtensions
{
    public static IGherkinTextSnapshot ToGherkinTextSnapshot(this DocumentBuffer buffer)
            => new LspTextSnapshot(buffer.Uri.ToString(), buffer.Version ?? 0, buffer.Text);
}
