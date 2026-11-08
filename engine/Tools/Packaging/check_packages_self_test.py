"""check-packages.sh --self-test (Docs/Packaging.md, "How it works", step 3): each rule of check_packages.py shown able
to fail. Every archive given is unpacked afresh, broken one way (a file left out or added, a name, the folder, a pack,
a mode or owner, a licence text, the Visual C++ runtime, the program's format, subsystem, bundle, symbols or version,
a native library's needs, and on Linux the program's run), packed again and checked: the check must fail and name
the rule. The archive packed again unbroken must pass. Prints each break as killed or SURVIVED."""
import os
import shutil
import struct
import tarfile
import tempfile
import zipfile
import zlib

import check_packages as c


def repack(package, archive, top=None, modes=None, owner=0, links=()):
    """Packs an unpacked package again (its entries as they are now) into `archive`, its folder `top`."""
    top = top or package.top
    modes = modes or {}
    entries = sorted(package.entries.items())
    if archive.endswith(".zip"):
        with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED, compresslevel=1) as z:
            z.writestr(top + "/", b"")
            for entry, kind in entries:
                if kind == "dir":
                    z.writestr(f"{top}/{entry}/", b"")
                else:
                    z.write(package.path(entry), f"{top}/{entry}")
        return
    with tarfile.open(archive, "w:gz", compresslevel=1) as tar:
        def add(name, kind, path=None):
            info = tarfile.TarInfo(name)
            info.uid = info.gid = owner
            info.mtime = 0
            if kind == "dir":
                info.type = tarfile.DIRTYPE
                info.mode = modes.get(name, 0o755)
                tar.addfile(info)
            else:
                info.mode = modes.get(name, 0o755 if kind == "exec" else 0o644)
                info.size = os.path.getsize(path)
                with open(path, "rb") as f:
                    tar.addfile(info, f)
        add(top + "/", "dir")
        for entry, kind in entries:
            add(f"{top}/{entry}", kind, None if kind == "dir" else package.path(entry))
        for name, target in links:
            info = tarfile.TarInfo(f"{top}/{name}")
            info.type = tarfile.SYMTYPE
            info.linkname = target
            info.mode = 0o777
            tar.addfile(info)


def patch(package, entry, old, new, offset=None):
    """Replaces bytes in a file of the package: the first `old` (the same length as `new`), or at `offset`."""
    data = bytearray(package.read(entry))
    if offset is None:
        offset = bytes(data).index(old)
    data[offset:offset + len(new)] = new
    with open(package.path(entry), "wb") as f:
        f.write(data)


def flip(package, entry, offset):
    """One byte of a file changed."""
    patch(package, entry, None, bytes([package.read(entry)[offset] ^ 1]), offset)


def remove(package, entry):
    os.remove(package.path(entry))
    del package.entries[entry]


def add(package, entry, data, kind="file"):
    os.makedirs(os.path.dirname(package.path(entry)) or package.folder, exist_ok=True)
    with open(package.path(entry), "wb") as f:
        f.write(data)
    package.entries[entry] = kind


def script(package, text):
    """The program replaced by a shell script (for the run's rules)."""
    with open(package.path(package.program_file), "w") as f:
        f.write("#!/bin/sh\n" + text + "\n")
    os.chmod(package.path(package.program_file), 0o755)


def png(path, colours):
    """A 4 x 4 PNG of one colour, or of two."""
    def chunk(kind, body):
        return struct.pack(">I", len(body)) + kind + body + struct.pack(">I", zlib.crc32(kind + body))
    rows = b"".join(b"\0" + b"\x80\x40\x20" * 3 + (b"\x80\x40\x20" if colours == 1 else b"\x10\x20\x30") for _ in range(4))
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 4, 4, 8, 2, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(rows)) + chunk(b"IEND", b""))


def bundle_offset(data, name):
    """Where a file inside the single-file program starts in it (from the bundle's manifest)."""
    at = data.find(c.BUNDLE_SIGNATURE)
    header = struct.unpack_from("<q", data, at - 8)[0]
    major, _, count = struct.unpack_from("<IIi", data, header)
    _, pos = c.read_7bit_string(data, header + 12)
    pos += 8 * 4 + 8 if major >= 2 else 0
    for _ in range(count):
        offset = struct.unpack_from("<q", data, pos)[0]
        pos += 16 + (8 if major >= 6 else 0) + 1
        entry, pos = c.read_7bit_string(data, pos)
        if entry == name:
            return offset
    raise KeyError(name)


