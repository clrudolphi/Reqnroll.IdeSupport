#!/usr/bin/env node
// ---------------------------------------------------------------------------
// dev-publish-server.mjs — Republish the LSP server for F5 dev-mode launches,
// but only when source has changed since the last dev publish.
//
// The Extension Development Host (F5) loads the server from:
//   src/LSP/Reqnroll.IdeSupport.LSP.Server/bin/Release/net10.0/win-x64/publish/
// (see resolveServerPath's non-production branch in src/extension.ts).
//
// Nothing previously rebuilt that folder before F5, so it could silently
// serve a stale binary (issue #44). This script compares the newest mtime
// under src/LSP against the published exe's mtime and republishes only when
// source is newer, keeping the common case (no server-side changes) fast.
// ---------------------------------------------------------------------------
import { execFileSync } from 'node:child_process';
import { existsSync, statSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const SCRIPT_DIR = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(SCRIPT_DIR, '..', '..', '..');
const LSP_ROOT = path.join(REPO_ROOT, 'src', 'LSP');
const SERVER_PROJECT = path.join(
  LSP_ROOT,
  'Reqnroll.IdeSupport.LSP.Server',
  'Reqnroll.IdeSupport.LSP.Server.csproj',
);
const CONNECTOR_PROJECT = path.join(
  LSP_ROOT,
  'Reqnroll.IdeSupport.LSP.Connector',
  'Connector',
  'Connector.csproj',
);
const PUBLISH_DIR = path.join(
  LSP_ROOT,
  'Reqnroll.IdeSupport.LSP.Server',
  'bin',
  'Release',
  'net10.0',
  'win-x64',
  'publish',
);
const PUBLISHED_EXE = path.join(PUBLISH_DIR, 'Reqnroll.IdeSupport.LSP.Server.exe');

const IGNORED_DIR_NAMES = new Set(['bin', 'obj', 'node_modules', '.git']);

function newestSourceMtime(dir) {
  let newest = 0;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.isDirectory()) {
      if (IGNORED_DIR_NAMES.has(entry.name)) continue;
      newest = Math.max(newest, newestSourceMtime(path.join(dir, entry.name)));
    } else {
      newest = Math.max(newest, statSync(path.join(dir, entry.name)).mtimeMs);
    }
  }
  return newest;
}

function isStale() {
  if (!existsSync(PUBLISHED_EXE)) return true;
  const publishedMtime = statSync(PUBLISHED_EXE).mtimeMs;
  return newestSourceMtime(LSP_ROOT) > publishedMtime;
}

if (!isStale()) {
  console.log('==> LSP server publish is up to date, skipping republish.');
  process.exit(0);
}

console.log('==> LSP source changed since last dev publish, republishing win-x64 server...');

execFileSync(
  'dotnet',
  ['restore', CONNECTOR_PROJECT, '--runtime', 'win-x64', '--nologo'],
  { stdio: 'inherit' },
);

execFileSync(
  'dotnet',
  [
    'publish',
    SERVER_PROJECT,
    '--configuration',
    'Release',
    '--runtime',
    'win-x64',
    '--self-contained',
    'true',
    '--nologo',
  ],
  { stdio: 'inherit' },
);

console.log('==> Dev server republished.');
