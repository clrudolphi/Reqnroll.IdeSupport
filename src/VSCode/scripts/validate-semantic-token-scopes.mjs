/**
 * validate-semantic-token-scopes.js
 *
 * Reads ReqnrollClassificationTypeNames.Ordered from the .NET server project
 * and verifies that every server token type has a corresponding entry in
 * the VS Code extension's package.json semanticTokenScopes contribution.
 *
 * Usage:
 *   node scripts/validate-semantic-token-scopes.js
 *
 * Exit code: 0 = match, 1 = mismatch
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '..', '..', '..');

// ── Step 1: Parse server token types from ReqnrollClassificationTypeNames.cs ──

const serverTypesPath = path.join(
  repoRoot,
  'src', 'Core', 'Reqnroll.IdeSupport.Common', 'Classification',
  'ReqnrollClassificationTypeNames.cs',
);

const source = fs.readFileSync(serverTypesPath, 'utf-8');

// Extract the `Ordered` array literal — everything between `new[] {` and `};`
const orderedMatch = source.match(
  /public static readonly IReadOnlyList<string> Ordered = new\[\]\s*\n?\{(.*?)\};/s,
);

if (!orderedMatch) {
  console.error('ERROR: Could not parse ReqnrollClassificationTypeNames.Ordered');
  process.exit(1);
}

// Extract every constant reference from the array body — each entry is a C# identifier
// on its own line, possibly followed by a comment.  Use a regex to grab each name.
const constantRefs = orderedMatch[1].match(/\b(\w+)\s*,/g);
if (!constantRefs || constantRefs.length === 0) {
  console.error('ERROR: No constant references found in Ordered array');
  process.exit(1);
}

const serverTokens = constantRefs
  .map((ref) => ref.replace(/,/, '').trim())
  .map((constName) => {
    // Find the actual const declaration to get the server-side token name
    const declMatch = source.match(
      new RegExp(`public const string ${constName}\\s*=\\s*"([^"]+)"`),
    );
    return declMatch ? declMatch[1] : null;
  })
  .filter((t) => t !== null);

console.log(`Server token types (${serverTokens.length}):`);
serverTokens.forEach((t) => console.log(`  ${t}`));

// ── Step 2: Parse package.json semanticTokenScopes ──────────────────────────

const packageJsonPath = path.join(repoRoot, 'src', 'VSCode', 'package.json');
const pkg = JSON.parse(fs.readFileSync(packageJsonPath, 'utf-8'));

const scopes = pkg.contributes?.semanticTokenScopes;
if (!scopes || scopes.length === 0) {
  console.error('ERROR: No semanticTokenScopes found in package.json');
  process.exit(1);
}

const pkgTokens = Object.keys(scopes[0].scopes);
console.log(`\npackage.json token types (${pkgTokens.length}):`);
pkgTokens.forEach((t) => console.log(`  ${t}`));

// ── Step 3: Compare ─────────────────────────────────────────────────────────

let exitCode = 0;

// Check every server token is in package.json
for (const serverToken of serverTokens) {
  if (!pkgTokens.includes(serverToken)) {
    console.error(`\nMISSING: Server token "${serverToken}" has no entry in package.json semanticTokenScopes`);
    exitCode = 1;
  }
}

// Check every package.json token is in the server legend (warn only — extra entries are harmless)
for (const pkgToken of pkgTokens) {
  if (!serverTokens.includes(pkgToken)) {
    console.warn(`\nWARNING: package.json has token "${pkgToken}" that is not in the server legend (harmless, but should be reviewed)`);
  }
}

if (exitCode === 0) {
  console.log('\n✅ All server semantic token types are mapped in package.json');
} else {
  console.log('\n❌ Mismatch found — update package.json semanticTokenScopes to match the server legend');
}

process.exit(exitCode);
