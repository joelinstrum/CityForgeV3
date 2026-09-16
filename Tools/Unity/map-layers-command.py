#!/usr/bin/env python3
"""Send QA commands to this checkout's Unity editor, never a neighbouring project."""
import argparse
from pathlib import Path
import tempfile
import time

parser = argparse.ArgumentParser()
parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
parser.add_argument('commands', nargs='+')
args = parser.parse_args()
prefix = 'cityforge-' + args.project.resolve().name.replace(' ', '_') + '-map-layers-'
root = Path(tempfile.gettempdir())
command_file, result_file = root / (prefix + 'command.txt'), root / (prefix + 'result.txt')
for command in args.commands:
    stamp = result_file.stat().st_mtime_ns if result_file.exists() else 0
    pending = command_file.with_suffix('.next')
    pending.write_text(command)
    pending.replace(command_file)
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        time.sleep(.1)
        if result_file.exists() and result_file.stat().st_mtime_ns != stamp:
            result = result_file.read_text()
            print(result, flush=True)
            if not result.startswith('OK '):
                raise SystemExit(1)
            break
    else:
        raise SystemExit('Timed out: ' + command)
    time.sleep(.3)
