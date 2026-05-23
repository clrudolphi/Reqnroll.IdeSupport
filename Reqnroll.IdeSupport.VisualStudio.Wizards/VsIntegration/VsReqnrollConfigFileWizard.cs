// VsIntegration layer
using Reqnroll.VisualStudio.Wizards.Core;

namespace Reqnroll.VisualStudio.Wizards.VsIntegration;

/// <summary>
/// Ported from VsReqnrollConfigFileWizard.
/// </summary>
public class VsReqnrollConfigFileWizard : VsTemplateWizardBase<ConfigFileTemplateWizard>
{
    protected override ConfigFileTemplateWizard ResolveWizard(EnvDTE.DTE dte) => new();
}
