using System.Net;
using System.Text;
using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Dom;

namespace Statventure.UI;

/// <summary>A component the programs ship, as the credits name it.</summary>
/// <param name="Name">Its name and version.</param>
/// <param name="Licence">The licence it is used under.</param>
/// <param name="Copyrights">Its copyright lines, each as the licence text states it (none where the text states none).</param>
/// <param name="Text">The shipped licence text that holds its licence and copyright lines (<c>licenses/…</c> beside the program).</param>
/// <param name="Notes">Lines it must also carry: FreeType's credit, where OpenAL Soft's source is.</param>
public sealed record CreditedComponent(string Name, string Licence, IReadOnlyList<string> Copyrights, string Text, IReadOnlyList<string>? Notes = null);

/// <summary>
/// The credits view (the BRUTAL License v1.0, 2.1; Q11, Q30; Docs/Packaging.md, "The BRUTAL logo and the credits"):
/// one engine view for both programs, which open it from their About buttons (the editor's top bar, the sandbox's
/// page; M12 steps 5 and 6). It shows the program's name and version (<see cref="ProgramInfo"/>; no commit, Q3),
/// "Built with BRUTAL" with BRUTAL's <c>NOTICE.txt</c> verbatim (its first line BRUTAL's copyright line), every
/// component the downloads ship with its licence and copyright lines (<see cref="Components"/>: the components the
/// shipped licence texts name, FreeType's required credit and OpenAL Soft's source among them), and where the full
/// texts are (<c>licenses/</c> and <c>NOTICE.txt</c> beside the program). In the editor's dark look (Q23) and the
/// engine's Lato (<see cref="EngineFonts"/>). <see cref="Markup"/> and <see cref="Css"/> give the content for a dialog
/// of a program's own; an instance is a whole document that scrolls.
/// </summary>
public sealed class Credits : IDisposable
{
    /// <summary>BRUTAL's copyright line (NOTICE.txt's first line; the licence's 2.1).</summary>
    public const string BrutalCopyright = "BRUTAL game development framework is Copyright (c) 2024 Ahwoo Limited.";

    /// <summary>The line pointing at the full texts.</summary>
    public const string LicencesLine = "The full texts of these licences are in the licenses folder beside the program (BRUTAL's in licenses/BRUTAL-LICENSE.txt), and BRUTAL's notice in NOTICE.txt.";

    static readonly Lazy<string> NoticeText = new(() => Encoding.UTF8.GetString(EngineFonts.Resource("brutal-notice.txt")));

    /// <summary>BRUTAL's <c>NOTICE.txt</c>, as every BRUTAL package gives it (the licence's 2.3).</summary>
    public static string Notice => NoticeText.Value;

