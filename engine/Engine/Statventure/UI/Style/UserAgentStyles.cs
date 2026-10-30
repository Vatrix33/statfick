namespace Statventure.UI.Style;

/// <summary>The engine's default styles for HTML-like elements (the parts of Chromium's html.css the UI uses).</summary>
public static class UserAgentStyles
{
    public const string Css = """
        html, address, blockquote, body, center, dd, details, dialog, dir, div, dl, dt, fieldset, figcaption, figure,
        footer, form, header, hgroup, hr, legend, main, menu, nav, ol, p, pre, search, section, article, aside,
        h1, h2, h3, h4, h5, h6, summary, ul { display: block; }
        head, script, style, template, title, meta, link, base, datalist, noscript, param, rp, area { display: none; }
        [hidden]:not([hidden="until-found" i]) { display: none; }
        [inert] { interactivity: inert !important; }
        li { display: list-item; }
        body { margin: 8px; }
        p { margin-top: 1em; margin-bottom: 1em; }
        blockquote { margin: 1em 40px; }
        figure { margin: 1em 40px; }
        h1 { font-size: 2em; margin-top: 0.67em; margin-bottom: 0.67em; font-weight: bold; }
        h2 { font-size: 1.5em; margin-top: 0.83em; margin-bottom: 0.83em; font-weight: bold; }
        h3 { font-size: 1.17em; margin-top: 1em; margin-bottom: 1em; font-weight: bold; }
        h4 { margin-top: 1.33em; margin-bottom: 1.33em; font-weight: bold; }
        h5 { font-size: 0.83em; margin-top: 1.67em; margin-bottom: 1.67em; font-weight: bold; }
        h6 { font-size: 0.67em; margin-top: 2.33em; margin-bottom: 2.33em; font-weight: bold; }
        ul, menu, dir { list-style-type: disc; margin-top: 1em; margin-bottom: 1em; padding-left: 40px; }
        ol { list-style-type: decimal; margin-top: 1em; margin-bottom: 1em; padding-left: 40px; }
        ul ul, ol ul, menu ul, ul menu { list-style-type: circle; }
        ol ol ul, ol ul ul, ul ol ul, ul ul ul { list-style-type: square; }
        ul ul, ul ol, ol ul, ol ol, ul menu, menu ul { margin-top: 0; margin-bottom: 0; }
        dl { margin-top: 1em; margin-bottom: 1em; }
        dd { margin-left: 40px; }
        pre { font-family: monospace; white-space: pre; margin: 1em 0; }
        hr { color: gray; margin: 0.5em auto; border-style: inset; border-width: 1px; overflow: hidden; }
        strong, b { font-weight: bolder; }
        em, i, cite, var, dfn, address { font-style: italic; }
        small { font-size: smaller; }
        big { font-size: larger; }
        sub { vertical-align: sub; font-size: smaller; }
        sup { vertical-align: super; font-size: smaller; }
        u, ins { text-decoration: underline; }
        s, del, strike { text-decoration: line-through; }
        a:any-link, a[href] { color: #0000ee; text-decoration: underline; cursor: pointer; }
        label { cursor: default; }
        legend { padding-left: 2px; padding-right: 2px; border: none; }
        fieldset { margin-left: 2px; margin-right: 2px; padding: 0.35em 0.75em 0.625em; border: 2px groove #c0c0c0; min-width: min-content; }
        details > summary:first-of-type { display: list-item; list-style: inside disclosure-closed; }
        details[open] > summary:first-of-type { list-style-type: disclosure-open; }
        dialog:not([open]) { display: none; }
        dialog { position: absolute; left: 0; right: 0; width: fit-content; height: fit-content; margin: auto; border: solid; padding: 1em; background-color: white; color: black; }

        input, textarea, select, button {
            margin: 0; font: 13.333333px sans-serif; color: FieldText; letter-spacing: normal; word-spacing: normal;
            line-height: normal; text-transform: none; text-indent: 0; text-shadow: none; display: inline-block; text-align: start;
        }
        input { padding: 1px 2px; border: 2px inset #767676; background-color: Field; cursor: text; }
        input[type="hidden"] { display: none; }
        button, input[type="button"], input[type="submit"], input[type="reset"] {
            padding: 1px 6px; border: 2px outset ButtonBorder; background-color: ButtonFace; color: ButtonText; text-align: center;
            cursor: default; box-sizing: border-box;
        }
        input[type="button"], input[type="submit"], input[type="reset"] { user-select: none; white-space: pre; }
        input[type="button"]:active, input[type="submit"]:active, input[type="reset"]:active, button:active { border-style: inset; }
        input[type="button"]:active:disabled, input[type="submit"]:active:disabled, input[type="reset"]:active:disabled, button:active:disabled { border-style: outset; }
        input[type="button"]:disabled, input[type="submit"]:disabled, input[type="reset"]:disabled, button:disabled {
            background-color: rgba(239, 239, 239, 0.3); border-color: rgba(118, 118, 118, 0.3); color: rgba(16, 16, 16, 0.3);
        }
        input[type="checkbox"] { margin: 3px 3px 3px 4px; padding: 0; border: none; background-color: transparent; box-sizing: border-box; cursor: default; }
        input[type="radio"] { margin: 3px 3px 0 5px; padding: 0; border: none; background-color: transparent; box-sizing: border-box; cursor: default; }
        input[type="range"] { margin: 2px; padding: 0; border: none; background-color: transparent; color: #101010; cursor: default; }
        input[type="checkbox"]:disabled, input[type="radio"]:disabled, input[type="range"]:disabled, input[type="file"]:disabled { background-color: transparent; }
        input[type="range"]:disabled { color: #c5c5c5; }
        input[type="file"] { padding: 0; border: none; background-color: transparent; cursor: default; }
        input[type="color"] { padding: 1px 2px; background-color: #efefef; border: 1px solid #767676; box-sizing: border-box; cursor: default; }
        input[type="search"] { box-sizing: border-box; }
        input:disabled, textarea:disabled { cursor: default; background-color: rgba(239, 239, 239, 0.3); color: #545454; border-color: rgba(118, 118, 118, 0.3); }
        select { box-sizing: border-box; border: 1px solid #767676; border-radius: 0; background-color: Field; color: FieldText; cursor: default; white-space: pre; }
        select:disabled { border-color: rgba(118, 118, 118, 0.3); opacity: 0.7; }
        select:disabled, optgroup:disabled, option:disabled, select[disabled] > option { color: GrayText; }
        textarea { font-family: monospace; padding: 2px; border: 1px solid #767676; background-color: Field; white-space: pre-wrap; overflow-wrap: break-word; overflow: auto; cursor: text; resize: -internal-textarea-auto; }
        progress { display: inline-block; box-sizing: border-box; width: 10em; height: 1em; vertical-align: -0.2em; }
        meter { display: inline-block; box-sizing: border-box; width: 5em; height: 1em; vertical-align: -0.2em; }
        :focus-visible { outline: auto 1px -webkit-focus-ring-color; }
        html:focus-visible, body:focus-visible { outline: none; }
        input[type="checkbox"]:focus-visible, input[type="radio"]:focus-visible { outline-offset: 2px; }
        a:any-link:focus-visible { outline-offset: 1px; }

        /* The parts of controls (Blink's user-agent shadow elements). */
        input::-webkit-inner-spin-button { appearance: auto; display: inline-block; cursor: default; flex: none; align-self: stretch; opacity: 0; pointer-events: none; }
        input:enabled:read-write:-webkit-any(:focus, :hover)::-webkit-inner-spin-button { opacity: 1; pointer-events: auto; }
        input[type="search"]::-webkit-search-cancel-button {
            appearance: auto; display: block; cursor: default; flex: none; margin-left: 1px; margin-right: 3px; opacity: 0; pointer-events: none; user-select: none !important;
        }
        input[type="search"]:enabled:read-write:-webkit-any(:focus, :hover)::-webkit-search-cancel-button { opacity: 1; pointer-events: auto; }
        input[type="range"]::-webkit-slider-container { flex: 1; min-width: 0; box-sizing: border-box; display: flex; }
        input[type="range"]::-webkit-slider-runnable-track { flex: 1; min-width: 0; align-self: center; box-sizing: border-box; display: block; }
        input[type="range"]::-webkit-slider-thumb { appearance: auto; box-sizing: border-box; display: block; }
        progress::-webkit-progress-inner-element { box-sizing: inherit; height: 100%; width: 100%; }
        progress::-webkit-progress-bar { background-color: gray; height: 100%; width: 100%; box-sizing: border-box; }
        progress::-webkit-progress-value { height: 100%; width: 50%; background-color: green; box-sizing: border-box; }
        img, canvas, surface { overflow: clip; }
        /* Blink's svg.css: inline SVG clips to its content box (overflow-clip-margin: content-box). */
        svg:not(:root), symbol, image, marker, pattern, foreignObject { overflow: hidden; }
        /* SVG elements without a CSS box (and nested svgs) transform about 0 0 (svg.css's
           "*:not(svg), *:not(foreignObject) > svg" in the SVG namespace). */
        svg * { transform-origin: 0 0; }
        ::placeholder { color: #757575; }

        sv-top-layer { display: block; position: fixed; left: 0; top: 0; right: 0; bottom: 0; z-index: 2147483647; pointer-events: none; }
        sv-top-layer > * { position: absolute; }
        sv-validation-bubble {
            display: block; max-width: 300px; margin-top: 4px; padding: 6px 10px; border: 1px solid #767676; background-color: white;
            color: black; font: 13.333333px sans-serif; box-shadow: 0 2px 6px rgba(0, 0, 0, 0.3); white-space: pre-wrap; pointer-events: none;
        }
        sv-validation-main, sv-validation-sub { display: block; }
        sv-validation-sub { margin-top: 2px; }
        sv-tooltip {
            display: block; box-sizing: content-box; padding: 3px 7px 4px; border: 1px solid rgb(60, 60, 60); background-color: white;
            color: rgb(60, 60, 60); font: 12px sans-serif; white-space: pre-wrap; overflow-wrap: break-word; pointer-events: none;
        }
        sv-select-popup {
            display: block; box-sizing: border-box; overflow-y: auto; border: 1px solid #767676; background-color: Field; color: FieldText;
            pointer-events: auto; white-space: pre; cursor: default; user-select: none;
        }
        sv-select-group, sv-select-group-label, sv-select-option { display: block; }
        sv-select-group-label { padding: 4px 2px 0; font-weight: bolder; }
        sv-select-option { padding: 4px 2px; min-height: 24px; box-sizing: border-box; }
        sv-select-group sv-select-option { padding-inline-start: 20px; }
        sv-select-option[highlighted] { background-color: SelectedItem; color: SelectedItemText; }
        sv-select-option[disabled], sv-select-group[disabled] { color: GrayText; }
        """;

    static Stylesheet? _sheet;

    public static Stylesheet Sheet => _sheet ??= Stylesheet.Parse(Css, "user-agent", StyleOrigin.UserAgent);
}
