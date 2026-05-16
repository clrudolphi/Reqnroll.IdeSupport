# Copilot Instructions

## Project Guidelines
- When debugging VS MEF CompositionFailedException where the Errors collection is empty, check %LOCALAPPDATA%\Microsoft\VisualStudio\{version}\ComponentModelCache\Microsoft.VisualStudio.Default.err — it contains the full composition error details including which specific imports/exports failed.