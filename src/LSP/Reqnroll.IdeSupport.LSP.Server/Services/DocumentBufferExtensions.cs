using Reqnroll.IdeSupport.LSP.Server.Document;
using Reqnroll.IdeSupport.LSP.Core.Document;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reqnroll.IdeSupport.LSP.Server.Services;

public static class DocumentBufferExtensions
{
    public static IGherkinTextSnapshot ToGherkinTextSnapshot(this DocumentBuffer buffer)
            => new LspTextSnapshot(buffer.Uri.ToString(), buffer.Version ?? 0, buffer.Text);
}
