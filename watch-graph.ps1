# ============================================================
# watch-graph.ps1  —  Auto-update graphify graph on code change
# Project: TeenPattiAsia
# Uses hash-based polling (reliable on Windows across all launch methods)
# ============================================================

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$AssetsPath  = Join-Path $ProjectRoot "Assets"
$GraphifyOut = Join-Path $ProjectRoot "graphify-out"
$PythonFile  = Join-Path $GraphifyOut ".graphify_python"
$LogFile     = Join-Path $GraphifyOut "watch.log"
$PollSeconds = 3      # how often to scan for changes
$Debounce    = 5      # seconds of quiet after last change before rebuilding

Set-Location $ProjectRoot

function Write-Log {
    param([string]$msg)
    $ts = Get-Date -Format "HH:mm:ss"
    $line = "[$ts] $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

function Get-GraphifyPython {
    if (Test-Path $PythonFile) { return (Get-Content $PythonFile -Raw).Trim() }
    return $null
}

function Get-FileSnapshot {
    # Returns hashtable of path -> LastWriteTimeUtc for all .cs files under Assets
    $snap = @{}
    Get-ChildItem -Path $AssetsPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | ForEach-Object {
        $snap[$_.FullName] = $_.LastWriteTimeUtc
    }
    return $snap
}

function Run-IncrementalUpdate {
    $py = Get-GraphifyPython
    if (-not $py) {
        Write-Log "ERROR: graphify not configured (.graphify_python missing). Run the full pipeline first."
        return
    }

    Write-Log "Change detected - running incremental update..."

    # Step 1: detect changed files
    $script1 = @"
import sys, json
from graphify.detect import detect_incremental, save_manifest
from pathlib import Path

result = detect_incremental(Path('Assets'))
new_total = result.get('new_total', 0)
Path('graphify-out/.graphify_incremental.json').write_text(json.dumps(result, ensure_ascii=False), encoding='utf-8')
deleted = list(result.get('deleted_files', []))
if new_total == 0 and not deleted:
    print('NO_CHANGES')
else:
    if deleted: print(f'{len(deleted)} deleted file(s)')
    if new_total > 0: print(f'{new_total} new/changed file(s)')
"@
    $script1 | Out-File "$GraphifyOut\.w1.py" -Encoding utf8
    $out1 = & $py "$GraphifyOut\.w1.py" 2>&1
    Remove-Item "$GraphifyOut\.w1.py" -ErrorAction SilentlyContinue

    if ("$out1" -match "NO_CHANGES") {
        Write-Log "No relevant changes - skipping rebuild."
        return
    }
    Write-Log "Detected: $out1"

    # Step 2: AST on changed files only
    $script2 = @"
import json
from graphify.extract import collect_files, extract
from pathlib import Path

inc = json.loads(Path('graphify-out/.graphify_incremental.json').read_text(encoding='utf-8'))
changed = [f for files in inc.get('new_files', {}).values() for f in files]
code_exts = {'.cs','.py','.ts','.js','.go','.rs','.java','.cpp','.c','.rb','.swift','.kt'}
code_files = [Path(f) for f in changed if Path(f).suffix.lower() in code_exts]

if code_files:
    result = extract(code_files)
    Path('graphify-out/.graphify_extract.json').write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding='utf-8')
    print(f'AST: {len(result["nodes"])} nodes, {len(result["edges"])} edges from {len(code_files)} file(s)')
else:
    Path('graphify-out/.graphify_extract.json').write_text(
        json.dumps({'nodes':[],'edges':[],'hyperedges':[],'input_tokens':0,'output_tokens':0}), encoding='utf-8')
    print('No code changes')

if __name__ == '__main__':
    pass
"@
    $script2 | Out-File "$GraphifyOut\.w2.py" -Encoding utf8
    $out2 = & $py "$GraphifyOut\.w2.py" 2>&1
    Remove-Item "$GraphifyOut\.w2.py" -ErrorAction SilentlyContinue
    Write-Log "$out2"

    # Step 3: merge into existing graph
    $script3 = @"
import json
from graphify.build import build_from_json
from graphify.export import to_json
from networkx.readwrite import json_graph
from pathlib import Path

G_existing = json_graph.node_link_graph(
    json.loads(Path('graphify-out/graph.json').read_text(encoding='utf-8')), edges='links')
G_new = build_from_json(
    json.loads(Path('graphify-out/.graphify_extract.json').read_text(encoding='utf-8')))

inc = json.loads(Path('graphify-out/.graphify_incremental.json').read_text(encoding='utf-8'))
deleted = set(inc.get('deleted_files', []))
if deleted:
    to_remove = [n for n, d in G_existing.nodes(data=True) if d.get('source_file') in deleted]
    G_existing.remove_nodes_from(to_remove)
    print(f'Pruned {len(to_remove)} ghost node(s)')

G_existing.update(G_new)
to_json(G_existing, {}, 'graphify-out/graph.json')
print(f'Graph: {G_existing.number_of_nodes()} nodes, {G_existing.number_of_edges()} edges')

from graphify.detect import save_manifest
save_manifest(inc['files'])
"@
    $script3 | Out-File "$GraphifyOut\.w3.py" -Encoding utf8
    $out3 = & $py "$GraphifyOut\.w3.py" 2>&1
    Remove-Item "$GraphifyOut\.w3.py" -ErrorAction SilentlyContinue
    Remove-Item "$GraphifyOut\.graphify_incremental.json" -ErrorAction SilentlyContinue
    Remove-Item "$GraphifyOut\.graphify_extract.json" -ErrorAction SilentlyContinue
    Write-Log "$out3"

    # Step 4: regenerate HTML
    $script4 = @"
import json
from graphify.export import to_html
from networkx.readwrite import json_graph
from pathlib import Path

G = json_graph.node_link_graph(
    json.loads(Path('graphify-out/graph.json').read_text(encoding='utf-8')), edges='links')
labels_raw  = json.loads(Path('graphify-out/.graphify_labels_saved.json').read_text(encoding='utf-8')) if Path('graphify-out/.graphify_labels_saved.json').exists() else {}
comms_raw   = json.loads(Path('graphify-out/.graphify_communities_saved.json').read_text(encoding='utf-8')) if Path('graphify-out/.graphify_communities_saved.json').exists() else {}
communities = {int(k): v for k, v in comms_raw.items()}
labels      = {int(k): v for k, v in labels_raw.items()}
if G.number_of_nodes() <= 5000:
    to_html(G, communities, 'graphify-out/graph.html', community_labels=labels or None)
    print('graph.html updated')
"@
    $script4 | Out-File "$GraphifyOut\.w4.py" -Encoding utf8
    $out4 = & $py "$GraphifyOut\.w4.py" 2>&1
    Remove-Item "$GraphifyOut\.w4.py" -ErrorAction SilentlyContinue
    Write-Log "$out4"

    Write-Log "Done. graph.html + graph.json refreshed."
    Write-Log "---"
}

