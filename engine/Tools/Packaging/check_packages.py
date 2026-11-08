#!/usr/bin/env python3
"""Checks the packages players download (M12, Q28; Docs/Packaging.md, "How it works", step 3). Run through
Tools/Scripts/check-packages.sh, which says how; Linux only (Q15: the Windows archives are checked here too).

Each archive is unpacked into a scratch folder and held to the rules below; every broken rule is reported, and the
check fails if any is. --self-test breaks each archive one rule at a time and shows that each break is caught.
"""
import fnmatch
import hashlib
import os
import re
import shutil
import signal
import struct
import subprocess
import sys
import tarfile
import tempfile
import zipfile
import zlib

ENGINE = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
PACKAGING = os.path.join(ENGINE, "Tools", "Packaging")
PACKS = os.path.join(ENGINE, "Assets", "Packs")

# M12-2: the executables' names; the assembly each program carries.
PROGRAMS = {
    "stat-fick": {"linux": "stat-fick", "windows": "Stat Fick.exe", "assembly": "Statventure.Sandbox", "packs": True},
    "statventure-editor": {"linux": "statventure-editor", "windows": "Statventure Editor.exe",
                           "assembly": "Statventure.Editor", "packs": False},
}
# Q1 and Q4: <program>-<version>-linux.tar.gz or -windows.zip, the version 2026.11.8.1 or dev.
VERSION = r"dev|[1-9][0-9]{3}\.(?:1[0-2]|[1-9])\.(?:3[01]|[12][0-9]|[1-9])\.[1-9][0-9]*"
ARCHIVE_NAME = re.compile(rf"^(stat-fick|statventure-editor)-({VERSION})-(linux\.tar\.gz|windows\.zip)$")
# What a native library may need from the system (Docs/Packaging.md, "Native libraries"): Linux's C and C++ runtimes
# and the Vulkan loader; Windows' own libraries, the UCRT (part of Windows 10 and 11) and the Vulkan loader (the GPU
# driver's). Everything else must be in the folder.
LINUX_SYSTEM = {"libc.so.6", "libm.so.6", "libdl.so.2", "librt.so.1", "libpthread.so.0", "libgcc_s.so.1",
                "libstdc++.so.6", "ld-linux-x86-64.so.2", "libvulkan.so.1"}
GLIBC_MOST = (2, 34)        # Ubuntu 22.04, Debian 12, Fedora 35 and later
GLIBCXX_MOST = (3, 4, 30)   # GCC 12's libstdc++: Ubuntu 22.04, Debian 12, Fedora 36 and later
WINDOWS_SYSTEM = {"kernel32.dll", "user32.dll", "gdi32.dll", "shell32.dll", "advapi32.dll", "ole32.dll", "oleaut32.dll",
                  "avrt.dll", "winmm.dll", "vulkan-1.dll"}
WINDOWS_UCRT = re.compile(r"^api-ms-win-crt-[a-z]+-l1-1-0\.dll$")
# .NET's single-file bundle marker (the host's bundle_signature).
BUNDLE_SIGNATURE = bytes([0x8b, 0x12, 0x02, 0xb9, 0x6a, 0x61, 0x20, 0x38, 0x72, 0x7b, 0x93, 0x02, 0x14, 0xd7, 0xa0, 0x32,
                          0x13, 0xf5, 0xb9, 0xe6, 0xef, 0xae, 0x33, 0x18, 0xee, 0x3b, 0x2d, 0xce, 0x24, 0xb3, 0x6a, 0xae])
# The kind of a portable PDB's Source Link entry (CustomDebugInformation), as its GUID's bytes.
SOURCE_LINK = bytes.fromhex("560511cc91a0384d9fec25ab9a351a6a")
CAPTURE_FRAMES = "30"
RUN_SECONDS = 180


class Package:
    """An archive unpacked: its name's parts, its entries (path in the folder -> dir, file or exec) and where they are."""

    def __init__(self, archive, folder):
        self.archive = archive
        self.name = os.path.basename(archive)
        self.folder = folder
        self.entries = {}
        self.problems = []
        match = ARCHIVE_NAME.match(self.name)
        if not match:
            self.problems.append(f"name: '{self.name}' isn't <program>-<version>-linux.tar.gz or -windows.zip, the version "
                                 "2026.11.8.1 or dev (Q1, Q4)")
            self.program = self.version = self.platform = None
            return
        self.program, self.version = match.group(1), match.group(2)
        self.platform = "linux" if match.group(3).startswith("linux") else "windows"
        self.top = f"{self.program}-{self.version}"
        self.program_file = PROGRAMS[self.program][self.platform]

    def path(self, entry):
        return os.path.join(self.folder, entry)

    def read(self, entry):
        with open(self.path(entry), "rb") as f:
            return f.read()


