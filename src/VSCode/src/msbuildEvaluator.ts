import * as fs from 'fs';
import * as path from 'path';
import { execFile } from 'child_process';

/**
 * Result of evaluating a .csproj via dotnet msbuild.
 */
export interface ProjectProperties {
  readonly outputAssemblyPath: string;
  readonly targetFrameworkMoniker: string;
  readonly defaultNamespace: string;
  readonly packageReferences: readonly PackageRef[];
}

export interface PackageRef {
  readonly packageId: string;
  readonly version: string;
}

/**
 * Evaluates a .csproj using `dotnet msbuild -getProperty` and reads
 * `project.assets.json` for package references.
 *
 * Returns `null` when `dotnet` is unavailable or evaluation fails
 * (caller falls back to v1 folder-prefix behaviour).
 */
export async function evaluateProject(projectFile: string): Promise<ProjectProperties | null> {
  try {
    const props = await getMsbuildProperties(projectFile);
    if (!props) return null;

    const outputAssemblyPath = buildOutputPath(projectFile, props);
    const packageReferences = readPackageReferences(
      props.ProjectAssetsFile,
      props.TargetFrameworkMoniker,
    );

    return {
      outputAssemblyPath,
      targetFrameworkMoniker: props.TargetFrameworkMoniker,
      defaultNamespace: props.RootNamespace,
      packageReferences,
    };
  } catch (err) {
    console.error(`MsbuildEvaluator: evaluation failed for ${projectFile}:`, err);
    return null;
  }
}

// ── MSBuild property evaluation ──────────────────────────────────────────

interface MsbuildProperties {
  TargetFrameworkMoniker: string;
  OutputPath: string;
  AssemblyName: string;
  RootNamespace: string;
  ProjectAssetsFile: string;
}

async function getMsbuildProperties(projectFile: string): Promise<MsbuildProperties | null> {
  return new Promise((resolve) => {
    const args = [
      'msbuild',
      projectFile,
      '-p:DesignTimeBuild=true',
      '-nologo',
      '-getProperty:TargetFrameworkMoniker;OutputPath;AssemblyName;RootNamespace;ProjectAssetsFile',
    ];

    const child = execFile(
      'dotnet',
      args,
      {
        timeout: 30_000,
        maxBuffer: 1024 * 64,
        env: { ...process.env, MSYS_NO_PATHCONV: '1' },
      },
      (error, stdout, _stderr) => {
        if (error) {
          console.error(
            `MsbuildEvaluator: dotnet msbuild failed for ${projectFile}: ${error.message}`,
          );
          resolve(null);
          return;
        }

        try {
          const parsed = JSON.parse(stdout) as {
            Properties: MsbuildProperties;
          };
          const p = parsed.Properties;

          if (!p.TargetFrameworkMoniker || !p.OutputPath || !p.AssemblyName) {
            console.error(`MsbuildEvaluator: missing required properties for ${projectFile}`);
            resolve(null);
            return;
          }

          resolve(p);
        } catch {
          console.error(
            `MsbuildEvaluator: failed to parse msbuild output for ${projectFile}: ${stdout.slice(0, 300)}`,
          );
          resolve(null);
        }
      },
    );

    // Suppress error on EPIPE / child process crashes — handled in callback
    child.on('error', () => {
      /* handled in callback */
    });
  });
}

// ── Output assembly path ─────────────────────────────────────────────────

function buildOutputPath(projectFile: string, props: MsbuildProperties): string {
  // OutputPath is relative to the project directory (e.g. bin\Debug\net10.0\)
  // AssemblyName is the file name without extension
  const projectDir = path.dirname(projectFile);
  const relativeOutput = props.OutputPath.replace(/\\$/, ''); // strip trailing backslash
  return path.resolve(projectDir, relativeOutput, `${props.AssemblyName}.dll`);
}

// ── Package references from project.assets.json ──────────────────────────

interface AssetsFile {
  targets?: Record<string, Record<string, { type?: string }>>;
  libraries?: Record<string, { type?: string }>;
}

function readPackageReferences(assetsFilePath: string, tfm: string): PackageRef[] {
  if (!assetsFilePath || !fs.existsSync(assetsFilePath)) {
    return [];
  }

  try {
    const assets = JSON.parse(fs.readFileSync(assetsFilePath, 'utf-8')) as AssetsFile;

    // `project.assets.json` has a `libraries` object where keys are "id/version"
    // Filter to NuGet packages (type: "package") in the current TFM target
    const targetKey = findTargetKey(assets, tfm);
    if (!targetKey) return [];

    const target = assets.targets?.[targetKey];
    if (!target) return [];

    return Object.entries(target)
      .filter(
        ([entryKey, value]) =>
          value?.type === 'package' && !entryKey.startsWith('Microsoft.NETCore.'),
      )
      .map(([key]) => {
        const slash = key.lastIndexOf('/');
        return {
          packageId: key.slice(0, slash),
          version: key.slice(slash + 1),
        };
      });
  } catch {
    return [];
  }
}

/**
 * Finds the TFM target key in project.assets.json that best matches
 * the given TargetFrameworkMoniker (e.g. ".NETCoreApp,Version=v8.0" → "net8.0").
 */
function findTargetKey(assets: AssetsFile, tfm: string): string | undefined {
  const targets = assets.targets;
  if (!targets) return undefined;

  // The assets file keys are short TFMs like "net8.0", "netstandard2.0", "net481"
  const shortTfm = tfmToShort(tfm);

  if (shortTfm && targets[shortTfm]) return shortTfm;

  // Fallback: return the first available target
  return Object.keys(targets)[0];
}

/**
 * Converts a full TargetFrameworkMoniker to a short name.
 * ".NETCoreApp,Version=v8.0" → "net8.0"
 * ".NETStandard,Version=v2.0" → "netstandard2.0"
 * ".NETFramework,Version=v4.8.1" → "net481"
 */
function tfmToShort(tfm: string): string {
  const match = tfm.match(
    /\.NET(?:CoreApp|Standard|Framework|Portable),Version=v(\d+(?:\.\d+)?(?:\.\d+)?)/i,
  );
  if (!match) return tfm.toLowerCase().replace(/[^a-z0-9.]/g, '');

  const version = match[1];
  const majorMinor = version.split('.').slice(0, 2);
  const major = Number(majorMinor[0]);
  const minor = Number(majorMinor[1] ?? 0);

  if (tfm.includes('NETFramework')) {
    return `net${major}${minor < 10 ? '0' : ''}${minor}`;
  }
  if (tfm.includes('NETStandard')) {
    return `netstandard${major}.${minor}`;
  }
  return `net${major}.${minor}`;
}
