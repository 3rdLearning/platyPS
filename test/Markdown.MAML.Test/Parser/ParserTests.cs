﻿using System;
using System.Linq;
using Markdown.MAML.Model;
using Markdown.MAML.Model.Markdown;
using Markdown.MAML.Parser;
using Xunit;

namespace Markdown.MAML.Test.Parser
{
    public class ParserTests
    {
        const string headingText = "Heading Text";
        const string codeBlockText = "Code block text\r\non multiple lines";
        const string paragraphText = "Some text\r\non multiple\r\nlines";
        const string hyperlinkText = "Microsoft Corporation";
        const string hyperlinkUri = "https://go.microsoft.com/fwlink/?LinkID=135175&query=stuff";

        [Fact]
        public void ParsesHeadingsWithHashPrefix()
        {

            for (int i = 1; i <= 6; i++)
            {
                HeadingNode headingNode =
                    this.ParseAndGetExpectedChild<HeadingNode>(
                        new String('#', i) + headingText + "\r\n",
                        MarkdownNodeType.Heading);

                Assert.Equal(i, headingNode.HeadingLevel);
                Assert.Equal(headingText, headingNode.Text);
            }
        }

        [Fact]
        public void ParsesHeadingsWithUnderlines()
        {
            string[] headingUnderlines =
            {
                new String('=', headingText.Length),
                new String('-', headingText.Length)
            };

            for (int i = 1; i <= 2; i++)
            {
                HeadingNode headingNode =
                    this.ParseAndGetExpectedChild<HeadingNode>(
                        headingText + "\r\n" + headingUnderlines[i - 1] + "\r\n",
                        MarkdownNodeType.Heading);

                Assert.Equal(i, headingNode.HeadingLevel);
                Assert.Equal(headingText, headingNode.Text);
            }
        }

        [Fact]
        public void ParsesCodeBlock()
        {
            CodeBlockNode codeBlockNode =
                this.ParseAndGetExpectedChild<CodeBlockNode>(
                    string.Format("```\r\n{0}\r\n```\r\n", codeBlockText),
                    MarkdownNodeType.CodeBlock);

            Assert.Equal(codeBlockText, codeBlockNode.Text);
        }


        [Fact]
        public void ParsesCodeBlockWithLanguageSpecified()
        {
            CodeBlockNode codeBlockNode =
                this.ParseAndGetExpectedChild<CodeBlockNode>(
                    string.Format("```powershell\r\n{0}\r\n```\r\n", codeBlockText),
                    MarkdownNodeType.CodeBlock);

            Assert.Equal(codeBlockText, codeBlockNode.Text);
        }

        [Fact]
        public void ParsesParagraph()
        {
            ParagraphNode paragraphNode =
                this.ParseAndGetExpectedChild<ParagraphNode>(
                    paragraphText,
                    MarkdownNodeType.Paragraph);

            Assert.Equal(paragraphText.Replace("\r\n", " "), paragraphNode.Spans.First().Text);
        }

        [Fact]
        public void ParsesHyperlink()
        {
            ParagraphNode paragraphNode =
                this.ParseAndGetExpectedChild<ParagraphNode>(
                    string.Format(
                        "[{0}]({1})",
                        hyperlinkText,
                        hyperlinkUri),
                    MarkdownNodeType.Paragraph);

            HyperlinkSpan hyperlinkSpan =
                Assert.IsType<HyperlinkSpan>(
                    paragraphNode.Spans.FirstOrDefault());

            Assert.Equal(hyperlinkText, hyperlinkSpan.Text);
            Assert.Equal(hyperlinkUri, hyperlinkSpan.Uri);
        }

        [Fact]
        public void ParsesHyperlinkWithoutLink()
        {
            ParagraphNode paragraphNode =
                this.ParseAndGetExpectedChild<ParagraphNode>(
                    string.Format(
                        "[{0}]()",
                        hyperlinkText),
                    MarkdownNodeType.Paragraph);

            HyperlinkSpan hyperlinkSpan =
                Assert.IsType<HyperlinkSpan>(
                    paragraphNode.Spans.FirstOrDefault());

            Assert.Equal(hyperlinkText, hyperlinkSpan.Text);
            Assert.Equal("", hyperlinkSpan.Uri);
        }