def unpack(archive, into):
    """Reads an archive into a Package, unpacking its folder's files under `into`."""
    package = Package(archive, into)
    if package.program is None:
        return package
    raw = []
    try:
        if package.platform == "linux":
            with tarfile.open(archive, "r:gz") as tar:
                for member in tar:
                    if member.uid != 0 or member.gid != 0:
                        package.problems.append(f"owner: {member.name} is owned by {member.uid}:{member.gid}, not 0:0")
                    if member.isdir():
                        if member.mode != 0o755:
                            package.problems.append(f"mode: {member.name} has mode {member.mode:o}, not 755")
                        raw.append((member.name, "dir", None))
                    elif member.isreg():
                        if member.mode not in (0o644, 0o755):
                            package.problems.append(f"mode: {member.name} has mode {member.mode:o}, not 644 or 755")
                        raw.append((member.name, "exec" if member.mode & 0o111 else "file", tar.extractfile(member).read()))
                    else:
                        package.problems.append(f"entry: {member.name} is a link or a special file")
        else:
            with zipfile.ZipFile(archive) as z:
                for info in z.infolist():
                    raw.append((info.filename, "dir", None) if info.filename.endswith("/")
                               else (info.filename, "file", z.read(info)))
    except (OSError, tarfile.TarError, zipfile.BadZipFile, EOFError, zlib.error) as error:
        package.problems.append(f"archive: can't be read: {error}")
        return package
    for name, kind, data in raw:
        path = name.rstrip("/")
        if path.startswith("/") or "\\" in path or ".." in path.split("/"):
            package.problems.append(f"entry: {name} isn't a plain relative path")
            continue
        if path == package.top:
            continue
        if not path.startswith(package.top + "/"):
            package.problems.append(f"folder: {name} isn't inside the folder {package.top}/ (Q4)")
            continue
        entry = path[len(package.top) + 1:]
        package.entries[entry] = kind
        target = package.path(entry)
        if kind == "dir":
            os.makedirs(target, exist_ok=True)
        else:
            os.makedirs(os.path.dirname(target), exist_ok=True)
            with open(target, "wb") as f:
                f.write(data)
            os.chmod(target, 0o755 if kind == "exec" else 0o644)
    for entry in list(package.entries):
        parts = entry.split("/")
        for i in range(1, len(parts)):
            package.entries.setdefault("/".join(parts[:i]), "dir")
    return package


# --- What the repo expects ------------------------------------------------------------------------------------------

def listing_path(program, platform):
    return os.path.join(PACKAGING, "listings", f"{program}-{platform}.txt")


