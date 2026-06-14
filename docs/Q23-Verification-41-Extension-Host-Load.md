# Q23 §4.1 Verification — Extension host load without a `.feature` trigger

**Purpose:** Determine whether the VS.Extensibility extension host loads from its menu contribution alone, or whether it requires the `LanguageServerProvider` (`.feature` file) to fire first.

**Prerequisites:**
- A Reqnroll solution with at least one `.feature` file and one `.cs` binding file
- The Reqnroll VSIX installed in the Experimental Instance

---

## Procedure

### Step 1 — Prepare a clean editor session

1. Open your Reqnroll test solution.
2. Close **all** open document tabs (`.feature`, `.cs`, etc.) so VS saves an empty editor session.
3. Close the solution.
4. Exit Visual Studio.

### Step 2 — Observe the Extensions menu on cold startup

1. Reopen the solution.
2. **Do NOT** click any document tab.
3. Immediately check the **Extensions** menu → locate the **Reqnroll** submenu.

**Record the result:**

> - **`☐` Yes** — "Extensions › Reqnroll" submenu is visible
> - **`☐` No** — "Extensions › Reqnroll" submenu is absent

### Step 3 — Open a `.feature` file

1. Click any `.feature` tab (or open one from Solution Explorer).
2. Check again in **Extensions** → **Reqnroll**.

**Record the result:**

> - **`☐` Appeared** — the submenu now shows after the `.feature` file was opened
> - **`☐` Still absent** — the submenu never appears

---

## Interpretation

| Step 2 result | Step 3 result | Meaning |
|---|---|---|
| Yes | — | Extension host loads from menu contribution alone. `ProvideAutoLoad` in §4.2 works as described. |
| No | Appeared | Extension host is gated on the LSP activation. `ProvideAutoLoad` in §4.2 is even more critical; add explicit host-activation code in `ReqnrollPluginPackage.InitializeAsync`. |
| No | Still absent | Deeper problem — the extension may not be loading at all. Check VSIX deployment and enabled status in Extensions Manager. |

---

## After recording the result

Send the result (Step 2 Yes/No and Step 3 Appeared/Still absent) to me. I'll adjust the implementation accordingly and proceed with the remaining pieces.
