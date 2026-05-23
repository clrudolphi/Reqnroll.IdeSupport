// VsIntegration layer
using Reqnroll.VisualStudio.Wizards.Core;

namespace Reqnroll.VisualStudio.Wizards.VsIntegration;

/// <summary>
/// Ported from VsFeatureFileWizard. Resolves without MEF since
/// FeatureFileTemplateWizard has no constructor dependencies.
/// </summary>
public class VsFeatureFileWizard : VsSimulatedItemAddWizardBase<FeatureFileTemplateWizard>
{
    protected override FeatureFileTemplateWizard ResolveWizard(EnvDTE.DTE dte) => new();
}
