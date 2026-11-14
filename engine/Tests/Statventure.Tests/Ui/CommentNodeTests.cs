using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// Comments are nodes, as in the DOM (GM1 step 1b: today's old game has one in its markup, between two text nodes):
/// the HTML tokenizer's comment forms, a node that keeps the text on either side apart, no box and no text of its own,
/// invisible to selectors (<c>:empty</c>, the structural pseudo-classes and sibling combinators), and no style or layout
/// work when one comes or goes.
/// </summary>
public class CommentNodeTests
{
    static string Kinds(IEnumerable<UiNode> nodes) => string.Join(" ", nodes.Select(n => n switch
    {
        UiText text => "text:" + text.Text,
        UiComment comment => "comment:" + comment.Data,
        UiElement element => element.Tag,
        _ => "?",
    }));

    [Theory]
    [InlineData("a<!--x-->b", "text:a comment:x text:b")]
    [InlineData("a<!-- two words -->b", "text:a comment: two words  text:b")]
    [InlineData("a<!-->b", "text:a comment: text:b")]
    [InlineData("a<!--->b", "text:a comment: text:b")]
    [InlineData("a<!--x--!>b", "text:a comment:x text:b")]
    [InlineData("a<!--x-y->-->b", "text:a comment:x-y-> text:b")]
    [InlineData("a<!--never closed", "text:a comment:never closed")]
    [InlineData("a<!bogus>b", "text:a comment:bogus text:b")]
    [InlineData("a<?pi x?>b", "text:a comment:?pi x? text:b")]
    [InlineData("<!DOCTYPE html>a", "text:a")]
    [InlineData("<b><!--in--></b>c", "b text:c")]
    public void TheTokenizersCommentFormsAreNodes(string html, string expected) => Assert.Equal(expected, Kinds(Markup.Parse(html)));

    [Fact]
    public void ACommentInsideAnElementIsItsChild()
    {
        var b = Markup.Element("<b>one<!--c-->two</b>");
        Assert.Equal("text:one comment:c text:two", Kinds(b.Children));
        // textContent is the text alone; setting it takes the comment away.
        Assert.Equal("onetwo", b.TextContent);
        b.TextContent = "new";
        Assert.Equal("text:new", Kinds(b.Children));
    }

    [Fact]
    public void ACommentSerializesAsWritten()
    {
        var b = Markup.Element("<b>one<!-- c --></b>");
        Assert.Equal("<b>one<!-- c --></b>", Markup.Serialize(b));
    }

    static UiDocument Document(string css, string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet(css, "t");
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    [Fact]
    public void SelectorsDontSeeComments()
    {
        var document = Document("""
            .e:empty { color: red; }
            b:first-child { color: red; }
            u + s { color: red; }
            i > :nth-child(2) { color: red; }
            """, """<div class="e" id="e"><!-- only a comment --></div><i><!--c--><b id="b"></b><!--c--><q id="q"></q></i><p><u></u><!--c--><s id="s"></s></p>""");
        static bool Red(UiDocument d, string id) => d.GetElementById(id)!.Style!.Color == Statventure.Core.Color.FromHex("#ff0000");
        Assert.True(Red(document, "e"));
        Assert.True(Red(document, "b"));
        Assert.True(Red(document, "s"));
        Assert.True(Red(document, "q"));
    }

    [Fact]
    public void TheTextOnEitherSideLaysOutAsOneRun()
    {
        var document = Document("span { font: 16px Lato; }", """<span id="a">one <!-- c --> two</span><br><span id="b">one two</span>""");
        var a = document.GetElementById("a")!.Box!;
        var b = document.GetElementById("b")!.Box!;
        // The comment makes no box, and the spaces on either side collapse to one.
        Assert.Equal(2, a.Children.Count);
        Assert.Equal(b.Fragments!.Sum(f => f.Rect.Width), a.Fragments!.Sum(f => f.Rect.Width), 3);
    }

    [Fact]
    public void ACommentComingOrGoingIsNoWork()
    {
        var document = Document("i > :nth-child(odd) { color: red; } u + s { color: blue; } .e:empty { color: green; }",
            """<i id="i"><b></b><b></b><b></b></i><p id="p"><u></u><s></s></p><div class="e" id="e"></div>""");
        var styles = document.Styles.StylesComputed;
        var laidOut = document.Layout.BoxesLaidOut;
        var comment = new UiComment("c");
        document.GetElementById("i")!.InsertBefore(comment, document.GetElementById("i")!.Children[1]);
        document.GetElementById("p")!.InsertBefore(new UiComment("d"), document.GetElementById("p")!.Children[1]);
        document.GetElementById("e")!.Append(new UiComment("e"));
        document.Update(0);
        comment.Parent!.RemoveChild(comment);
        document.Update(0);
        Assert.Equal(styles, document.Styles.StylesComputed);
        Assert.Equal(laidOut, document.Layout.BoxesLaidOut);
    }
}