def debug_entries(data, base):
    """The file offsets of an assembly's debug directory entries (the assembly at `base` in `data`)."""
    assembly = data[base:]
    at, size, _ = c.pe_directory(assembly, 6)
    return [base + at + 28 * i for i in range(size // 28)]


def no_embedded_symbols(package, _):
    data = package.read(package.program_file)
    for entry in debug_entries(data, bundle_offset(data, "Statventure.dll")):
        if struct.unpack_from("<I", data, entry + 12)[0] == 17:
            patch(package, package.program_file, None, struct.pack("<I", 2), entry + 12)


def machine_path_in_symbols(package, _):
    """The engine's embedded symbols changed so their paths start /X/ instead of /_/ (the PDB recompressed in place)."""
    engine_symbols(package, b"\x01_", b"\x01X")


def source_link_in_symbols(package, _):
    """A Source Link entry in the engine's symbols: the C# language's GUID made Source Link's."""
    engine_symbols(package, bytes.fromhex("f862513fc607d311905300c04fa302a1"), c.SOURCE_LINK)


def github_in_symbols(package, _):
    """The engine's symbols naming github: their paths' part Engine made github."""
    engine_symbols(package, b"\x06Engine", b"\x06github")


def github_in_engine(package, _):
    """The engine's assembly naming github: its type name EngineInfo made githubInfo."""
    data = package.read(package.program_file)
    base = bundle_offset(data, "Statventure.dll")
    patch(package, package.program_file, None, b"githubInfo", data.index(b"EngineInfo", base))


def engine_symbols(package, old, new):
    """Every `old` in the engine's embedded symbols made `new` (the same length), the PDB recompressed in place."""
    data = package.read(package.program_file)
    for entry in debug_entries(data, bundle_offset(data, "Statventure.dll")):
        _, _, _, _, kind, length, _, raw = struct.unpack_from("<IIHHIIII", data, entry)
        if kind != 17:
            continue
        base = bundle_offset(data, "Statventure.dll")
        start = base + raw
        pdb = zlib.decompressobj(-15).decompress(data[start + 8:start + length])
        if old not in pdb:
            raise RuntimeError(f"the engine's symbols hold no {old!r}")
        pdb = pdb.replace(old, new)
        packer = zlib.compressobj(9, zlib.DEFLATED, -15)
        squeezed = packer.compress(pdb) + packer.flush()
        if len(squeezed) > length - 8:
            raise RuntimeError("the changed symbols don't fit in place")
        patch(package, package.program_file, None, squeezed + b"\0" * (length - 8 - len(squeezed)), start + 8)
        patch(package, package.program_file, None, struct.pack("<I", len(squeezed) + 8), entry + 16)


def renamed_inside(package, _):
    data = package.read(package.program_file)
    header = struct.unpack_from("<q", data, data.find(c.BUNDLE_SIGNATURE) - 8)[0]
    at = data.index(b"\x0eSystem.Xml.dll", header)
    patch(package, package.program_file, None, b"\x0eXystem.Xml.dll", at)


def pe_field(package, delta, value):
    data = package.read(package.program_file)
    at = struct.unpack_from("<I", data, 0x3c)[0]
    patch(package, package.program_file, None, value, at + delta)


def breaks():
    """(label, applies to (program, platform), break, problems the check must report, run the program)."""
    L, W = "linux", "windows"
    native = {L: "libzlib.so.1", W: "zlib.dll"}
    return [
        ("a licence text left out", None, lambda p, x: remove(p, "licenses/openal-soft-COPYING.txt"),
         ["missing: licenses/openal-soft-COPYING.txt", "licence: licenses/openal-soft-COPYING.txt isn't in the package",
          "is covered by licenses/openal-soft-COPYING.txt, which isn't in the package"], False),
        ("NOTICE.txt left out", None, lambda p, x: remove(p, "NOTICE.txt"),
         ["missing: NOTICE.txt", "licence: NOTICE.txt isn't in the package"], False),
        ("a native library left out", None, lambda p, x: remove(p, native[p.platform]),
         ["missing: " + "{native}"], False),
        ("a file added", None, lambda p, x: add(p, "notes.txt", b"hello\n"),
         ["extra: notes.txt", "licence: no line of licenses.txt covers notes.txt"], False),
        ("a symbols file kept", None, lambda p, x: add(p, "libHarfBuzzSharp.pdb", b"pdb"), ["extra: libHarfBuzzSharp.pdb"], False),
        ("a licence text not in licenses.txt", None, lambda p, x: add(p, "licenses/OTHER.txt", b"text\n"),
         ["extra: licenses/OTHER.txt", "licence: licenses/OTHER.txt isn't a text licenses.txt names"], False),
        ("the program not renamed", None,
         lambda p, x: (os.rename(p.path(p.program_file), p.path(c.PROGRAMS[p.program]["assembly"] + (".exe" if p.platform == W else ""))),
                       p.entries.__setitem__(c.PROGRAMS[p.program]["assembly"] + (".exe" if p.platform == W else ""), p.entries.pop(p.program_file))),
         ["missing: {program}", "program: {program} isn't in the package"], False),
        ("the folder without the version", None, lambda p, x: x.update(top=p.program), ["folder: "], False),
        ("the archive's name with a zero-padded day", None, lambda p, x: x.update(name=p.name.replace(p.version, "2026.11.08.1")),
         ["name: "], False),
        ("the archive's name with another version", None,
         lambda p, x: x.update(name=p.name.replace(p.version, "2026.11.9.1"), top=f"{p.program}-2026.11.9.1"),
         ["version: "], False),
        ("a pack changed", ("stat-fick", None), lambda p, x: flip(p, "packs/ui.pak", 100),
         ["packs: packs/ui.pak differs from Assets/Packs/ui.pak"], False),
        ("a pack left out", ("stat-fick", None), lambda p, x: remove(p, "packs/audio.pak"),
         ["packs: Assets/Packs/audio.pak isn't in packs/"], False),
        ("a pack in the editor's", ("statventure-editor", None),
         lambda p, x: add(p, "packs/ui.pak", open(os.path.join(c.PACKS, "ui.pak"), "rb").read()),
         ["packs: packs/ui.pak isn't a committed pack"], False),
        ("NOTICE.txt changed", None, lambda p, x: add(p, "NOTICE.txt", p.read("NOTICE.txt") + b"\n"),
         ["licence: NOTICE.txt differs from Tools/Packaging/NOTICE.txt"], False),
        ("the program without its executable bit", (None, L), lambda p, x: p.entries.__setitem__(p.program_file, "file"),
         ["the executable bit: {program} is file"], False),
        ("a library without its executable bit", (None, L), lambda p, x: p.entries.__setitem__("libglfw.so.3", "file"),
         ["the executable bit: libglfw.so.3 is file"], False),
        ("a text with the executable bit", (None, L), lambda p, x: p.entries.__setitem__("NOTICE.txt", "exec"),
         ["the executable bit: NOTICE.txt is exec"], False),
        ("a file of mode 600", (None, L), lambda p, x: x.update(modes={f"{p.top}/NOTICE.txt": 0o600}), ["mode: "], False),
        ("a folder of mode 700", (None, L), lambda p, x: x.update(modes={f"{p.top}/licenses": 0o700}), ["mode: "], False),
        ("entries owned by 1000", (None, L), lambda p, x: x.update(owner=1000), ["owner: "], False),
        ("a symbolic link", (None, L), lambda p, x: x.update(links=[("libglfw.so.9", "libglfw.so.3")]), ["entry: "], False),
        ("the Visual C++ runtime left out", (None, W), lambda p, x: remove(p, "vcruntime140_1.dll"),
         ["runtime: vcruntime140_1.dll (the Visual C++ runtime) isn't in the package", "imports VCRUNTIME140_1.dll"], False),
        ("the Visual C++ runtime changed", (None, W), lambda p, x: flip(p, "msvcp140.dll", 2000),
         ["runtime: msvcp140.dll isn't the Visual C++ runtime's"], False),
        ("the console subsystem", (None, W), lambda p, x: pe_field(p, 24 + 68, struct.pack("<H", 3)), ["console: "], False),
        ("a 32-bit program", (None, W), lambda p, x: pe_field(p, 4, struct.pack("<H", 0x14c)),
         ["isn't a PE32+ x86-64 program"], False),
        ("an ARM program", (None, L), lambda p, x: patch(p, p.program_file, None, struct.pack("<H", 183), 18),
         ["isn't an x86-64 ELF program"], False),
        ("not a single-file program", None,
         lambda p, x: patch(p, p.program_file, None, b"\0", p.read(p.program_file).find(c.BUNDLE_SIGNATURE)),
         ["single-file: {program} isn't a single-file program"], False),
        ("a file inside the program renamed", None, renamed_inside,
         ["single-file: System.Xml.dll isn't inside", "single-file: Xystem.Xml.dll is inside",
          "licence: no line of licenses.txt covers bundle:Xystem.Xml.dll"], False),
        ("the engine's symbols not embedded", None, no_embedded_symbols, ["symbols: Statventure.dll has no embedded symbols"], False),
        ("a machine path in the engine's symbols", None, machine_path_in_symbols,
         ["symbols: Statventure.dll names /X/"], False),
        ("a Source Link entry in the engine's symbols", None, source_link_in_symbols,
         ["repo address: Statventure.dll's symbols hold a Source Link entry"], False),
        ("github in the engine's symbols", None, github_in_symbols, ["repo address: Statventure.dll's symbols name github"], False),
        ("github in the engine", None, github_in_engine, ["repo address: Statventure.dll names github"], False),
        ("a library needing a file not there", (None, L), lambda p, x: patch(p, "libpng18.so.18", b"libzlib.so.1\0", b"libzlib.so.9\0"),
         ["needs: libpng18.so.18 needs libzlib.so.9"], False),
        ("a library without $ORIGIN", (None, L), lambda p, x: patch(p, "libfreetype.so.6", b"$ORIGIN\0", b"$ORIGXN\0"),
         ["needs: libfreetype.so.6 needs libpng18.so.18 from its folder but has no $ORIGIN"], False),
        ("a library needing a newer glibc", (None, L), lambda p, x: patch(p, "libglfw.so.3", b"GLIBC_2.34\0", b"GLIBC_2.39\0"),
         ["needs: libglfw.so.3 needs GLIBC_2.39"], False),
        ("a library needing a newer libstdc++", (None, L), lambda p, x: patch(p, "libKtx.so", b"GLIBCXX_3.4.30\0", b"GLIBCXX_3.4.32\0"),
         ["needs: libKtx.so needs GLIBCXX_3.4.32"], False),
        ("a library importing a DLL not there", (None, W), lambda p, x: patch(p, "vma.dll", b"VCRUNTIME140_1.dll", b"VCRUNTIME140_9.dll"),
         ["needs: vma.dll imports VCRUNTIME140_9.dll"], False),
        ("the game drawing nothing", ("stat-fick", L), lambda p, x: script(p, "exit 0"),
         ["run: {program} drew nothing"], True),
        ("the game drawing one colour", ("stat-fick", L),
         lambda p, x: script(p, f"cp '{x['flat']}' \"$2\""), ["run: {program} drew nothing"], True),
        ("the game failing", ("stat-fick", L), lambda p, x: script(p, "exit 3"), ["run: {program} ended with 3"], True),
        ("the game writing to the player's folder", ("stat-fick", L),
         lambda p, x: (script(p, f"mkdir -p '{x['real']}/stat-fick' && touch '{x['real']}/stat-fick/settings.json'; "
                                 f"cp '{x['picture']}' \"$2\""), x.update(fake_real=True)),
         ["run: the player's real folders changed"], True),
        ("the editor taking a wrong argument", ("statventure-editor", L), lambda p, x: script(p, "exit 0"),
         ["run: {program} given a wrong argument ended with 0"], True),
        ("the editor without its usage", ("statventure-editor", L), lambda p, x: script(p, "exit 2"),
         ["run: {program} given a wrong argument ended with 2"], True),
    ]


def run(archives, work):
    survived = total = 0
    flat = os.path.join(work, "flat.png")
    png(flat, 1)
    picture = os.path.join(work, "picture.png")
    png(picture, 2)
    for archive in archives:
        # The archive packed again unbroken passes (so each break below is the only change).
        folder = tempfile.mkdtemp(dir=work)
        package = c.unpack(archive, folder)
        control = os.path.join(tempfile.mkdtemp(dir=work), package.name)
        repack(package, control)
        problems, _ = c.check(control, work, run=False)
        if problems:
            print(f"FAILED {package.name}: packed again unbroken, it fails the check: {problems}")
            return 1
        shutil.rmtree(folder)
        for label, scope, apply, expected, run_it in breaks():
            if scope and (scope[0] not in (None, package.program) or scope[1] not in (None, package.platform)):
                continue
            total += 1
            folder = tempfile.mkdtemp(dir=work)
            broken = c.unpack(archive, folder)
            real = tempfile.mkdtemp(dir=work)
            context = {"flat": flat, "real": real, "picture": picture}
            try:
                apply(broken, context)
            except (RuntimeError, ValueError, KeyError) as error:
                print(f"SURVIVED {broken.name}: {label}: the break couldn't be made ({error})")
                survived += 1
                continue
            name = context.get("name", broken.name)
            target = os.path.join(tempfile.mkdtemp(dir=work), name)
            repack(broken, target, context.get("top"), context.get("modes"), context.get("owner", 0), context.get("links", ()))
            saved = os.environ.get("XDG_DATA_HOME")
            if context.get("fake_real"):
                os.environ["XDG_DATA_HOME"] = real
            try:
                problems, _ = c.check(target, work, run=run_it)
            finally:
                if saved is None:
                    os.environ.pop("XDG_DATA_HOME", None)
                else:
                    os.environ["XDG_DATA_HOME"] = saved
            wanted = [e.replace("{program}", broken.program_file).replace("{native}", {"linux": "libzlib.so.1", "windows": "zlib.dll"}[broken.platform])
                      for e in expected]
            missing = [w for w in wanted if not any(w in p for p in problems)]
            if missing:
                survived += 1
                print(f"SURVIVED {package.name}: {label}: no problem saying {missing} (found: {problems})")
            else:
                print(f"killed {package.name}: {label}")
            shutil.rmtree(folder, ignore_errors=True)
            shutil.rmtree(real, ignore_errors=True)
    print(f"{total} breaks, {total - survived} caught" + (f", {survived} NOT CAUGHT" if survived else ""))
    return 1 if survived else 0
