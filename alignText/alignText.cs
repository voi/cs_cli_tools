using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System.Xml;
using System.Collections;
using System.Collections.Generic;

namespace AlignText
{
    public class AlignTextApi
    {
        ////
        class TokenizedLine
        {
            //
            static Regex regexJaKatakanaWidth2 = new Regex(@"ｳﾞ|ｶﾞ|ｷﾞ|ｸﾞ|ｹﾞ|ｺﾞ|ｻﾞ|ｼﾞ|ｽﾞ|ｾﾞ|ｿﾞ|ﾀﾞ|ﾁﾞ|ﾂﾞ|ﾃﾞ|ﾄﾞ|ﾊﾞ|ﾋﾞ|ﾌﾞ|ﾍﾞ|ﾎﾞ|ﾊﾟ|ﾋﾟ|ﾌﾟ|ﾍﾟ|ﾎﾟ", RegexOptions.Compiled);
            static Regex regexJaKatakanaWidth1 = new Regex(@"ｦ|ｧ|ｨ|ｩ|ｪ|ｫ|ｬ|ｭ|ｮ|ｯ|ｰ|ｱ|ｲ|ｳ|ｴ|ｵ|ｶ|ｷ|ｸ|ｹ|ｺ|ｻ|ｼ|ｽ|ｾ|ｿ|ﾀ|ﾁ|ﾂ|ﾃ|ﾄ|ﾅ|ﾆ|ﾇ|ﾈ|ﾉ|ﾊ|ﾋ|ﾌ|ﾍ|ﾎ|ﾏ|ﾐ|ﾑ|ﾒ|ﾓ|ﾔ|ﾕ|ﾖ|ﾗ|ﾘ|ﾙ|ﾚ|ﾛ|ﾜ|ﾝ", RegexOptions.Compiled);
            static Regex regexEachChar = new Regex(@".", RegexOptions.Compiled);

            //
            List<string> lineTokens;

            //
            public int CurrentTextWidth { get; private set; }

            public TokenizedLine(IEnumerable<string> tokens)
            {
                this.lineTokens = new List<string>(tokens);
                this.setCurrentTextWidth();
            }

            public void PaddingText(int alignWidth, Func<int, int, string> paddinger)
            {
                if(this.lineTokens.Count > 1)
                {
                    var text = this.lineTokens[0].TrimEnd();

                    this.lineTokens.RemoveAt(0);
                    this.lineTokens[0] = (text + 
                        paddinger(this.CurrentTextWidth, Math.Max(alignWidth - this.CurrentTextWidth, 0)) + 
                        this.lineTokens[0]);

                    this.setCurrentTextWidth();
                }
            }

            public override string ToString()
            {
                return this.lineTokens.FirstOrDefault();
            }

            void setCurrentTextWidth()
            {
                this.CurrentTextWidth = ((this.lineTokens.Count > 1) ?
                    TokenizedLine.getTextWidth(this.lineTokens[0]) : 0);
            }

            static int getTextWidth(string str)
            {
                var strToWidth = str.TrimEnd();

                strToWidth = regexJaKatakanaWidth2.Replace(strToWidth, "..");
                strToWidth = regexJaKatakanaWidth1.Replace(strToWidth, ".");
                strToWidth = regexEachChar.Replace(strToWidth, (match) => {
                    return ((match.Value[0] <= 0x7F) ? "." : "..");
                });

                return strToWidth.Length;
            }

            public static string paddingBySpace(int textWidth, int margin)
            {
                return String.Empty.PadRight(margin + 1, ' ');
            }

            public static string paddingByTab(int textWidth, int margin)
            {
                return String.Empty.PadRight((margin / 4 + ((textWidth % 4 > 0) ? 2 : 1)), '\t');
            }
        }

        ///
        enum ApiMode {
            None = 0,
            Pipe,
            Clipboard
        }

        IEnumerable<string> AlignTextLines(Options options, IEnumerable<string> source)
        {
            //
            var workingLines = new List<TokenizedLine>(
                source.Select((line) => { return new TokenizedLine(options.Tokenize(line)); }));

            ////
            int alignWidth = workingLines.Select((line) => { return line.CurrentTextWidth; }).Max();

            while(alignWidth > 0)
            {
                foreach(var line in workingLines)
                {
                    line.PaddingText(alignWidth, options.Paddinger);
                }

                alignWidth = workingLines.Select((line) => { return line.CurrentTextWidth; }).Max();
            }

            ////
            return workingLines.Select((line) => { return line.ToString(); });
        }

        //
        class Options
        {
            public ApiMode Mode { get; private set; }

            Regex Pattern { get; set; }
            string Separeter { get; set; }
            int SeparateCount { get; set; }
            char[] Delimiters { get; set; }

            public Func<int, int, string> Paddinger { get; private set; }

            public Options(IEnumerable<string> inputs)
            {
                //
                this.Mode = ApiMode.None;
                this.Separeter = "\f$&";
                this.SeparateCount = 1;
                this.Delimiters = new char[]{ '\f' };
                this.Paddinger = TokenizedLine.paddingBySpace;

                //
                var patternText = String.Empty;
                var isRegex = false;

                foreach(var arg in inputs)
                {
                    switch(arg)
                    {
                        case "-p":
                            this.Mode = ApiMode.Pipe;
                            break;

                        case "-c":
                            this.Mode = ApiMode.Clipboard;
                            break;

                        case "-g":  // align all target text
                            this.SeparateCount = -1;
                            break;

                        case "-t":  // use tab ('\t')
                            this.Paddinger = TokenizedLine.paddingByTab;
                            break;

                        case "-a":  // align at after specified pattern
                            this.Separeter = "$&\f";
                            break;

                        case "-r":  // pattern as regexp
                            isRegex = true;
                            break;

                        default:    // text to align
                            patternText = arg;
                            break;
                    }
                }

                ////
                this.Pattern = new Regex((isRegex ? patternText : Regex.Escape(patternText)), RegexOptions.Compiled);
            }

            public IEnumerable<string> Tokenize(string line)
            {
                return this.Pattern.Replace(line, this.Separeter, this.SeparateCount).Split(this.Delimiters);
            }
        }

        //
        [STAThread]
        static void Main()
        {
            var api = new AlignTextApi();
            var options = new Options(Environment.GetCommandLineArgs());

            //
            if(options.Mode == ApiMode.Clipboard)
            {
                var delimiters = new string[]{ Environment.NewLine };
                var lines = Clipboard.GetText().Split(delimiters, StringSplitOptions.None);

                var alignedLines = api.AlignTextLines(options, lines);

                Clipboard.SetText(String.Join(Environment.NewLine, alignedLines));
            }

            //
            if(options.Mode == ApiMode.Pipe)
            {
                var lines = new List<string>();

                while(Console.In.Peek() > 0)
                {
                    lines.Add(Console.In.ReadLine());
                }

                foreach(var line in api.AlignTextLines(options, lines))
                {
                    Console.Out.WriteLine(line);
                }
            }
        }
    }
}
