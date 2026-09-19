#!/usr/bin/env python3
"""Sync validated committed changes to the isolated review editor and reopen it.

Never targets the main project, changes region saves, or pushes Git commits.
"""
import argparse
import datetime
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import time

SOURCE = Path(__file__).resolve().parents[3]
REVIEW = Path('/Users/joelinstrum/dev/CityForge-Regions-Review')
UNITY = Path('/Applications/Unity/Hub/Editor/6000.1.12f1/Unity.app/Contents/MacOS/Unity')
STATE = REVIEW / 'RecoveryBackups' / 'review-sync-state.json'

def git(*args):
    return subprocess.check_output(['git', '-C', str(SOURCE), *args], text=True).strip()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--from-commit', help='Last deployed commit, required only before the first tracked sync')
    parser.add_argument('--dry-run', action='store_true')
    args = parser.parse_args()
    if not (REVIEW / 'ProjectSettings' / 'ProjectVersion.txt').exists():
        raise SystemExit('Expected isolated review project is missing.')
    if SOURCE == REVIEW or REVIEW.name != 'CityForge-Regions-Review':
        raise SystemExit('Refusing unexpected review target.')
    if git('status', '--porcelain'):
        raise SystemExit('Commit the validated changes before syncing the review project.')
    state = json.loads(STATE.read_text()) if STATE.exists() else {}
    base = args.from_commit or state.get('commit')
    if not base:
        raise SystemExit('Supply --from-commit for the first sync.')
    head = git('rev-parse', 'HEAD')
    changed = git('diff', '--name-only', '--no-renames', base, head).splitlines()
    allowed = ('Assets/', 'Packages/', 'ProjectSettings/', 'Documentation/')
    changed = [p for p in changed if p.startswith(allowed) or p == 'AGENTS.md']
    print(f'Review sync: {base[:8]} -> {head[:8]} ({len(changed)} files)', flush=True)
    if args.dry_run:
        print('\n'.join(changed))
        return
    # Match the complete project argument; never terminate another Unity editor.
    rows = subprocess.check_output(['ps', '-axo', 'pid=,command='], text=True).splitlines()
    for row in rows:
        pid_text, command = row.strip().split(None, 1)
        if not command.startswith(str(UNITY) + ' '):
            continue
        marker = ' -projectPath ' + str(REVIEW)
        if marker not in command:
            continue
        # Paths containing spaces in other project names cannot match the complete target token.
        tail = command.split(marker, 1)[1]
        if tail and not tail.startswith(' '):
            continue
        pid = int(pid_text)
        os.kill(pid, signal.SIGTERM)
        for _ in range(30):
            try:
                os.kill(pid, 0)
            except ProcessLookupError:
                break
            time.sleep(1)
        else:
            raise SystemExit('Review editor did not close; no files were changed.')
    stamp = datetime.datetime.now().strftime('%Y%m%d-%H%M%S')
    backup = REVIEW / 'RecoveryBackups' / ('automatic-review-' + stamp)
    backup.mkdir(parents=True)
    for relative in changed:
        source, target, saved = SOURCE / relative, REVIEW / relative, backup / relative
        if target.exists():
            saved.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(target, saved)
        if source.is_file():
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
        elif target.is_file():
            target.unlink()  # Previous copy is preserved in this sync's recovery backup.
    log = Path('/tmp') / ('cityforge-regions-review-' + stamp + '.log')
    STATE.write_text(json.dumps(dict(commit=head, source=str(SOURCE), backup=str(backup), log=str(log)), indent=2)+'\n')
    process = subprocess.Popen([str(UNITY), '-projectPath', str(REVIEW), '-logFile', str(log)],
                               stdin=subprocess.DEVNULL, stdout=subprocess.DEVNULL,
                               stderr=subprocess.DEVNULL, start_new_session=True)
    print(f'Reopened review Unity (PID {process.pid}). Log: {log}', flush=True)
    # Wait for compilation and initial import so a failed load is not reported as ready.
    deadline = time.monotonic() + 50
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise SystemExit(f'Review Unity exited; inspect {log}')
        content = log.read_text(errors='replace') if log.exists() else ''
        if 'error CS' in content or 'Scripts have compiler errors' in content:
            raise SystemExit(f'Review compilation failed; inspect {log}')
        if '[Project] Loading completed' in content:
            print('Review project loaded without compiler errors.', flush=True)
            return
        time.sleep(1)
    print(f'Unity is still opening; verify completion in {log}', flush=True)

if __name__ == '__main__':
    main()
