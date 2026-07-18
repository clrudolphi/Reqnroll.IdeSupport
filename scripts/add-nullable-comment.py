import os
import re

root = r'C:\Users\clrud\source\Reqnroll.IdeSupport\tests'
pattern = re.compile(r'^(#nullable disable)')

files = []
for dirpath, dirnames, filenames in os.walk(root):
    # skip obj and bin
    dirnames[:] = [d for d in dirnames if d not in ('obj', 'bin')]
    for fn in filenames:
        if fn.endswith('.cs'):
            path = os.path.join(dirpath, fn)
            files.append(path)

count = 0
for path in sorted(files):
    with open(path, 'r', encoding='utf-8', errors='replace') as f:
        content = f.read()
    
    # Check if file starts with #nullable disable (possibly after BOM)
    stripped = content.lstrip('\ufeff')
    if stripped.startswith('#nullable disable'):
        # Add comment above
        comment = '// #nullable disable — suppress nullable warnings; see issue #207\n'
        new_content = content.replace('#nullable disable', comment + '#nullable disable', 1)
        with open(path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        count += 1
        print(f'  Updated: {os.path.relpath(path, root)}')

print(f'\nUpdated {count} files')
