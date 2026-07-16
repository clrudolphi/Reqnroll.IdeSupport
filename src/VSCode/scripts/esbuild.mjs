// esbuild.mjs — bundles src/extension.ts + its imports (including npm
// dependencies like vscode-languageclient) into a single out/extension.js.
//
// Packaged VSIXs don't ship node_modules (both .vscodeignore and .gitignore
// exclude it), so the extension must be self-contained at runtime — only
// the 'vscode' module itself is left external, since VS Code injects that.
//
// Usage:
//   node scripts/esbuild.mjs            # production build (minified)
//   node scripts/esbuild.mjs --watch     # watch mode for development

import * as esbuild from 'esbuild';

const watch = process.argv.includes('--watch');
const production = process.argv.includes('--production') || !watch;

const ctx = await esbuild.context({
  entryPoints: ['src/extension.ts'],
  bundle: true,
  outfile: 'out/extension.js',
  external: ['vscode'],
  format: 'cjs',
  platform: 'node',
  target: 'node18',
  sourcemap: !production,
  minify: production,
});

if (watch) {
  await ctx.watch();
} else {
  await ctx.rebuild();
  await ctx.dispose();
}