        [Fact]
        public void TextSpansCanContainDoubleQuotes()
        {
            string documentText = @"
# Foo
This is a :""text"" with doublequotes
";
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(
                    documentText);
            var children = documentNode.Children.ToArray();
            Assert.Equal(2, children.Count());
            var spans = Assert.IsType<ParagraphNode>(children[1]).Spans.ToArray();
            Assert.Equal(@"This is a :""text"" with doublequotes", spans[0].Text);
        }

        [Fact]
        public void TextSpansCanContainBrackets()
        {
            string documentText = @"
# Foo
about_Hash_Tables (http://go.microsoft.com/fwlink/?LinkID=135175).
";
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(
                    documentText);
            var children = documentNode.Children.ToArray();
            Assert.Equal(2, children.Count());
            var spans = Assert.IsType<ParagraphNode>(children[1]).Spans.ToArray();
            Assert.Equal(@"about_Hash_Tables (http://go.microsoft.com/fwlink/?LinkID=135175).", spans[0].Text);
        }

        [Fact]
        public void TextSpansCanContainSquareBrackets()
        {
            string documentText = @"
# Foo
Not a hyperlink [PSObject].
";
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(
                    documentText);
            var children = documentNode.Children.ToArray();
            Assert.Equal(2, children.Count());
            var spans = Assert.IsType<ParagraphNode>(children[1]).Spans.ToArray();
            Assert.Equal(@"Not a hyperlink [PSObject].", spans[0].Text);
        }

        [Fact]
        public void ParsesParagraphWithSupportedCharacters()
        {
            const string allCharacterString = 
                "This is a \"test\" string; it's very helpful.  Success: yes!?";

            ParagraphNode paragraphNode =
                this.ParseAndGetExpectedChild<ParagraphNode>(
                    allCharacterString,
                    MarkdownNodeType.Paragraph);

            ParagraphSpan[] spans = paragraphNode.Spans.ToArray();

            Assert.Equal(allCharacterString, spans[0].Text);
        }

        [Fact]
        public void ParsesEscapedLessAndMoreCorrectly()
        {
            const string allCharacterString =
                @"\<port-number\>";

            ParagraphNode paragraphNode =
                this.ParseAndGetExpectedChild<ParagraphNode>(
                    allCharacterString,
                    MarkdownNodeType.Paragraph);

            ParagraphSpan[] spans = paragraphNode.Spans.ToArray();

            Assert.Equal("<port-number>", spans[0].Text);
        }

        [Fact]
        public void ParsesParagraphWithFormattedSpans()
        {
            ParagraphNode paragraphNode =
                this.ParseAndGetExpectedChild<ParagraphNode>(
                    "Normal\r\n\r\nText *Italic*  \r\n\r\n**Bold**\r\n### New header!\r\nBoooo\r\n----\r\n",
                    MarkdownNodeType.Paragraph);

            ParagraphSpan[] spans = paragraphNode.Spans.ToArray();

            Assert.Equal("Normal\r\nText", spans[0].Text);
            Assert.Equal("Italic", spans[1].Text);
            Assert.IsType<HardBreakSpan>(spans[2]);
            Assert.Equal("Bold", spans[3].Text);
        }

        [Fact]
        public void ParsesDocumentWithMultipleNodes()
        {
            string documentText =
                string.Format(
@"
# {0}

{2}

```
{1}
```

## {0}
{2} [{3}]({4})
", headingText, codeBlockText, paragraphText, hyperlinkText, hyperlinkUri);

            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(
                    documentText);

            HeadingNode headingNode =
                this.AssertNodeType<HeadingNode>(
                    documentNode.Children.ElementAtOrDefault(0),
                    MarkdownNodeType.Heading);

            Assert.Equal(headingText, headingNode.Text);
            Assert.Equal(1, headingNode.HeadingLevel);

            CodeBlockNode codeBlockNode =
                this.AssertNodeType<CodeBlockNode>(
                    documentNode.Children.ElementAtOrDefault(2),
                    MarkdownNodeType.CodeBlock);

            Assert.Equal(codeBlockText, codeBlockNode.Text);

            headingNode =
                this.AssertNodeType<HeadingNode>(
                    documentNode.Children.ElementAtOrDefault(3),
                    MarkdownNodeType.Heading);

            Assert.Equal(headingText, headingNode.Text);
            Assert.Equal(2, headingNode.HeadingLevel);

            ParagraphNode paragraphNode =
                this.AssertNodeType<ParagraphNode>(
                    documentNode.Children.ElementAtOrDefault(4),
                    MarkdownNodeType.Paragraph);

            Assert.Equal(paragraphText.Replace("\r\n", " "), paragraphNode.Spans.First().Text);

            HyperlinkSpan hyperlinkSpan =
                Assert.IsType<HyperlinkSpan>(
                    paragraphNode.Spans.ElementAt(1));

            Assert.Equal(hyperlinkText, hyperlinkSpan.Text);
            Assert.Equal(hyperlinkUri, hyperlinkSpan.Uri);
        }

        [Fact]
        public void CanPaserEmptySourceBlock()
        {
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(@"#### 1:

```powershell
```

```powershell
[Parameter(
  ValueFromPipeline = $true,
  ParameterSetName = 'Set 1')]
```
");
            HeadingNode headingNode =
                this.AssertNodeType<HeadingNode>(
                    documentNode.Children.ElementAtOrDefault(0),
                    MarkdownNodeType.Heading);

            Assert.Equal(4, headingNode.HeadingLevel);

            CodeBlockNode codeBlockNode =
                this.AssertNodeType<CodeBlockNode>(
                    documentNode.Children.ElementAtOrDefault(1),
                    MarkdownNodeType.CodeBlock);

            Assert.Equal("", codeBlockNode.Text);
        }

        [Fact]
        public void UnderstandsOneLineBreakVsTwoLineBreaks()
        {
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(@"
1
2

3
");


            ParagraphNode paragraphNode =
                this.AssertNodeType<ParagraphNode>(
                    documentNode.Children.ElementAtOrDefault(0),
                    MarkdownNodeType.Paragraph);

            Assert.Equal("1 2\r\n3", paragraphNode.Spans.First().Text);
        }

        [Fact]
        public void CanUseMultiplyInputStrings()
        {
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(new string[] {
@"# Hello
", // TODO: bug: if there is no new-line after header, it fails to parse it.
@"This is new line",
@"```powershell
Code snippet
```"
                });


            Assert.Equal(3, documentNode.Children.Count());

            HeadingNode node1 =
                this.AssertNodeType<HeadingNode>(
                    documentNode.Children.ElementAtOrDefault(0),
                    MarkdownNodeType.Heading);
            ParagraphNode node2 =
                this.AssertNodeType<ParagraphNode>(
                    documentNode.Children.ElementAtOrDefault(1),
                    MarkdownNodeType.Paragraph);
            CodeBlockNode node3 =
                this.AssertNodeType<CodeBlockNode>(
                    documentNode.Children.ElementAtOrDefault(2),
                    MarkdownNodeType.CodeBlock);
        }

        [Fact]
        public void ParseEscapingSameWayAsGithub()
        {
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(@"
\<
\\<
\\\<
\\\\<
\\\\\<
\\\\[
\
\\
\\\
\\\\
(
)
[
]
\(
\)
\[
\\[
\]
");


            ParagraphNode paragraphNode =
                this.AssertNodeType<ParagraphNode>(
                    documentNode.Children.ElementAtOrDefault(0),
                    MarkdownNodeType.Paragraph);

            // NOTE: to update this example, create a gist on github to check out how it's parsed.
            Assert.Equal(@"< \< \< \\< \\< \\[ \ \ \\ \\ ( ) [ ] ( ) [ \[ ]", paragraphNode.Spans.First().Text);
        }

        [Fact]
        public void GetYamlMetadataWorks()
        {
            var map = MarkdownParser.GetYamlMetadata(@"

---
foo: foo1

bar: bar1
---

foo: bar # this is not part of yaml metadata
"
);
            Assert.Equal("foo1", map["foo"]);
            Assert.Equal("bar1", map["bar"]);
            Assert.Equal(2, map.Count);
        }

        [Fact]
        public void ParsesExample3FromGetPSSnapin()
        {
            string codeblockText = 
@"The first command gets snap-ins that have been added to the current session, including the snap-ins that are installed with Windows PowerShell. In this example, ManagementFeatures is not returned. This indicates that it has not been added to the session.
PS C:\>get-pssnapin

The second command gets snap-ins that have been registered on your system (including those that have already been added to the session). It does not include the snap-ins that are installed with Windows PowerShell.In this case, the command does not return any snap-ins. This indicates that the ManagementFeatures snapin has not been registered on the system.
PS C:\>get-pssnapin -registered

The third command creates an alias, ""installutil"", for the path to the InstallUtil tool in .NET Framework.
PS C:\>set-alias installutil $env:windir\Microsoft.NET\Framework\v2.0.50727\installutil.exe

The fourth command uses the InstallUtil tool to register the snap-in. The command specifies the path to ManagementCmdlets.dll, the file name or ""module name"" of the snap-in.
PS C:\>installutil C:\Dev\Management\ManagementCmdlets.dll

The fifth command is the same as the second command. This time, you use it to verify that the ManagementCmdlets snap-in is registered.
PS C:\>get-pssnapin -registered

The sixth command uses the Add-PSSnapin cmdlet to add the ManagementFeatures snap-in to the session. It specifies the name of the snap-in, ManagementFeatures, not the file name.
PS C:\>add-pssnapin ManagementFeatures

To verify that the snap-in is added to the session, the seventh command uses the Module parameter of the Get-Command cmdlet. It displays the items that were added to the session by a snap-in or module.
PS C:\>get-command -module ManagementFeatures

You can also use the PSSnapin property of the object that the Get-Command cmdlet returns to find the snap-in or module in which a cmdlet originated. The eighth command uses dot notation to find the value of the PSSnapin property of the Set-Alias cmdlet.
PS C:\>(get-command set-alias).pssnapin";
            string descriptionText =
                @"This example demonstrates the process of registering a snap-in on your system and then adding it to your session. It uses ManagementFeatures, a fictitious snap-in implemented in a file called ManagementCmdlets.dll.";
            string documentText = string.Format(@"
#### -------------------------- EXAMPLE 3 --------------------------

```powershell
{0}

```
{1}


### RELATED LINKS
[Online Version:](http://go.microsoft.com/fwlink/p/?linkid=289570)
[Get-PSSnapin]()
[Remove-PSSnapin]()
[about_Profiles]()
[about_PSSnapins]()

## Clear-History

### SYNOPSIS
Deletes entries from the command history.

### DESCRIPTION
The Clear-History cmdlet deletes commands from the command history, that is, the list of commands entered during the current session.
Without parameters, Clear-History deletes all commands from the session history, but you can use the parameters of Clear-History to delete selected commands.

### PARAMETERS

#### CommandLine [String[]]

```powershell
[Parameter(ParameterSetName = 'Set 2')]
```

Deletes commands with the specified text strings. If you enter more than one string, Clear-History deletes commands with any of the strings.

", codeblockText, descriptionText);

            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode =
                markdownParser.ParseString(
                    documentText);

            HeadingNode headingNode =
                this.AssertNodeType<HeadingNode>(
                    documentNode.Children.ElementAtOrDefault(0),
                    MarkdownNodeType.Heading);

            Assert.Equal(4, headingNode.HeadingLevel);

            CodeBlockNode codeBlockNode =
                this.AssertNodeType<CodeBlockNode>(
                    documentNode.Children.ElementAtOrDefault(1),
                    MarkdownNodeType.CodeBlock);

            Assert.Equal(codeblockText, codeBlockNode.Text);

            ParagraphNode paragraphNode =
                this.AssertNodeType<ParagraphNode>(
                    documentNode.Children.ElementAtOrDefault(2),
                    MarkdownNodeType.Paragraph);
            Assert.Equal(descriptionText, paragraphNode.Spans.First().Text);
        }

        private TNode ParseAndGetExpectedChild<TNode>(
            string markdownString, 
            MarkdownNodeType expectedNodeType)
        {
            MarkdownParser markdownParser = new MarkdownParser();
            DocumentNode documentNode = markdownParser.ParseString(markdownString);
            return 
                this.AssertNodeType<TNode>(
                    documentNode.Children.FirstOrDefault(),
                    expectedNodeType);
        }

        private TNode AssertNodeType<TNode>(
            MarkdownNode markdownNode,
            MarkdownNodeType expectedNodeType)
        {
            Assert.NotNull(markdownNode);
            Assert.Equal(expectedNodeType, markdownNode.NodeType);
            return Assert.IsType<TNode>(markdownNode);
        }
    }
}
