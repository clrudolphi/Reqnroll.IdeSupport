using Reqnroll.IDE.Common.Diagnostics;
using System.Collections;
using System.ComponentModel.Composition;

namespace Reqnroll.VisualStudio.Diagnostics;

[Export(typeof(IDeveroomLogger))]
[Export(typeof(DeveroomCompositeLogger))]
public class DeveroomCompositeLogger : Reqnroll.IDE.Common.Diagnostics.DeveroomCompositeLogger
{
  
}