# ── Save community label snapshot (used during watch-time HTML regen) ─────────
$py = Get-GraphifyPython
if ($py) {
    $snap = @"
import json, shutil
from pathlib import Path
lf = Path('graphify-out/.graphify_labels.json')
if lf.exists(): shutil.copy(lf, 'graphify-out/.graphify_labels_saved.json')
"@
    $snap | Out-File "$GraphifyOut\.snap.py" -Encoding utf8
    & $py "$GraphifyOut\.snap.py" 2>$null
    Remove-Item "$GraphifyOut\.snap.py" -ErrorAction SilentlyContinue
}

# ── Polling loop ───────────────────────────────────────────────────────────────
Write-Log "Watcher started. Polling $AssetsPath every ${PollSeconds}s for .cs changes."
Write-Log "Debounce: ${Debounce}s after last change before rebuild."

$snapshot       = Get-FileSnapshot
$lastChangeTime = $null
$pendingRebuild = $false

try {
    while ($true) {
        Start-Sleep -Seconds $PollSeconds

        $current = Get-FileSnapshot

        # Detect any added, removed, or modified files
        $changed = $false
        foreach ($path in $current.Keys) {
            if (-not $snapshot.ContainsKey($path) -or $snapshot[$path] -ne $current[$path]) {
                $changed = $true; break
            }
        }
        if (-not $changed) {
            foreach ($path in $snapshot.Keys) {
                if (-not $current.ContainsKey($path)) { $changed = $true; break }
            }
        }

        if ($changed) {
            $snapshot        = $current
            $lastChangeTime  = [DateTime]::UtcNow
            $pendingRebuild  = $true
        }

        # Fire rebuild only after Debounce seconds of quiet
        if ($pendingRebuild -and $lastChangeTime -and ([DateTime]::UtcNow - $lastChangeTime).TotalSeconds -ge $Debounce) {
            $pendingRebuild = $false
            Run-IncrementalUpdate
        }
    }
} finally {
    Write-Log "Watcher stopped."
}