    /// <summary>
    /// The shipped components, in the credits' order: BRUTAL's libraries with what each contains (as each package's
    /// third-party notices list them), then HarfBuzz, .NET and its libraries, then what the engine itself embeds besides
    /// BRUTAL's logo: the fonts and Chromium's two search field images.
    /// </summary>
    public static IReadOnlyList<CreditedComponent> Components { get; } =
    [
        new("FreeType 2.14.3", "FreeType License (FTL)",
            ["Copyright 1996-2002, 2006 by David Turner, Robert Wilhelm, and Werner Lemberg"], "licenses/FTL.txt",
            ["Portions of this software are copyright © 2026 The FreeType Project (https://freetype.org). All rights reserved.",
             "This software is based in part of the work of the FreeType Team."]),
        new("BDF font driver (in FreeType)", "MIT License",
            ["Copyright (C) 2001-2002 by Francesco Zappa Nardelli", "Copyright 2000 Computing Research Labs, New Mexico State University",
             "Copyright 2001-2002, 2011 Francesco Zappa Nardelli"], "licenses/BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt"),
        new("PCF font driver (in FreeType)", "MIT License",
            ["Copyright (C) 2000 by Francesco Zappa Nardelli"], "licenses/BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt"),
        new("libpng 1.8.0.git", "PNG Reference Library License version 2",
            ["Copyright (c) 1995-2026 The PNG Reference Library Authors.", "Copyright (c) 2018-2026 Cosmin Truta.",
             "Copyright (c) 2000-2002, 2004, 2006-2018 Glenn Randers-Pehrson.", "Copyright (c) 1996-1997 Andreas Dilger.",
             "Copyright (c) 1995-1996 Guy Eric Schalnat, Group 42, Inc."], "licenses/BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt"),
        new("zlib 1.3.2.1", "zlib License",
            ["Copyright (C) 1995-2026 Jean-loup Gailly and Mark Adler"], "licenses/BRUTAL-ZLIB-THIRD-PARTY-NOTICES.txt"),
        new("GLFW 3.5.1", "zlib License",
            ["Copyright (c) 2002-2006 Marcus Geelnard", "Copyright (c) 2006-2019 Camilla Löwy"], "licenses/BRUTAL-GLFW-THIRD-PARTY-NOTICES.txt"),
        new("SDL_GameControllerDB (in GLFW)", "zlib License",
            ["Copyright (C) 1997-2013 Sam Lantinga <slouken@libsdl.org>"], "licenses/BRUTAL-GLFW-THIRD-PARTY-NOTICES.txt"),
        new("Wayland protocol descriptions (in GLFW, Linux)", "MIT License; pointer-gestures-unstable-v1 under the HPND (sell variant) licence",
            ["Copyright © 2022 Kenny Levinsen", "Copyright © 2015 Samsung Electronics Co., Ltd", "Copyright © 2014 Jonas Ådahl",
             "Copyright © 2015 Red Hat Inc.", "Copyright © 2015, 2021 Red Hat Inc.", "Copyright © 2013-2016 Collabora, Ltd.",
             "Copyright © 2008-2011 Kristian Høgsberg", "Copyright © 2010-2011 Intel Corporation", "Copyright © 2012-2013 Collabora, Ltd.",
             "Copyright © 2020 Aleix Pol Gonzalez <aleixpol@kde.org>", "Copyright © 2020 Carlos Garnacho <carlosg@gnome.org>",
             "Copyright © 2018 Simon Ser", "Copyright © 2008-2013 Kristian Høgsberg", "Copyright © 2013 Rafael Antognolli",
             "Copyright © 2013 Jasper St. Pierre", "Copyright © 2010-2013 Intel Corporation", "Copyright © 2015-2017 Samsung Electronics Co., Ltd",
             "Copyright © 2015-2017 Red Hat Inc."], "licenses/BRUTAL-GLFW-THIRD-PARTY-NOTICES.txt"),
        new("OpenAL Soft 1.25.2", "GNU Library General Public License, version 2 or later (LGPL-2.0-or-later)",
            [], "licenses/BRUTAL-OPENAL-THIRD-PARTY-NOTICES.txt",
            ["Its source: https://github.com/kcat/openal-soft at commit dd4e07de0fe73d8c0326c4502f63e71da8ef268b, unmodified; it is the separate library libopenal.so.1 (OpenAL32.dll on Windows) beside the program.",
             "The licence's text: licenses/openal-soft-COPYING.txt."]),
        new("Spherical-Harmonic-Transform (in OpenAL Soft)", "BSD 3-Clause License",
            ["Copyright (c) 2015, Archontis Politis", "Copyright (c) 2019, Anis A. Hireche", "Copyright (c) 2019, Christopher Robinson"],
            "licenses/openal-soft-BSD-3Clause.txt"),
        new("PFFFT (in OpenAL Soft)", "PFFFT's licence (BSD-style)",
            ["Copyright (c) 2023 Christopher Robinson", "Copyright (c) 2013 Julien Pommier ( pommier@modartt.com )",
             "Copyright (c) 2004 the University Corporation for Atmospheric Research (\"UCAR\"). All rights reserved."],
            "licenses/openal-soft-LICENSE-pffft.txt"),
        new("{fmt} 11.2.0 (in OpenAL Soft)", "MIT License",
            ["Copyright (c) 2012 - present, Victor Zverovich and {fmt} contributors"], "licenses/openal-soft-fmt-LICENSE.txt"),
        new("GSL: Guidelines Support Library 4.2.0 (in OpenAL Soft)", "MIT License",
            ["Copyright (c) 2015 Microsoft Corporation. All rights reserved."], "licenses/openal-soft-gsl-LICENSE.txt"),
        new("stb_image 2.30 and stb_image_write 1.16", "MIT License or the Unlicense",
            ["Copyright (c) 2017 Sean Barrett"], "licenses/BRUTAL-STB-THIRD-PARTY-NOTICES.txt"),
        new("Vulkan API registry and C headers 1.4.335", "Apache License 2.0",
            ["Copyright 2015-2025 The Khronos Group Inc."], "licenses/BRUTAL-VULKAN-THIRD-PARTY-NOTICES.txt"),
        new("Vulkan API specification reference pages 1.4.335", "Creative Commons Attribution 4.0 International",
            ["Copyright 2014-2025 The Khronos Group Inc."], "licenses/BRUTAL-VULKAN-ABSTRACTIONS-THIRD-PARTY-NOTICES.txt"),
        new("Vulkan Memory Allocator 3.4.0-development", "MIT License",
            ["Copyright (c) 2017-2025 Advanced Micro Devices, Inc. All rights reserved."], "licenses/BRUTAL-VULKAN-VMA-THIRD-PARTY-NOTICES.txt"),
        new("GLI 0.8.3", "MIT License or the Happy Bunny License",
            ["Copyright (c) 2010 - 2020 G-Truc Creation"], "licenses/BRUTAL-GLI-THIRD-PARTY-NOTICES.txt"),
        new("OpenGL Mathematics (GLM) 0.9.9.9 (in GLI)", "MIT License or the Happy Bunny License",
            ["Copyright (c) 2005 - G-Truc Creation"], "licenses/BRUTAL-GLI-THIRD-PARTY-NOTICES.txt"),
        new("glTF 2.0 specification JSON schemas", "The Khronos Group's specification copyright licence",
            ["Copyright 2012-2021 The Khronos Group Inc."], "licenses/BRUTAL-GLTF-THIRD-PARTY-NOTICES.txt"),
        new("KTX-Software 4.4.0 (libktx)", "Apache License 2.0", [], "licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        new("Basis Universal 1.16 (in libktx)", "Apache License 2.0", [], "licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        new("miniz 1.15, Basis Universal's fork (in libktx)", "Apache License 2.0",
            ["Copyright (C) 2019-2021 Binomial LLC. All Rights Reserved."], "licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        new("Zstandard 1.5.5 (in libktx)", "BSD 3-Clause License",
            ["Copyright (c) 2016-present, Facebook, Inc. All rights reserved."], "licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        new("astc-encoder 5.3.0 (in libktx)", "Apache License 2.0", [], "licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        new("dfdutils (in libktx)", "Apache License 2.0", [], "licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        new("Vulkan headers (in libktx)", "Apache License 2.0", [], "licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        new("HarfBuzzSharp 14.2.1", "MIT License",
            ["Copyright (c) 2015-2016 Xamarin, Inc.", "Copyright (c) 2017-2018 Microsoft Corporation."], "licenses/HarfBuzzSharp-LICENSE.txt"),
        new("HarfBuzz (in HarfBuzzSharp)", "the \"Old MIT\" licence",
            ["Copyright © 2010,2011,2012 Google, Inc.", "Copyright © 2012 Mozilla Foundation", "Copyright © 2011 Codethink Limited",
             "Copyright © 2008,2010 Nokia Corporation and/or its subsidiary(-ies)", "Copyright © 2009 Keith Stribley",
             "Copyright © 2009 Martin Hosken and SIL International", "Copyright © 2007 Chris Wilson", "Copyright © 2006 Behdad Esfahbod",
             "Copyright © 2005 David Turner", "Copyright © 2004,2007,2008,2009,2010 Red Hat, Inc.", "Copyright © 1998-2004 David Turner and Werner Lemberg"],
            "licenses/HarfBuzzSharp-THIRD-PARTY-NOTICES.txt"),
        new(".NET 10 runtime and libraries", "MIT License",
            ["Copyright (c) .NET Foundation and Contributors"], "licenses/dotnet-LICENSE.txt",
            ["The third-party components .NET contains are named in licenses/dotnet-THIRD-PARTY-NOTICES.txt."]),
        new("Brotli (in .NET)", "MIT License",
            ["Copyright (c) 2009, 2010, 2013-2016 by the Brotli Authors."], "licenses/dotnet-THIRD-PARTY-NOTICES.txt"),
        new("zlib-ng (in .NET)", "zlib License",
            ["(C) 1995-2024 Jean-loup Gailly and Mark Adler"], "licenses/dotnet-THIRD-PARTY-NOTICES.txt"),
        new("Microsoft.Extensions (logging, options, dependency injection, object pool)", "MIT License",
            ["Copyright (c) .NET Foundation and Contributors"], "licenses/dotnet-LICENSE.txt",
            ["The third-party notices of its object pool: licenses/Microsoft.Extensions.ObjectPool-THIRD-PARTY-NOTICES.txt."]),
        new(".NET Community Toolkit (CommunityToolkit.HighPerformance 8.4.2)", "MIT License",
            ["Copyright © .NET Foundation and Contributors"], "licenses/CommunityToolkit.HighPerformance-License.md",
            ["The third-party notices it carries: licenses/CommunityToolkit.HighPerformance-ThirdPartyNotices.txt."]),
        new("Lato", "SIL Open Font License 1.1",
            ["Copyright (c) 2010-2015, Łukasz Dziedzic (dziedzic@typoland.com), with Reserved Font Name Lato."], "licenses/Lato-OFL.txt"),
        new("STIX Two", "SIL Open Font License 1.1",
            ["Copyright 2001-2021 The STIX Fonts Project Authors (https://github.com/stipub/stixfonts), with Reserved Font Name \"TM Math\". STIX Fonts™ is a trademark of The Institute of Electrical and Electronics Engineers, Inc."],
            "licenses/STIX-OFL.txt"),
        new("Chromium 152 (the search field's cancel button images, in the engine)", "BSD 3-Clause License",
            ["Copyright 2015 The Chromium Authors"], "licenses/Chromium-LICENSE.txt"),
    ];

    /// <summary>The credits' stylesheet (the editor's dark look, Lato 13 px; every rule under <c>.credits</c>).</summary>
    public const string Css = """
        .credits { max-width: 640px; margin: 0 auto; padding: 24px 28px 32px; color: #eeeae1; font: 13px/20px Lato; }
        .credits h1, .credits h2, .credits h3, .credits p { margin: 0; font-weight: 400; }
        .credits h1 { font-size: 20px; line-height: 28px; font-weight: 700; }
        .credits h2 { margin: 24px 0 10px; padding-bottom: 4px; border-bottom: 1px solid #3a3d38; font-size: 15px; font-weight: 700; }
        .credits h3 { font-weight: 700; }
        .credits .version, .credits .licence { color: #aaa9a1; }
        .credits .notice { white-space: pre-wrap; }
        .credits .component { margin-bottom: 12px; }
        """;

    /// <summary>The stylesheet of the whole view: the dark ground and a scroller holding <see cref="Css"/>'s content.</summary>
    public const string ViewCss = """
        html, body { height: 100%; margin: 0; }
        html, body { background: #1a1c1a; }
        body { overflow: hidden; }
        .credits-view { height: 100%; overflow: auto; scrollbar-color: #7d817b #292c28; }
        """;

    /// <summary>The credits of <paramref name="title"/> (the program's name as it shows it: "Statventure Editor") at <paramref name="program"/>'s version.</summary>
    public static string Markup(string title, ProgramInfo program)
    {
        static string E(string text) => WebUtility.HtmlEncode(text);
        var html = new StringBuilder();
        html.Append("""<div class="credits">""");
        html.Append($"""<h1>{E(title)}</h1><p class="version">Version {E(program.Version)}</p>""");
        html.Append($"""<h2>Built with BRUTAL</h2><p class="notice">{E(Notice)}</p>""");
        html.Append("<h2>Components</h2>");
        foreach (var component in Components)
        {
            html.Append($"""<div class="component"><h3>{E(component.Name)}</h3><p class="licence">{E(component.Licence)}</p>""");
            foreach (var line in component.Copyrights.Concat(component.Notes ?? []))
                html.Append($"<p>{E(line)}</p>");
            html.Append("</div>");
        }
        html.Append($"""<h2>Licences</h2><p>{E(LicencesLine)}</p>""");
        html.Append("</div>");
        return html.ToString();
    }

    readonly FontLibrary _fonts = new();

    /// <summary>The credits as a whole document (its own font library with the engine's Lato).</summary>
    public Credits(string title, ProgramInfo program)
    {
        EngineFonts.AddTo(_fonts);
        Document = new UiDocument(_fonts);
        Document.AddStylesheet(ViewCss, "Statventure.UI.Credits.View");
        Document.AddStylesheet(Css, "Statventure.UI.Credits");
        Dom.Markup.SetInnerHtml(Document.Body, $"""<div class="credits-view">{Markup(title, program)}</div>""");
    }

    /// <summary>The view's document.</summary>
    public UiDocument Document { get; }

    /// <summary>Its scroller (the content's box), for scrolling it.</summary>
    public UiElement Scroller => Document.QuerySelector(".credits-view")!;

    public void Dispose() => _fonts.Dispose();
}
