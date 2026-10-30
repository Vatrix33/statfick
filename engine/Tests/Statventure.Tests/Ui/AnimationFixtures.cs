namespace Statventure.Tests.Ui;

/// <summary>
/// A page with animations or transitions. The browser seeks every animation to each sample time
/// and reports the computed values of the listed properties for each element with an id.
/// <see cref="Trigger"/> is a class added to the body after the first style pass (to start transitions).
/// </summary>
public sealed record AnimationFixture(string Name, string Css, string Html, string[] Properties, string? Trigger = null);

public static class AnimationFixtures
{
    public static readonly double[] Times = [0, 0.1, 0.25, 0.4, 0.5, 0.75, 0.9, 1.2, 1.6, 2.5];

    public static readonly AnimationFixture[] All =
    [
        new("timing", """
            @keyframes fade { from { opacity: 0; } to { opacity: 1; } }
            div { width: 20px; height: 20px; animation: fade 1s both; }
            #linear { animation-timing-function: linear; }
            #ease { animation-timing-function: ease; }
            #in { animation-timing-function: ease-in; }
            #out { animation-timing-function: ease-out; }
            #inout { animation-timing-function: ease-in-out; }
            #bezier { animation-timing-function: cubic-bezier(0.2, 1.4, 0.6, -0.4); }
            #steps { animation-timing-function: steps(4); }
            #stepstart { animation-timing-function: steps(3, jump-start); }
            #stepend { animation-timing-function: step-end; }
            """, """<div id="linear"></div><div id="ease"></div><div id="in"></div><div id="out"></div><div id="inout"></div><div id="bezier"></div><div id="steps"></div><div id="stepstart"></div><div id="stepend"></div>""",
            ["opacity"]),
        new("iterations", """
            @keyframes slide { 0% { left: 0px; } 100% { left: 100px; } }
            div { position: relative; width: 10px; height: 10px; animation: slide 0.5s linear; }
            #twice { animation-iteration-count: 2; }
            #alternate { animation-iteration-count: 3; animation-direction: alternate; }
            #reverse { animation-direction: reverse; animation-fill-mode: forwards; }
            #altrev { animation-iteration-count: infinite; animation-direction: alternate-reverse; }
            #delay { animation-delay: 0.3s; animation-fill-mode: backwards; }
            #delaynone { animation-delay: 0.3s; }
            #negative { animation-delay: -0.2s; animation-fill-mode: both; }
            #half { animation-iteration-count: 1.5; animation-fill-mode: forwards; }
            #forwards { animation-fill-mode: forwards; }
            """, """<div id="twice"></div><div id="alternate"></div><div id="reverse"></div><div id="altrev"></div><div id="delay"></div><div id="delaynone"></div><div id="negative"></div><div id="half"></div><div id="forwards"></div>""",
            ["left"]),
        new("keyframes", """
            @keyframes multi {
              0% { opacity: 0; transform: translateX(0px); }
              30% { opacity: 1; animation-timing-function: ease-in; }
              60% { transform: translateX(50px) rotate(30deg); }
              100% { opacity: 0.2; transform: translateX(80px) rotate(90deg); }
            }
            @keyframes partial { 50% { width: 80px; } }
            @keyframes colors { from { color: #ff0000; background-color: rgba(0, 0, 255, 0); } to { color: #0000ff; background-color: rgba(0, 128, 0, 0.8); } }
            div { width: 20px; height: 20px; }
            #multi { animation: multi 1s linear both; }
            #partial { animation: partial 1s linear both; }
            #colors { animation: colors 1s linear both; }
            """, """<div id="multi"></div><div id="partial"></div><div id="colors"></div>""",
            ["opacity", "transform", "width", "color", "background-color"]),
        new("transforms", """
            @keyframes a { from { transform: rotate(0deg) scale(1); } to { transform: rotate(180deg) scale(2); } }
            @keyframes b { from { transform: translate(10px, 0px); } to { transform: scale(2) rotate(45deg); } }
            @keyframes c { from { transform: none; } to { transform: translateY(-20px) skewX(20deg); } }
            @keyframes d { 0% { translate: 0px 0px; rotate: 0deg; scale: 1; } 100% { translate: 30px 10px; rotate: 90deg; scale: 0.5 2; } }
            @keyframes e { from { transform: translate(-50%, -50%) scale(0.6); } to { transform: translate(-50%, -50%) scale(1.2); } }
            div { width: 40px; height: 20px; animation: 1s linear both; }
            #a { animation-name: a; } #b { animation-name: b; } #c { animation-name: c; } #d { animation-name: d; } #e { animation-name: e; }
            """, """<div id="a"></div><div id="b"></div><div id="c"></div><div id="d"></div><div id="e"></div>""",
            ["transform", "translate", "rotate", "scale"]),
        new("effects", """
            @keyframes glow { from { filter: blur(0px) brightness(1); box-shadow: 0 0 0 rgba(255, 200, 0, 0); } to { filter: blur(4px) brightness(1.6); box-shadow: 0 4px 20px rgba(255, 200, 0, 0.8); } }
            @keyframes drop { from { filter: drop-shadow(0 0 0 rgba(0,0,0,0)); } to { filter: drop-shadow(0 6px 8px rgba(0,0,0,0.6)); } }
            @keyframes add { from { filter: none; } to { filter: saturate(2) sepia(0.5); } }
            @keyframes clip { from { clip-path: inset(0 100% 0 0); } to { clip-path: inset(0 0 0 0); } }
            @keyframes vis { 0% { visibility: hidden; } 100% { visibility: visible; } }
            @keyframes spacing { from { letter-spacing: 0.3em; } to { letter-spacing: 0px; } }
            div { width: 40px; height: 20px; animation: 1s linear both; }
            #glow { animation-name: glow; } #drop { animation-name: drop; } #add { animation-name: add; } #clip { animation-name: clip; } #vis { animation-name: vis; } #spacing { animation-name: spacing; }
            """, """<div id="glow"></div><div id="drop"></div><div id="add"></div><div id="clip"></div><div id="vis"></div><div id="spacing">text</div>""",
            ["filter", "box-shadow", "clip-path", "visibility", "letter-spacing"]),
        new("transitions", """
            div { width: 40px; height: 20px; background-color: rgb(200, 100, 50); color: rgb(0, 0, 0); opacity: 1; transform: none; }
            #t1 { transition: opacity 1s linear, transform 0.5s ease-out 0.2s; }
            #t2 { transition: all 1s ease; }
            #t3 { transition: background-color 0.8s cubic-bezier(0.1, 0.7, 0.3, 1), width 1s steps(5); }
            #t4 { transition: color 0.5s; }
            #t4 span { display: inline-block; }
            .go #t1 { opacity: 0.2; transform: translateX(40px) rotate(20deg); }
            .go #t2 { width: 100px; background-color: rgb(20, 40, 200); }
            .go #t3 { width: 90px; background-color: rgba(0, 200, 0, 0.5); }
            .go #t4 { color: rgb(255, 255, 255); }
            """, """<div id="t1"></div><div id="t2"></div><div id="t3"></div><div id="t4"><span id="t4span">inherits</span></div>""",
            ["opacity", "transform", "width", "background-color", "color"], Trigger: "go"),
    ];
}
