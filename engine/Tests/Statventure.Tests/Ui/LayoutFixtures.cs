namespace Statventure.Tests.Ui;

/// <summary>A small page the engine lays out and the game's browser measures (Golden/layout-reference.json).</summary>
public sealed record LayoutFixture(string Name, int Width, int Height, string Css, string Html);

/// <summary>
/// Layout fixtures covering what the game's stylesheet uses: block flow and margins, inline
/// formatting and text, flexbox, grid, positioning, overflow and scrollbars, intrinsic sizes,
/// lists, generated content and form controls.
/// </summary>
public static class LayoutFixtures
{
    /// <summary>Styles every fixture starts from (the fonts are the game's own).</summary>
    public const string BaseCss = "body { margin: 0; font-family: Lato; font-size: 16px; }\n";

    public static readonly LayoutFixture[] All =
    [
        new("block-basic", 600, 400, """
            .a { width: 300px; height: 40px; margin: 10px 20px; padding: 5px 7px; border: 3px solid #000; }
            .b { box-sizing: border-box; width: 50%; height: 30px; padding: 0 10px; border: 2px solid; }
            .c { width: 200px; margin: 0 auto; height: 20px; }
            .d { width: 150px; margin-left: auto; height: 20px; }
            .e { max-width: 120px; min-width: 50px; height: 10px; padding: 4%; }
            .f { width: calc(100% - 37px); height: 12px; border-left: 0.5px solid; border-right: 1.5px solid; }
            .g { width: min(80%, 260px); height: 8px; }
            .h { width: clamp(100px, 30vw, 500px); height: 9px; }
            .i { width: 10em; height: 2rem; font-size: 20px; }
            """, """
            <div class="a"></div><div class="b"></div><div class="c"></div><div class="d"></div>
            <div class="e"></div><div class="f"></div><div class="g"></div><div class="h"></div><div class="i"></div>
            """),
        new("margin-collapse", 600, 500, """
            div { background: #ccc; }
            .s1 { margin: 20px 0 30px; height: 10px; }
            .s2 { margin: 15px 0 -5px; height: 10px; }
            .parent { margin-top: 10px; }
            .child { margin-top: 25px; margin-bottom: 40px; height: 10px; }
            .empty { margin: 12px 0 18px; }
            .bfc { overflow: hidden; margin: 5px 0; }
            .bfc > .child { margin-top: 15px; }
            .pad { padding-top: 1px; margin-top: 8px; }
            .pad > .child { margin-top: 12px; }
            .neg { margin-top: -10px; height: 10px; }
            .fixed { height: 50px; }
            .fixed > .child { margin-bottom: 30px; }
            """, """
            <div class="s1"></div><div class="s2"></div>
            <div class="parent"><div class="child"></div></div>
            <div class="empty"></div>
            <div class="bfc"><div class="child"></div></div>
            <div class="pad"><div class="child"></div></div>
            <div class="neg"></div>
            <div class="fixed"><div class="child"></div></div>
            <div class="s1"></div>
            """),
        new("inline-text", 600, 500, """
            p { width: 260px; margin: 0 0 10px; }
            .big { font-size: 24px; }
            .small { font-size: 12px; }
            .serif { font-family: "STIX Two Text"; font-size: 18px; }
            .wide { width: 520px; }
            """, """
            <p>The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs.</p>
            <p>Mixed <span class="big">large text</span> and <span class="small">small text</span> on <b>bold</b> and <i>italic</i> lines that wrap around.</p>
            <p class="serif">STIX Two Text in a serif face, <em>with italics</em>, wrapping onto a second line.</p>
            <p class="wide">Supercalifragilisticexpialidocious-and-hyphenated-words break after hyphens when the line is too narrow for them.</p>
            <p style="width: 90px">Averyveryverylongunbreakablewordthatoverflows next</p>
            <p>  Leading   and   collapsed    spaces   </p>
            """),
        new("inline-boxes", 600, 400, """
            p { width: 300px; margin: 0 0 12px; line-height: 1.5; }
            .pad { padding: 4px 10px; border: 2px solid; margin: 0 6px; background: #eee; }
            .em { padding: 0 1em; }
            .nested { font-size: 20px; }
            .nested b { font-size: 14px; padding: 2px; }
            """, """
            <p>Before <span class="pad">a padded inline box that wraps across more than one line</span> after.</p>
            <p><span class="em">em padding</span><span class="pad">adjacent</span>no space</p>
            <p>Text <span class="nested">nested <b>inner</b> boxes</span> end.</p>
            <p><span></span></p>
            <p><span class="pad"></span></p>
            """),
        new("line-height", 600, 600, """
            div { width: 400px; margin-bottom: 8px; }
            .n1 { line-height: 1; }
            .n2 { line-height: 2; }
            .px { line-height: 30px; }
            .normal { line-height: normal; font-size: 13px; }
            .mix { line-height: 1.2; }
            .mix span { font-size: 28px; }
            .va span { font-size: 12px; }
            .sub { vertical-align: sub; }
            .sup { vertical-align: super; }
            .top { vertical-align: top; }
            .bottom { vertical-align: bottom; }
            .middle { vertical-align: middle; }
            .tt { vertical-align: text-top; }
            .tb { vertical-align: text-bottom; }
            .len { vertical-align: 5px; }
            .pct { vertical-align: -50%; }
            .tall { line-height: 40px; }
            .stix { font-family: "STIX Two Text"; }
            """, """
            <div class="n1">Line height one wrapping text that goes on and on to fill a couple of lines.</div>
            <div class="n2">Line height two</div>
            <div class="px">Thirty pixels</div>
            <div class="normal">Normal at 13px</div>
            <div class="mix">Small <span>Large</span> small</div>
            <div class="va">Base <span class="sub">sub</span> <span class="sup">sup</span> <span class="top">top</span> <span class="bottom">bottom</span> <span class="middle">middle</span> <span class="tt">tt</span> <span class="tb">tb</span> <span class="len">len</span> <span class="pct">pct</span></div>
            <div class="va tall">Tall <span class="top">top</span> <span class="bottom">bottom</span> <span class="middle">middle</span></div>
            <div class="stix">STIX line <span style="font-family: Lato">Lato</span> mixed</div>
            """),
        new("white-space", 600, 600, """
            div { width: 200px; margin-bottom: 6px; }
            .nowrap { white-space: nowrap; }
            .pre { white-space: pre; }
            .prewrap { white-space: pre-wrap; }
            .preline { white-space: pre-line; }
            """, """
            <div class="nowrap">This line does not wrap even though it is long.</div>
            <div class="pre">  pre  keeps
            spaces and
              newlines</div>
            <div class="prewrap">pre-wrap   keeps   spaces but wraps when the text is long enough.</div>
            <div class="preline">pre-line   collapses   spaces
            but keeps newlines</div>
            <div>first<br>second<br><br>fourth</div>
            <div>text <br> after break</div>
            """),
        new("text-align", 600, 500, """
            div { width: 300px; margin-bottom: 6px; }
            .c { text-align: center; }
            .r { text-align: right; }
            .j { text-align: justify; }
            .e { text-align: end; }
            .ci { text-align: center; }
            .ci span { display: inline-block; width: 50px; height: 10px; }
            """, """
            <div class="c">Centred text that wraps onto a second line here.</div>
            <div class="r">Right aligned text that wraps onto a second line.</div>
            <div class="j">Justified text spreads its words to fill each line except the last one, which stays left.</div>
            <div class="e">End aligned</div>
            <div class="ci"><span></span><span></span></div>
            """),
        new("inline-block", 600, 500, """
            .row { width: 500px; margin-bottom: 10px; }
            .ib { display: inline-block; padding: 4px; border: 1px solid; }
            .w { width: 80px; }
            .h { height: 40px; }
            .clip { overflow: hidden; height: 30px; }
            .mid { vertical-align: middle; }
            .top { vertical-align: top; }
            .bot { vertical-align: bottom; }
            .empty { width: 30px; height: 30px; }
            .two { width: 60px; }
            """, """
            <div class="row">Text <span class="ib">inline block</span> text <span class="ib w">fixed width wraps</span> end</div>
            <div class="row">A <span class="ib h">tall</span> B <span class="ib clip">clipped</span> C <span class="ib empty"></span> D</div>
            <div class="row">M <span class="ib mid h">mid</span> T <span class="ib top h">top</span> B <span class="ib bot h">bot</span> x</div>
            <div class="row">Two <span class="ib two">lines inside block</span> after</div>
            <div class="row"><span class="ib w">a</span><span class="ib w">b</span><span class="ib w">c</span><span class="ib w">d</span><span class="ib w">e</span><span class="ib w">f</span></div>
            """),
        new("spacing-transform", 600, 400, """
            div { width: 400px; margin-bottom: 6px; }
            .ls { letter-spacing: 2px; }
            .lsn { letter-spacing: -0.04em; font-size: 30px; }
            .ws { word-spacing: 6px; }
            .up { text-transform: uppercase; letter-spacing: 0.12em; font-size: 12px; }
            .cap { text-transform: capitalize; }
            .lo { text-transform: lowercase; }
            .tab { font-variant-numeric: tabular-nums; }
            .w5 { font-weight: 500; }
            .w6 { font-weight: 600; }
            .w9 { font-weight: 900; }
            """, """
            <div class="ls">Letter spacing two pixels</div>
            <div class="lsn">Negative em spacing</div>
            <div class="ws">Word spacing six pixels</div>
            <div class="up">Uppercase small label</div>
            <div class="cap">capitalize every word here</div>
            <div class="lo">LOWERCASE TEXT</div>
            <div class="tab">1111 0000 9876</div>
            <div>1111 0000 9876</div>
            <div class="w5">Weight 500</div><div class="w6">Weight 600</div><div class="w9">Weight 900</div>
            """),
        new("ellipsis-clamp", 600, 400, """
            .el { width: 180px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; margin-bottom: 6px; }
            .clamp { width: 200px; display: -webkit-box; -webkit-box-orient: vertical; -webkit-line-clamp: 2; overflow: hidden; margin-bottom: 6px; }
            .fits { width: 300px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
            """, """
            <div class="el">This text is much too long for its box</div>
            <div class="el"><span>Inside a span</span> that is also too long</div>
            <div class="clamp">Line clamped text keeps only two lines of this long paragraph and cuts the rest off with an ellipsis.</div>
            <div class="clamp">Short clamp.</div>
            <div class="fits">Fits fine</div>
            """),
        new("flex-row", 600, 500, """
            .f { display: flex; width: 500px; margin-bottom: 8px; background: #ddd; }
            .f > div { height: 20px; background: #aaa; }
            .g1 > div { flex: 1; }
            .g2 > :first-child { flex: 2; } .g2 > div { flex: 1 1 0; }
            .basis > div { flex: 0 0 100px; }
            .shrink > div { width: 200px; }
            .shrink > :last-child { flex-shrink: 3; }
            .gap { gap: 12px; }
            .gap > div { width: 50px; }
            .jc-c { justify-content: center; } .jc-e { justify-content: flex-end; } .jc-sb { justify-content: space-between; }
            .jc-sa { justify-content: space-around; } .jc-se { justify-content: space-evenly; }
            .jc > div { width: 60px; }
            .maxw > div { flex: 1; max-width: 80px; }
            .minw > div { flex: 1; } .minw > :first-child { min-width: 300px; }
            .pct > div { width: 25%; padding: 0 5%; }
            .content > div { flex: 1 1 auto; }
            """, """
            <div class="f g1"><div></div><div></div><div></div></div>
            <div class="f g2"><div></div><div></div><div></div></div>
            <div class="f basis"><div></div><div></div></div>
            <div class="f shrink"><div></div><div></div><div></div></div>
            <div class="f gap"><div></div><div></div><div></div></div>
            <div class="f jc jc-c"><div></div><div></div></div>
            <div class="f jc jc-e"><div></div><div></div></div>
            <div class="f jc jc-sb"><div></div><div></div><div></div></div>
            <div class="f jc jc-sa"><div></div><div></div><div></div></div>
            <div class="f jc jc-se"><div></div><div></div><div></div></div>
            <div class="f maxw"><div></div><div></div></div>
            <div class="f minw"><div></div><div></div><div></div></div>
            <div class="f pct"><div></div><div></div></div>
            <div class="f content"><div>short</div><div>a much longer piece of content</div></div>
            """),
        new("flex-align", 600, 600, """
            .f { display: flex; width: 500px; height: 80px; margin-bottom: 8px; background: #ddd; }
            .f > div { width: 60px; background: #aaa; }
            .auto { height: auto; }
            .c { align-items: center; } .e { align-items: flex-end; } .s { align-items: flex-start; } .b { align-items: baseline; }
            .b > :nth-child(2) { font-size: 30px; padding-top: 10px; }
            .self > :nth-child(1) { align-self: flex-end; } .self > :nth-child(2) { align-self: center; } .self > :nth-child(3) { height: 20px; }
            .am > :nth-child(1) { margin-left: auto; } .am > :nth-child(2) { margin: auto; height: 20px; }
            .text > div { width: auto; }
            .mt > div { margin-top: 10px; }
            """, """
            <div class="f"><div>stretch</div><div>items</div></div>
            <div class="f c"><div>center</div><div>two<br>lines</div></div>
            <div class="f e"><div>end</div></div>
            <div class="f s"><div>start</div></div>
            <div class="f b"><div>base</div><div>line</div><div>x</div></div>
            <div class="f self"><div>end</div><div>mid</div><div>fix</div><div>str</div></div>
            <div class="f am"><div>a</div><div>b</div></div>
            <div class="f auto text"><div>auto height text</div><div>more</div></div>
            <div class="f auto mt"><div>m</div></div>
            """),
        new("flex-wrap", 600, 600, """
            .f { display: flex; flex-wrap: wrap; width: 300px; margin-bottom: 8px; background: #ddd; }
            .f > div { width: 90px; height: 20px; background: #aaa; }
            .gap { gap: 6px 10px; }
            .h { height: 120px; }
            .ac-c { align-content: center; } .ac-sb { align-content: space-between; } .ac-s { align-content: flex-start; }
            .grow > div { flex: 1 0 90px; }
            .rev { flex-wrap: wrap-reverse; }
            .var > div:nth-child(2n) { height: 35px; }
            """, """
            <div class="f"><div></div><div></div><div></div><div></div><div></div></div>
            <div class="f gap"><div></div><div></div><div></div><div></div><div></div></div>
            <div class="f h"><div></div><div></div><div></div><div></div></div>
            <div class="f h ac-c"><div></div><div></div><div></div><div></div></div>
            <div class="f h ac-sb"><div></div><div></div><div></div><div></div></div>
            <div class="f h ac-s"><div></div><div></div><div></div><div></div></div>
            <div class="f grow"><div></div><div></div><div></div><div></div></div>
            <div class="f rev h"><div></div><div></div><div></div><div></div></div>
            <div class="f var"><div></div><div></div><div></div><div></div><div></div></div>
            """),
        new("flex-column", 600, 700, """
            .f { display: flex; flex-direction: column; width: 200px; margin-bottom: 8px; background: #ddd; }
            .f > div { background: #aaa; }
            .h { height: 150px; }
            .grow > :nth-child(2) { flex: 1; }
            .shrink > div { height: 80px; }
            .minh { min-height: 120px; }
            .minh > :last-child { flex: 1; }
            .center { align-items: center; justify-content: center; }
            .gap { gap: 7px; }
            .pct > div { height: 30%; }
            .row-in-col > div { display: flex; }
            .row-in-col span { flex: 1; }
            """, """
            <div class="f"><div>one</div><div>two</div></div>
            <div class="f h grow"><div>top</div><div>grows</div><div>bottom</div></div>
            <div class="f h shrink"><div>a</div><div>b</div><div>c</div></div>
            <div class="f minh"><div>first</div><div>fills</div></div>
            <div class="f h center"><div>centred</div></div>
            <div class="f gap"><div>g1</div><div>g2</div><div>g3</div></div>
            <div class="f h pct"><div>p1</div><div>p2</div></div>
            <div class="f row-in-col"><div><span>l</span><span>r</span></div></div>
            """),
        new("flex-min-size", 600, 400, """
            .f { display: flex; width: 300px; margin-bottom: 8px; background: #ddd; }
            .f > div { flex: 1; background: #aaa; }
            .z > div { min-width: 0; }
            .o > div { overflow: hidden; }
            .nw div { white-space: nowrap; }
            .col { flex-direction: column; height: 60px; }
            .col > div { flex: 1 1 0; min-height: 0; }
            """, """
            <div class="f"><div>Averyveryverylongunbreakablewordhere</div><div>b</div></div>
            <div class="f z"><div>Averyveryverylongunbreakablewordhere</div><div>b</div></div>
            <div class="f o"><div>Averyveryverylongunbreakablewordhere</div><div>b</div></div>
            <div class="f nw"><div>no wrap text that is long</div><div>other side</div></div>
            <div class="f col"><div>one<br>two<br>three<br>four</div><div>b</div></div>
            """),
        new("flex-reverse", 600, 400, """
            .f { display: flex; width: 400px; height: 60px; margin-bottom: 8px; background: #ddd; }
            .f > div { width: 70px; background: #aaa; }
            .rr { flex-direction: row-reverse; }
            .cr { flex-direction: column-reverse; height: 120px; }
            .cr > div { height: 20px; }
            .ord > :first-child { order: 2; } .ord > :last-child { order: -1; }
            .rr-c { flex-direction: row-reverse; justify-content: center; }
            .rr-am { flex-direction: row-reverse; } .rr-am > :first-child { margin-right: auto; }
            """, """
            <div class="f rr"><div>1</div><div>2</div><div>3</div></div>
            <div class="f cr"><div>1</div><div>2</div><div>3</div></div>
            <div class="f ord"><div>1</div><div>2</div><div>3</div></div>
            <div class="f rr-c"><div>1</div><div>2</div></div>
            <div class="f rr-am"><div>1</div><div>2</div></div>
            """),
        new("flex-nested", 600, 500, """
            .outer { display: flex; width: 500px; height: 200px; background: #ddd; margin-bottom: 8px; }
            .side { width: 120px; display: flex; flex-direction: column; }
            .side > div { flex: 1; border: 1px solid; }
            .main { flex: 1; display: flex; flex-direction: column; padding: 10px; }
            .main header { height: 30px; }
            .main section { flex: 1; overflow: auto; min-height: 0; }
            .main section div { height: 50%; }
            .main footer { padding: 4px; }
            .inline { display: inline-flex; gap: 4px; padding: 2px; border: 1px solid; }
            """, """
            <div class="outer"><div class="side"><div>a</div><div>b</div></div><div class="main"><header>Header</header><section><div>half</div></section><footer>Footer text</footer></div></div>
            <div>Text with <span class="inline"><span>inline</span><span>flex</span></span> inside.</div>
            """),
        new("grid-fixed", 600, 500, """
            .g { display: grid; width: 500px; margin-bottom: 10px; background: #ddd; }
            .g > div { background: #aaa; min-height: 10px; }
            .a { grid-template-columns: 100px 1fr 2fr; gap: 10px; }
            .b { grid-template-columns: repeat(3, 1fr); grid-template-rows: 30px auto 40px; column-gap: 5px; row-gap: 3px; }
            .c { grid-template-columns: 30% 1fr; }
            .d { grid-template-columns: 1fr 1fr; grid-auto-rows: 25px; }
            .e { grid-template-columns: 50px 50px; }
            """, """
            <div class="g a"><div>1</div><div>2</div><div>3</div><div>4 has more text in it</div></div>
            <div class="g b"><div>1</div><div>2</div><div>3</div><div>4</div><div>5 text that wraps around in the cell</div><div>6</div><div>7</div></div>
            <div class="g c"><div>left</div><div>right</div></div>
            <div class="g d"><div>1</div><div>2</div><div>3</div></div>
            <div class="g e"><div>1</div><div>2</div><div>3</div></div>
            """),
        new("grid-areas", 600, 500, """
            .g { display: grid; width: 500px; height: 300px; grid-template-columns: 120px 1fr 80px; grid-template-rows: 50px 1fr 40px;
                 grid-template-areas: "head head head" "side main aside" "foot foot foot"; gap: 4px; background: #ddd; }
            .g > div { background: #aaa; }
            .h { grid-area: head; } .s { grid-area: side; } .m { grid-area: main; } .x { grid-area: aside; } .f { grid-area: foot; }
            .lines { display: grid; width: 400px; grid-template-columns: [start] 1fr [mid] 1fr [end]; margin-top: 10px; }
            .lines > div { background: #aaa; height: 10px; }
            .l1 { grid-column: start / end; } .l2 { grid-column: mid; } .l3 { grid-column: 1 / -1; } .l4 { grid-column: span 2; } .l5 { grid-row: 5; grid-column: 2; }
            """, """
            <div class="g"><div class="h">head</div><div class="s">side</div><div class="m">main</div><div class="x">aside</div><div class="f">foot</div></div>
            <div class="lines"><div class="l1"></div><div class="l2"></div><div class="l3"></div><div class="l4"></div><div class="l5"></div></div>
            """),
        new("grid-auto", 600, 500, """
            .g { display: grid; width: 400px; margin-bottom: 10px; background: #ddd; gap: 2px; }
            .g > div { background: #aaa; min-height: 15px; }
            .col { grid-auto-flow: column; grid-template-rows: 20px 20px; }
            .dense { grid-template-columns: repeat(3, 1fr); grid-auto-flow: row dense; }
            .wide { grid-column: span 2; }
            .tall { grid-row: span 2; }
            .sparse { grid-template-columns: repeat(3, 1fr); }
            .autocol { grid-auto-columns: 50px; grid-auto-flow: column; }
            """, """
            <div class="g col"><div>1</div><div>2</div><div>3</div><div>4</div><div>5</div></div>
            <div class="g dense"><div>1</div><div class="wide">2</div><div class="wide">3</div><div>4</div><div>5</div></div>
            <div class="g sparse"><div>1</div><div class="wide">2</div><div class="wide">3</div><div>4</div><div class="tall">5</div><div>6</div></div>
            <div class="g autocol"><div>a</div><div>b</div><div>c</div></div>
            """),
        new("grid-repeat", 700, 500, """
            .g { display: grid; width: 600px; gap: 10px; margin-bottom: 10px; background: #ddd; }
            .g > div { background: #aaa; height: 20px; }
            .fill { grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); }
            .fit { grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); }
            .fixed { grid-template-columns: repeat(auto-fill, 100px); }
            .mixed { grid-template-columns: 80px repeat(auto-fill, minmax(90px, 1fr)) 60px; }
            """, """
            <div class="g fill"><div></div><div></div></div>
            <div class="g fit"><div></div><div></div></div>
            <div class="g fill"><div></div><div></div><div></div><div></div><div></div><div></div><div></div></div>
            <div class="g fixed"><div></div><div></div><div></div><div></div><div></div><div></div><div></div></div>
            <div class="g mixed"><div></div><div></div><div></div><div></div><div></div></div>
            """),
        new("grid-intrinsic", 700, 500, """
            .g { display: grid; width: 600px; gap: 6px; margin-bottom: 10px; background: #ddd; }
            .g > div { background: #aaa; }
            .a { grid-template-columns: auto 1fr auto; }
            .b { grid-template-columns: min-content max-content 1fr; }
            .c { grid-template-columns: minmax(0, 1fr) 2fr; }
            .d { grid-template-columns: fit-content(150px) 1fr; }
            .e { grid-template-columns: 1fr 1fr; }
            .inline { display: inline-grid; grid-template-columns: auto auto; gap: 4px; width: auto; }
            """, """
            <div class="g a"><div>label</div><div>flexible middle</div><div>end text</div></div>
            <div class="g b"><div>min content words</div><div>max content words</div><div>rest</div></div>
            <div class="g c"><div>Averyveryverylongunbreakablewordinthefirstcolumn</div><div>second</div></div>
            <div class="g d"><div>fit content text that is rather long for the column</div><div>x</div></div>
            <div class="g e"><div>Averyveryverylongunbreakablewordinthefirstcolumnmorethanhalf</div><div>b</div></div>
            <div class="inline"><div>one</div><div>two two</div><div>three</div><div>4</div></div>
            """),
        new("grid-align", 600, 600, """
            .g { display: grid; width: 400px; height: 120px; grid-template-columns: 1fr 1fr; grid-template-rows: 1fr 1fr; margin-bottom: 10px; background: #ddd; }
            .g > div { background: #aaa; }
            .pi { place-items: center; }
            .ji { justify-items: end; align-items: start; }
            .self > :nth-child(1) { justify-self: center; } .self > :nth-child(2) { align-self: end; } .self > :nth-child(3) { place-self: center end; }
            .jc { grid-template-columns: 80px 80px; grid-template-rows: 30px 30px; justify-content: center; align-content: space-between; }
            .jc2 { grid-template-columns: 80px 80px; grid-template-rows: 30px; justify-content: space-evenly; align-content: end; }
            .fixed > div { width: 40px; height: 20px; }
            .marg > div { margin: auto; }
            """, """
            <div class="g pi"><div>a</div><div>b</div><div>c</div></div>
            <div class="g ji"><div>a</div><div>b</div></div>
            <div class="g self"><div>a</div><div>b</div><div>c</div></div>
            <div class="g jc"><div>a</div><div>b</div><div>c</div></div>
            <div class="g jc2"><div>a</div><div>b</div></div>
            <div class="g fixed"><div></div><div></div></div>
            <div class="g marg"><div>m</div></div>
            """),
        new("positioned", 600, 500, """
            .rel { position: relative; width: 400px; height: 200px; margin: 20px; padding: 10px; border: 5px solid; }
            .a1 { position: absolute; top: 10px; left: 20px; width: 50px; height: 30px; }
            .a2 { position: absolute; right: 0; bottom: 0; width: 25%; height: 20%; }
            .a3 { position: absolute; top: 0; bottom: 0; left: 100px; right: 100px; }
            .a4 { position: absolute; inset: 0; margin: auto; width: 60px; height: 40px; }
            .a5 { position: absolute; top: 50px; right: 10px; }
            .a6 { position: absolute; }
            .r { position: relative; top: 5px; left: -3px; width: 30px; height: 10px; }
            .f { position: fixed; bottom: 10px; right: 10px; width: 50px; height: 20px; }
            .after { height: 30px; }
            """, """
            <div class="rel">
              <div class="a1"></div><div class="a2"></div><div class="a3"></div><div class="a4"></div>
              <div class="a5">shrink to fit text</div>
              <div class="after"></div><div class="a6">static position</div>
              <div class="r"></div>
              <span>inline <span class="a6">abs in line</span> text</span>
            </div>
            <div class="f"></div>
            """),
        new("positioned-nested", 600, 500, """
            .outer { position: relative; width: 500px; height: 300px; }
            .mid { position: absolute; left: 50px; top: 40px; width: 300px; height: 200px; border: 2px solid; padding: 8px; }
            .inner { position: absolute; right: 10px; bottom: 10px; width: 50%; }
            .inner p { margin: 0; }
            .static { margin: 30px; }
            .static .abs { position: absolute; left: 5px; top: 5px; }
            .pct { position: absolute; left: 10%; top: 10%; width: 30%; height: 30%; }
            .nowidth { position: absolute; left: 10px; right: auto; top: 260px; }
            """, """
            <div class="outer">
              <div class="mid"><div class="inner"><p>Inner text that wraps in half the width</p></div><div class="pct"></div></div>
              <div class="static"><div class="abs">abs</div></div>
              <div class="nowidth">no width given here</div>
            </div>
            """),
        new("overflow-scroll", 600, 600, """
            .box { width: 200px; height: 80px; margin-bottom: 10px; border: 2px solid; padding: 5px; }
            .auto { overflow: auto; }
            .scroll { overflow: scroll; }
            .hidden { overflow: hidden; }
            .thin { scrollbar-width: thin; }
            .none { scrollbar-width: none; }
            .gutter { scrollbar-gutter: stable; }
            .tall { height: 200px; }
            .wide { width: 400px; height: 10px; }
            .y { overflow-y: auto; overflow-x: hidden; }
            """, """
            <div class="box auto"><div class="tall">tall content</div></div>
            <div class="box auto thin"><div class="tall">thin scrollbar</div></div>
            <div class="box auto"><div>fits</div></div>
            <div class="box scroll"><div>always scroll</div></div>
            <div class="box auto gutter thin"><div>gutter</div></div>
            <div class="box auto"><div class="wide"></div></div>
            <div class="box y none"><div class="tall">none</div></div>
            """),
        new("percent-heights", 600, 500, """
            .p { height: 200px; width: 300px; margin-bottom: 10px; }
            .c { height: 50%; background: #aaa; }
            .auto { width: 300px; margin-bottom: 10px; }
            .minh { min-height: 25%; }
            .bb { box-sizing: border-box; height: 50%; padding: 10px; border: 5px solid; }
            .maxh { max-height: 30%; height: 500px; }
            """, """
            <div class="p"><div class="c">half</div></div>
            <div class="auto"><div class="c">indefinite</div></div>
            <div class="p"><div class="bb">border box</div></div>
            <div class="p"><div class="maxh">max</div></div>
            <div class="p"><div class="minh">min</div></div>
            """),
        new("aspect-ratio", 600, 600, """
            .a { width: 200px; aspect-ratio: 2; margin-bottom: 6px; background: #aaa; }
            .b { width: 30%; aspect-ratio: 16 / 9; background: #aaa; margin-bottom: 6px; }
            .c { width: 100px; aspect-ratio: 1; padding: 10px; background: #aaa; margin-bottom: 6px; }
            .d { width: 100px; aspect-ratio: 1; padding: 10px; box-sizing: border-box; background: #aaa; margin-bottom: 6px; }
            .e { width: 80px; aspect-ratio: 4; background: #aaa; margin-bottom: 6px; }
            .f { display: flex; gap: 6px; width: 400px; margin-bottom: 6px; }
            .f > div { flex: 1; aspect-ratio: 1; background: #aaa; }
            .g { display: grid; grid-template-columns: repeat(3, 1fr); width: 300px; gap: 6px; }
            .g > div { aspect-ratio: 3 / 2; background: #aaa; }
            .h { height: 50px; aspect-ratio: 2; background: #aaa; }
            """, """
            <div class="a"></div><div class="b"></div><div class="c"></div><div class="d"></div>
            <div class="e">overflowing content in a short box</div>
            <div class="f"><div></div><div></div><div></div></div>
            <div class="g"><div></div><div></div><div></div></div>
            <div><div class="h" style="display:inline-block"></div></div>
            """),
        new("lists", 600, 500, """
            ul, ol { width: 300px; }
            .none { list-style: none; padding: 0; margin: 0; }
            .inside { list-style-position: inside; }
            .small { padding-left: 18px; font-size: 13px; }
            """, """
            <ul><li>First item</li><li>Second item that is long enough to wrap onto another line</li></ul>
            <ol><li>One</li><li>Two</li><li>Three</li></ol>
            <ul class="none"><li>no marker</li><li>none</li></ul>
            <ol class="inside"><li>inside marker</li></ol>
            <ol class="small"><li>small</li><li><div>block in item</div></li></ol>
            """),
        new("pseudo", 600, 400, """
            .a::before { content: "before "; }
            .a::after { content: " after"; font-weight: 700; }
            .b { position: relative; width: 200px; height: 40px; }
            .b::after { content: ""; position: absolute; inset: 5px; border: 1px solid; }
            .c::before { content: attr(data-label); display: block; font-size: 12px; }
            .d { display: flex; gap: 5px; }
            .d::before { content: "x"; width: 20px; }
            .d::after { content: "y"; flex: 1; }
            """, """
            <div class="a">text</div>
            <div class="b">box</div>
            <div class="c" data-label="Label">value</div>
            <div class="d"><span>middle</span></div>
            """),
        new("form-controls", 700, 500, """
            div { margin-bottom: 6px; }
            button, input, select, textarea { font: inherit; }
            .styled { padding: 7px 10px; border: 1px solid; border-radius: 3px; }
            .w { width: 58px; padding: 5px; border: 1px solid; }
            .block { display: block; width: 100%; box-sizing: border-box; }
            .flex { display: inline-flex; align-items: center; gap: 6px; }
            .tall { height: 50px; }
            """, """
            <div><button>Default button</button> <button class="styled">Styled</button> <button class="flex styled"><span>+</span><span>Flex</span></button></div>
            <div><button class="tall">Tall button</button> <button disabled>Disabled</button></div>
            <div><input type="text"> <input type="text" size="5"> <input type="number" class="w"></div>
            <div><input type="checkbox"> <input type="radio"> <input type="range"></div>
            <div><select><option>Option one</option><option>A longer option two</option></select></div>
            <div><input class="block" type="search"></div>
            """),
        new("intrinsic", 600, 600, """
            .ib { display: inline-block; border: 1px solid; padding: 3px; }
            .max { width: max-content; border: 1px solid; }
            .min { width: min-content; border: 1px solid; }
            .fit { width: fit-content; border: 1px solid; margin: 0 auto; }
            .abs { position: absolute; left: 300px; top: 300px; border: 1px solid; }
            .wrap { width: 250px; }
            .nested { display: inline-block; }
            .nested div { display: inline-block; width: 60px; }
            """, """
            <div class="wrap"><span class="ib">An inline block whose text wraps because the available width is narrow</span></div>
            <div class="max">max-content width text</div>
            <div class="min">min-content width text</div>
            <div class="fit">fit content</div>
            <div class="abs">absolute shrink to fit with wrapping text inside of it</div>
            <div class="nested"><div>a</div><div>b</div><div>c</div></div>
            """),
        new("ua-defaults", 600, 900, "body { margin: 8px; font-family: Lato; }", """
            <h1>Heading one</h1><h2>Heading two</h2><h3>Heading three</h3><h4>Heading four</h4><h5>Heading five</h5><h6>Heading six</h6>
            <p>Paragraph</p><p>Another <strong>strong</strong> <em>em</em> <small>small</small></p>
            <dl><dt>Term</dt><dd>Definition</dd></dl>
            <hr>
            <blockquote>Quote</blockquote>
            """),
        new("display-misc", 600, 400, """
            .contents { display: contents; }
            .none { display: none; }
            .flex { display: flex; gap: 10px; }
            .flex > * { background: #aaa; }
            summary { list-style: none; }
            """, """
            <div class="flex"><div>a</div><div class="contents"><div>b</div><div>c</div></div><div class="none">d</div><span>e</span> loose text</div>
            <div>before<div class="contents"><span>inline in contents</span></div>after</div>
            <details><summary>Closed summary</summary><p>Hidden</p></details>
            <details open><summary>Open summary</summary><p>Shown</p></details>
            """),
        new("units", 800, 400, """
            html { font-size: 20px; }
            .a { width: 10rem; height: 1rem; }
            .b { width: 50vw; height: 5vh; }
            .c { font-size: 1.5em; width: 4em; height: 1em; }
            .d { width: calc(50% + 2rem - 10px); height: calc(1em * 2); }
            .e { width: max(100px, 20%); height: min(10px, 5vh); }
            .f { padding: clamp(2px, 1vw, 20px); width: 50px; }
            """, """
            <div class="a"></div><div class="b"></div><div class="c"></div><div class="d"></div><div class="e"></div><div class="f"></div>
            """),
        new("game-like", 900, 600, """
            :root { --line: #b9af9a; --ink: #1c1511; --font-ui: Lato, "Segoe UI", Arial, sans-serif; --font-title: "STIX Two Text", Georgia, serif; }
            body { font-family: var(--font-ui); letter-spacing: -0.01em; color: var(--ink); }
            .panel { display: grid; grid-template-columns: minmax(0, 1fr) 280px; gap: 18px; padding: 20px; }
            .card { position: relative; padding: 15px 17px; border: 1px solid var(--line); border-radius: 3px; }
            .card h2 { margin: 0; font-family: var(--font-title); font-weight: 400; font-size: 1.75rem; letter-spacing: -0.045em; }
            .eyebrow { margin: 0 0 6px; font-size: .6875rem; letter-spacing: .16em; text-transform: uppercase; }
            .stats { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 8px; margin-top: 12px; }
            .stat { display: flex; align-items: baseline; justify-content: space-between; padding: 6px 8px; border: 1px solid var(--line); }
            .stat strong { font-family: var(--font-title); font-size: 1.5rem; font-weight: 400; line-height: 1; }
            .stat small { font-size: .75rem; text-transform: uppercase; letter-spacing: .05em; }
            .side { display: flex; flex-direction: column; gap: 10px; }
            .btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 10px; border: 1px solid var(--line); border-radius: 3px; background: #fff; font: inherit; font-size: .8125rem; }
            .note { position: absolute; right: 17px; top: 15px; font-size: .75rem; }
            .list { margin: 10px 0 0; padding: 0; list-style: none; display: grid; gap: 4px; }
            .list li { display: flex; justify-content: space-between; gap: 10px; }
            .list li span:last-child { font-variant-numeric: tabular-nums; }
            """, """
            <div class="panel">
              <section class="card"><p class="eyebrow">Character</p><h2>Ser Alrik of the Vale</h2><span class="note">Level 12</span>
                <div class="stats"><div class="stat"><small>Str</small><strong>14</strong></div><div class="stat"><small>Dex</small><strong>9</strong></div><div class="stat"><small>Int</small><strong>17</strong></div></div>
                <ul class="list"><li><span>Accuracy</span><span>82%</span></li><li><span>Evasion</span><span>14%</span></li><li><span>Armor</span><span>31%</span></li></ul>
              </section>
              <aside class="side"><button class="btn"><span>+</span> Level up</button><button class="btn">Restore</button><div class="card"><p class="eyebrow">Gold</p>1,240</div></aside>
            </div>
            """),
    ];
}