def read_listing(program, platform):
    """Tools/Packaging/listings/<program>-<platform>.txt: (entries: path -> kind, bundle: names)."""
    entries, bundle = {}, set()
    with open(listing_path(program, platform), encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n")
            if not line or line.startswith("#"):
                continue
            kind, _, path = line.partition(" ")
            if kind == "bundle":
                bundle.add(path)
            else:
                entries[path.rstrip("/")] = kind
    return entries, bundle


def read_licences():
    """Tools/Packaging/licenses.txt: the shipped texts and the cover lines (pattern, texts)."""
    shipped, covers = set(), []
    with open(os.path.join(PACKAGING, "licenses.txt"), encoding="utf-8") as f:
        for line in f:
            parts = line.split()
            if not parts or parts[0].startswith("#"):
                continue
            if parts[0] == "text":
                shipped.add(parts[1])
            elif parts[0] == "cover":
                covers.append((parts[1], [] if parts[2:] == ["-"] else parts[2:]))
    return shipped, covers


def read_vc_runtime():
    files = {}
    with open(os.path.join(PACKAGING, "vc-runtime.txt"), encoding="utf-8") as f:
        for line in f:
            parts = line.split()
            if len(parts) == 4 and parts[0] == "file":
                files[parts[1]] = parts[3]
    return files


# --- Binaries -------------------------------------------------------------------------------------------------------

def is_elf(data):
    return data[:4] == b"\x7fELF"


def is_pe(data):
    if data[:2] != b"MZ" or len(data) < 0x40:
        return False
    at = struct.unpack_from("<I", data, 0x3c)[0]
    return data[at:at + 4] == b"PE\0\0"


def pe_headers(data):
    """(machine, optional header magic, subsystem, sections, optional header offset, its size)."""
    at = struct.unpack_from("<I", data, 0x3c)[0]
    machine, sections, _, _, _, optional_size, _ = struct.unpack_from("<HHIIIHH", data, at + 4)
    optional = at + 24
    magic = struct.unpack_from("<H", data, optional)[0]
    subsystem = struct.unpack_from("<H", data, optional + 68)[0]
    table = optional + optional_size
    section_list = [struct.unpack_from("<8sIIII", data, table + 40 * i) for i in range(sections)]
    return machine, magic, subsystem, section_list, optional


def pe_offset(section_list, rva):
    for _, virtual_size, address, raw_size, raw in section_list:
        if address <= rva < address + max(virtual_size, raw_size):
            return rva - address + raw
    return None


def pe_directory(data, index):
    _, magic, _, section_list, optional = pe_headers(data)
    directories = optional + (96 if magic == 0x10b else 112)
    rva, size = struct.unpack_from("<II", data, directories + 8 * index)
    return (pe_offset(section_list, rva) if rva else None), size, section_list


def is_il_only(data):
    """Whether a PE file is an IL-only .NET assembly: a CLI header (the COM descriptor, data directory 14) whose flags
    have COMIMAGE_FLAGS_ILONLY (1), so no native code. .NET loads such an assembly itself (beside a single-file program:
    the game's data library, M12-9), never through Windows' loader, so its one import, mscoree.dll (the old CLR's
    entry, which every managed PE file names), is no need of it (M12 step 6; the manager's decision (a), 2026-11-08)."""
    at, size, _ = pe_directory(data, 14)
    if at is None or size < 20:
        return False
    return bool(struct.unpack_from("<I", data, at + 16)[0] & 1)


def pe_imports(data):
    """The DLLs a PE file imports by name (its import table)."""
    at, _, section_list = pe_directory(data, 1)
    names = []
    while at is not None:
        lookup, _, _, name_rva, thunks = struct.unpack_from("<IIIII", data, at)
        if lookup == 0 and name_rva == 0 and thunks == 0:
            break
        name_at = pe_offset(section_list, name_rva)
        names.append(data[name_at:data.index(b"\0", name_at)].decode("ascii"))
        at += 20
    return names


def elf_needs(path):
    """(NEEDED, RUNPATH or RPATH, versions required) of an ELF file, from objdump -p."""
    output = subprocess.run(["objdump", "-p", path], capture_output=True, text=True).stdout
    needed, runpath, versions = [], "", []
    references = False
    for line in output.splitlines():
        parts = line.split()
        if len(parts) == 2 and parts[0] == "NEEDED":
            needed.append(parts[1])
        elif len(parts) == 2 and parts[0] in ("RUNPATH", "RPATH"):
            runpath += parts[1] + ":"
        elif line.startswith("Version References:"):
            references = True
        elif references and len(parts) == 4 and parts[0].startswith("0x"):
            versions.append(parts[3])
        elif references and line and not line.startswith(" "):
            references = False
    return needed, runpath, versions


def version_of(name, prefix):
    match = re.fullmatch(re.escape(prefix) + r"([0-9]+(?:\.[0-9]+)*)", name)
    return tuple(int(n) for n in match.group(1).split(".")) if match else None


# .NET's single-file bundle (src/installer/managed/Microsoft.NET.HostModel/Bundle: the header and its manifest).

def read_7bit_string(data, at):
    length, shift = 0, 0
    while True:
        b = data[at]
        at += 1
        length |= (b & 0x7f) << shift
        shift += 7
        if b < 0x80:
            break
    return data[at:at + length].decode("utf-8"), at + length


def bundle_entries(data):
    """{name: (kind, bytes)} of a single-file program's bundle, or None when it isn't one."""
    at = data.find(BUNDLE_SIGNATURE)
    if at < 8:
        return None
    header = struct.unpack_from("<q", data, at - 8)[0]
    if header <= 0 or header >= len(data):
        return None
    major, _, count = struct.unpack_from("<IIi", data, header)
    pos = header + 12
    _, pos = read_7bit_string(data, pos)
    if major >= 2:
        pos += 8 * 4 + 8
    entries = {}
    for _ in range(count):
        offset, size = struct.unpack_from("<qq", data, pos)
        pos += 16
        compressed = 0
        if major >= 6:
            compressed = struct.unpack_from("<q", data, pos)[0]
            pos += 8
        kind = data[pos]
        pos += 1
        name, pos = read_7bit_string(data, pos)
        body = zlib.decompress(data[offset:offset + compressed]) if compressed else data[offset:offset + size]
        entries[name] = (kind, body)
    return entries


def embedded_pdb(assembly):
    """The portable PDB embedded in an assembly (its debug directory's EmbeddedPortablePdb entry), or None."""
    at, size, _ = pe_directory(assembly, 6)
    for i in range(size // 28 if at is not None else 0):
        _, _, _, _, kind, length, _, raw = struct.unpack_from("<IIHHIIII", assembly, at + 28 * i)
        if kind == 17 and assembly[raw:raw + 4] == b"MPDB":
            return zlib.decompressobj(-15).decompress(assembly[raw + 8:raw + length])
    return None


def pdb_documents(pdb):
    """The source paths a portable PDB names (its Document table)."""
    version_length = struct.unpack_from("<I", pdb, 12)[0]
    at = 16 + version_length + 2
    count = struct.unpack_from("<H", pdb, at)[0]
    at += 2
    streams = {}
    for _ in range(count):
        offset, _ = struct.unpack_from("<II", pdb, at)
        end = pdb.index(b"\0", at + 8)
        streams[pdb[at + 8:end].decode()] = offset
        at = (end + 4) & ~3
    tables = streams["#~"]
    heap_sizes = pdb[tables + 6]
    present = struct.unpack_from("<Q", pdb, tables + 8)[0]
    at = tables + 24
    rows = {}
    for table in range(64):
        if present >> table & 1:
            rows[table] = struct.unpack_from("<I", pdb, at)[0]
            at += 4
    blob_size = 4 if heap_sizes & 4 else 2
    guid_size = 4 if heap_sizes & 2 else 2
    blobs = streams["#Blob"]

    def compressed(data, p):
        b = data[p]
        if b & 0x80 == 0:
            return b, p + 1
        if b & 0xc0 == 0x80:
            return ((b & 0x3f) << 8) | data[p + 1], p + 2
        return ((b & 0x1f) << 24) | (data[p + 1] << 16) | (data[p + 2] << 8) | data[p + 3], p + 4

    def blob(index):
        length, p = compressed(pdb, blobs + index)
        return pdb[p:p + length]

    # A standalone (embedded) PDB's #~ stream holds only the debug tables, the Document table (0x30) first.
    names = []
    row_size = 2 * blob_size + 2 * guid_size
    for r in range(rows.get(0x30, 0)):
        index = int.from_bytes(pdb[at + r * row_size:at + r * row_size + blob_size], "little")
        data = blob(index)
        separator, p, parts = data[:1].decode(), 1, []
        while p < len(data):
            part, p = compressed(data, p)
            parts.append(blob(part).decode("utf-8") if part else "")
        names.append(separator.join(parts))
    return names


# --- The rules ------------------------------------------------------------------------------------------------------

def check_listing(package, problems):
    expected, _ = read_listing(package.program, package.platform)
    for path in sorted(set(expected) - set(package.entries)):
        problems.append(f"missing: {path}")
    for path in sorted(set(package.entries) - set(expected)):
        problems.append(f"extra: {path}")
    for path in sorted(set(expected) & set(package.entries)):
        if expected[path] != package.entries[path]:
            problems.append(f"the executable bit: {path} is {package.entries[path]}, the listing says {expected[path]}")


def check_packs(package, problems):
    """M9 Q2, S9-A: the game's packs/ holds exactly the committed packs, byte for byte; the editor has none."""
    shipped = {e[len("packs/"):] for e, k in package.entries.items() if e.startswith("packs/") and k != "dir"}
    committed = {n for n in os.listdir(PACKS) if n.endswith(".pak")} if PROGRAMS[package.program]["packs"] else set()
    for name in sorted(committed - shipped):
        problems.append(f"packs: Assets/Packs/{name} isn't in packs/")
    for name in sorted(shipped - committed):
        problems.append(f"packs: packs/{name} isn't a committed pack")
    for name in sorted(shipped & committed):
        with open(os.path.join(PACKS, name), "rb") as f:
            if f.read() != package.read("packs/" + name):
                problems.append(f"packs: packs/{name} differs from Assets/Packs/{name}")


def check_texts(package, problems):
    """Q10: NOTICE.txt and licenses/ are Tools/Packaging's texts, byte for byte, all of them."""
    shipped, _ = read_licences()
    for text in sorted(shipped):
        if text not in package.entries:
            problems.append(f"licence: {text} isn't in the package")
            continue
        with open(os.path.join(PACKAGING, text), "rb") as f:
            if f.read() != package.read(text):
                problems.append(f"licence: {text} differs from Tools/Packaging/{text}")
    for entry, kind in package.entries.items():
        if entry.startswith("licenses/") and kind != "dir" and entry not in shipped:
            problems.append(f"licence: {entry} isn't a text licenses.txt names")


def check_vc_runtime(package, problems):
    """Q9: the Visual C++ runtime's three DLLs beside the Windows program, the ones vc-runtime.txt pins."""
    if package.platform != "windows":
        return
    for name, sha256 in read_vc_runtime().items():
        if name not in package.entries:
            problems.append(f"runtime: {name} (the Visual C++ runtime) isn't in the package")
        elif hashlib.sha256(package.read(name)).hexdigest() != sha256:
            problems.append(f"runtime: {name} isn't the Visual C++ runtime's (vc-runtime.txt)")


def check_program(package, problems):
    """The executable: its format (ELF x86-64; PE32+ x86-64 with the GUI subsystem, Q6), single-file (Q5) with the
    bundle the listing names, its own assemblies' symbols embedded with paths from the repo's root (Q7). Returns the
    bundle's entries."""
    name = package.program_file
    if name not in package.entries or package.entries[name] == "dir":
        problems.append(f"program: {name} isn't in the package")
        return {}
    data = package.read(name)
    if package.platform == "linux":
        if not (is_elf(data) and data[4] == 2 and struct.unpack_from("<H", data, 18)[0] == 62):
            problems.append(f"program: {name} isn't an x86-64 ELF program")
    elif not is_pe(data):
        problems.append(f"program: {name} isn't a Windows program")
    else:
        machine, magic, subsystem, _, _ = pe_headers(data)
        if machine != 0x8664 or magic != 0x20b:
            problems.append(f"program: {name} isn't a PE32+ x86-64 program")
        if subsystem != 2:
            problems.append(f"console: {name} has subsystem {subsystem}, not the GUI's (2): a console would open (Q6)")
    bundle = bundle_entries(data)
    if bundle is None:
        problems.append(f"single-file: {name} isn't a single-file program (no bundle, Q5)")
        return {}
    _, expected = read_listing(package.program, package.platform)
    for entry in sorted(expected - set(bundle)):
        problems.append(f"single-file: {entry} isn't inside {name}")
    for entry in sorted(set(bundle) - expected):
        problems.append(f"single-file: {entry} is inside {name}, the listing doesn't name it")
    own = PROGRAMS[package.program]["assembly"] + ".dll"
    # The repo's own assemblies, inside the program and beside it (the data library, M12-9), with their symbols: no
    # Source Link entry and no GitHub address in them (the repo's address stays out of the programs, the user, 2026-11-08).
    beside = {e: (1, package.read(e)) for e, k in package.entries.items() if k != "dir" and fnmatch.fnmatchcase(e, "Statventure*.dll")}
    for entry, (_, body) in sorted(list(bundle.items()) + list(beside.items())):
        if not fnmatch.fnmatchcase(entry, "Statventure*.dll"):
            continue
        if b"github" in body.lower():
            problems.append(f"repo address: {entry} names github")
        try:
            pdb = embedded_pdb(body)
        except (struct.error, ValueError, zlib.error):
            pdb = None
        if pdb is not None and (SOURCE_LINK in pdb or b'{"documents"' in pdb):
            problems.append(f"repo address: {entry}'s symbols hold a Source Link entry")
        if pdb is not None and b"github" in pdb.lower():
            problems.append(f"repo address: {entry}'s symbols name github")
    # The version the program reads at run time (its informational version, "<version>+<commit>"; ProgramInfo) is the
    # archive's.
    if own in bundle:
        body = bundle[own][1]
        # The attribute's value is a length-prefixed string (ECMA-335 II.23.3).
        found = next((m for m in re.finditer(rb"(" + VERSION.encode() + rb")\+[0-9a-f]{40}", body)
                      if m.start() > 0 and body[m.start() - 1] == len(m.group(0))), None)
        if found is None:
            problems.append(f"version: {own} records no version and commit")
        elif found.group(1).decode() != package.version:
            problems.append(f"version: {own} is version {found.group(1).decode()}, the archive's name says {package.version}")
    for entry, (kind, body) in sorted(bundle.items()):
        if not entry.endswith(".dll"):
            continue
        try:
            pdb = embedded_pdb(body)
        except (struct.error, ValueError, zlib.error):
            pdb = None
        if pdb is None:
            if entry in (own, "Statventure.dll"):
                problems.append(f"symbols: {entry} has no embedded symbols (Q7)")
            continue
        for document in pdb_documents(pdb):
            if not document.startswith("/_/"):
                problems.append(f"symbols: {entry} names {document}, not a path from the repo's root (/_/, Q7)")
                break
    return bundle


def check_natives(package, problems):
    """Every native library's needs met: in the folder (Linux: found through $ORIGIN) or from the system; Linux's
    glibc and libstdc++ versions no newer than the oldest systems the packages are for."""
    files = {e for e, k in package.entries.items() if k != "dir" and "/" not in e}
    lower = {f.lower() for f in files}
    for entry in sorted(files):
        data = package.read(entry)
        if package.platform == "linux" and is_elf(data):
            needed, runpath, versions = elf_needs(package.path(entry))
            for name in needed:
                if name in files:
                    if "$ORIGIN" not in runpath.split(":"):
                        problems.append(f"needs: {entry} needs {name} from its folder but has no $ORIGIN in its RUNPATH")
                elif name not in LINUX_SYSTEM:
                    problems.append(f"needs: {entry} needs {name}, which is neither in the folder nor the system's")
            for version in versions:
                glibc, cxx = version_of(version, "GLIBC_"), version_of(version, "GLIBCXX_")
                if glibc and glibc > GLIBC_MOST or cxx and cxx > GLIBCXX_MOST:
                    problems.append(f"needs: {entry} needs {version}, newer than GLIBC_2.34 and GLIBCXX_3.4.30")
        elif package.platform == "windows" and is_pe(data) and not is_il_only(data):
            # A native DLL's (or a mixed .NET one's) imports; an IL-only assembly's only import isn't a need (is_il_only).
            for name in pe_imports(data):
                if name.lower() not in lower and name.lower() not in WINDOWS_SYSTEM and not WINDOWS_UCRT.match(name.lower()):
                    problems.append(f"needs: {entry} imports {name}, which is neither in the folder nor Windows'")


def check_coverage(package, bundle, problems):
    """Every file of the package, and every file inside the program, covered by licenses.txt, with its texts there."""
    _, covers = read_licences()
    files = [e for e, k in package.entries.items() if k != "dir"] + ["bundle:" + e for e in bundle]
    for entry in sorted(files):
        for pattern, texts in covers:
            if fnmatch.fnmatchcase(entry, pattern.replace("{program}", package.program_file)):
                for text in texts:
                    if text not in package.entries:
                        problems.append(f"licence: {entry} is covered by {text}, which isn't in the package")
                break
        else:
            problems.append(f"licence: no line of licenses.txt covers {entry}")


def check(archive, work, run=True):
    """Every rule for one archive; the problems found (none: the package passes)."""
    folder = tempfile.mkdtemp(dir=work)
    try:
        package = unpack(archive, folder)
        problems = list(package.problems)
        if package.program is None or any(p.startswith("archive:") for p in problems):
            return problems, package
        check_listing(package, problems)
        check_packs(package, problems)
        check_texts(package, problems)
        check_vc_runtime(package, problems)
        bundle = check_program(package, problems)
        check_natives(package, problems)
        check_coverage(package, bundle, problems)
        if run and package.platform == "linux":
            run_program(package, work, problems)
        return problems, package
    finally:
        shutil.rmtree(folder, ignore_errors=True)


# --- Running the Linux packages -------------------------------------------------------------------------------------

def real_folders():
    """The state of this machine's game folder (whole) and Documents folder (top level), as mutate.py watches them."""
    data = os.environ.get("XDG_DATA_HOME", "")
    if not data.startswith("/"):
        data = os.path.join(os.path.expanduser("~"), ".local", "share")
    state = []
    for folder, whole in ((os.path.join(data, "stat-fick"), True), (os.path.expanduser("~/Documents"), False)):
        entries = []
        if os.path.isdir(folder):
            for root, folders, names in os.walk(folder):
                for name in folders + names:
                    info = os.lstat(os.path.join(root, name))
                    entries.append((os.path.relpath(os.path.join(root, name), folder), info.st_size, info.st_mtime_ns))
                if not whole:
                    break
        state.append((folder, os.path.isdir(folder), sorted(entries)))
    return state


def headless(command, env, scratch):
    """Runs a command with no display of the user's: in a headless Wayland compositor (cage on wlroots' headless
    backend) or, without cage, a virtual X server (xvfb-run). (exit code, output) or (None, why it couldn't)."""
    env = {k: v for k, v in env.items() if k not in ("DISPLAY", "WAYLAND_DISPLAY")}
    output, code = os.path.join(scratch, "output.txt"), os.path.join(scratch, "exit-code.txt")
    if shutil.which("cage"):
        env.update(WLR_BACKENDS="headless", WLR_LIBINPUT_NO_DEVICES="1", XDG_SESSION_TYPE="wayland",
                   CHECK_OUTPUT=output, CHECK_CODE=code)
        line = ["cage", "--", "sh", "-c", '"$@" > "$CHECK_OUTPUT" 2>&1; echo $? > "$CHECK_CODE"; kill -TERM $PPID', "sh"]
    elif shutil.which("xvfb-run"):
        env.update(CHECK_OUTPUT=output, CHECK_CODE=code)
        line = ["xvfb-run", "-a", "sh", "-c", '"$@" > "$CHECK_OUTPUT" 2>&1; echo $? > "$CHECK_CODE"', "sh"]
    else:
        return None, "no headless display: cage or xvfb-run is needed to run the Linux packages (or --no-run)"
    process = subprocess.Popen(line + command, env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
                               start_new_session=True)
    try:
        process.wait(timeout=RUN_SECONDS)
    except subprocess.TimeoutExpired:
        os.killpg(process.pid, signal.SIGKILL)
        process.wait()
        return None, f"it didn't end within {RUN_SECONDS} s"
    text = open(output, encoding="utf-8", errors="replace").read() if os.path.exists(output) else ""
    if not os.path.exists(code):
        return None, "the headless display ended before it: " + text[-500:]
    return int(open(code).read().strip() or "-1"), text


def png_colours(path):
    """How many distinct pixels a PNG has, up to 2 (8-bit RGB or RGBA, as the engine writes them); 0 if unreadable."""
    try:
        data = open(path, "rb").read()
        if data[:8] != b"\x89PNG\r\n\x1a\n":
            return 0
        at, idat, width = 8, b"", 0
        while at < len(data):
            length, kind = struct.unpack_from(">I4s", data, at)
            body = data[at + 8:at + 8 + length]
            if kind == b"IHDR":
                width, height, depth, colour = struct.unpack_from(">IIBB", body)
                if depth != 8 or colour not in (2, 6):
                    return 0
                step = 3 if colour == 2 else 4
            elif kind == b"IDAT":
                idat += body
            at += 12 + length
        raw = zlib.decompress(idat)
        stride = width * step
        previous = bytearray(stride)
        seen = set()
        for y in range(height):
            filter_type = raw[y * (stride + 1)]
            row = bytearray(raw[y * (stride + 1) + 1:(y + 1) * (stride + 1)])
            for i in range(stride):
                left = row[i - step] if i >= step else 0
                up = previous[i]
                corner = previous[i - step] if i >= step else 0
                if filter_type == 1:
                    row[i] = (row[i] + left) & 0xff
                elif filter_type == 2:
                    row[i] = (row[i] + up) & 0xff
                elif filter_type == 3:
                    row[i] = (row[i] + (left + up) // 2) & 0xff
                elif filter_type == 4:
                    p = left + up - corner
                    pa, pb, pc = abs(p - left), abs(p - up), abs(p - corner)
                    row[i] = (row[i] + (left if pa <= pb and pa <= pc else up if pb <= pc else corner)) & 0xff
            for x in range(0, stride, step):
                seen.add(bytes(row[x:x + step]))
                if len(seen) > 1:
                    return 2
            previous = row
        return len(seen)
    except (OSError, struct.error, zlib.error, ValueError, IndexError, UnboundLocalError):
        return 0


def run_program(package, work, problems):
    """The Linux program started headless on a scratch per-user folder, nothing audible: the game (until the port the
    stand-in) draws and quits with --capture; the editor, given a wrong argument, prints its usage and ends with 2."""
    scratch = tempfile.mkdtemp(dir=work)
    try:
        env = dict(os.environ, XDG_DATA_HOME=os.path.join(scratch, "player"), ALSOFT_DRIVERS="null")
        os.makedirs(env["XDG_DATA_HOME"])
        program = package.path(package.program_file)
        before = real_folders()
        if package.program == "stat-fick":
            capture = os.path.join(scratch, "capture.png")
            code, output = headless([program, "--capture", capture, "--frames", CAPTURE_FRAMES], env, scratch)
            if code is None:
                problems.append(f"run: {package.program_file} couldn't be run: {output}")
            elif code != 0:
                problems.append(f"run: {package.program_file} ended with {code}: {output[-800:]}")
            elif png_colours(capture) < 2:
                problems.append(f"run: {package.program_file} drew nothing (no capture, or one colour)")
        else:
            code, output = headless([program, "--player-folder", os.path.join(scratch, "player", "stat-fick"),
                                     "--not-an-option"], env, scratch)
            if code is None:
                problems.append(f"run: {package.program_file} couldn't be run: {output}")
            elif code != 2 or "usage:" not in output:
                problems.append(f"run: {package.program_file} given a wrong argument ended with {code}, not 2 with its "
                                f"usage: {output[-800:]}")
        if real_folders() != before:
            problems.append("run: the player's real folders changed while the program ran")
    finally:
        shutil.rmtree(scratch, ignore_errors=True)


# --- Commands -------------------------------------------------------------------------------------------------------

def make_listing(archive, work):
    """The listing an archive has, for Tools/Packaging/listings/ after a change meant to alter the packages."""
    folder = tempfile.mkdtemp(dir=work)
    try:
        package = unpack(archive, folder)
        if package.program is None:
            sys.exit("; ".join(package.problems))
        lines = [f"# The {package.program} package for {package.platform.capitalize()}: every path in its folder "
                 f"({package.program}-<version>/ in the archive) and every file inside its single-file program. Made by",
                 f"# check-packages.sh --listing (Docs/Packaging.md, \"How it works\", step 3); a package must match it."]
        for entry in sorted(package.entries):
            kind = package.entries[entry]
            lines.append(f"{kind} {entry}/" if kind == "dir" else f"{kind} {entry}")
        bundle = bundle_entries(package.read(package.program_file)) or {}
        lines += [f"bundle {name}" for name in sorted(bundle)]
        return "\n".join(lines) + "\n"
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def main(argv):
    run, self_test, listing, archives = True, False, False, []
    for argument in argv:
        if argument == "--no-run":
            run = False
        elif argument == "--self-test":
            self_test = True
        elif argument == "--listing":
            listing = True
        else:
            archives.append(os.path.abspath(argument))
    if not archives:
        sys.exit("usage: check_packages.py [--no-run | --self-test | --listing] <archive>...")
    work = tempfile.mkdtemp(prefix="check-packages.", dir=os.environ.get("CHECK_PACKAGES_WORK"))
    try:
        if listing:
            for archive in archives:
                sys.stdout.write(make_listing(archive, work))
            return 0
        if self_test:
            sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
            import check_packages_self_test as tests
            return tests.run(archives, work)
        failed = 0
        for archive in archives:
            if not os.path.isfile(archive):
                print(f"FAILED {archive}: no such archive")
                failed += 1
                continue
            problems, package = check(archive, work, run)
            size = os.path.getsize(archive)
            files = sum(1 for k in package.entries.values() if k != "dir")
            if problems:
                failed += 1
                print(f"FAILED {os.path.basename(archive)} ({len(problems)} problems):")
                for problem in problems:
                    print("  " + problem)
            else:
                ran = " (ran)" if run and package.platform == "linux" else ""
                print(f"ok {os.path.basename(archive)}: {size / 1048576:.1f} MB ({size} bytes), {files} files{ran}")
        return 1 if failed else 0
    finally:
        shutil.rmtree(work, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
