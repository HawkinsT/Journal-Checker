using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using UnidecodeSharpFork;       // convert unicode strings to ascii with .Unidecode() e.g. Inter-Ação > Inter-Acao

namespace JournalChecker
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Journal Checker (v1.03)";

            string bibFileName = "";
            Boolean inConsole = false;

            if (args.Length != 0)
            {
                inConsole = true;
                bibFileName = args[0].ToString();
            }else
            {
                Console.WriteLine("Please select a .bib file to analyze...");

                OpenFileDialog bibFile = new OpenFileDialog();
                bibFile.Filter = "BibTeX database (*.bib) | *.bib";
                bibFile.ShowDialog();
                bibFileName = bibFile.FileName;

                if (bibFileName == "")
                {
                    Environment.Exit(0);
                }
            }

            if (!File.Exists(bibFileName) || Path.GetExtension(bibFileName) != ".bib")
            {
                Console.WriteLine("Error: " + bibFileName + " does not exist or file type is invalid (must be a .bib).");
                Environment.Exit(0);
            }

            Console.Clear();

            int entries = 0;
            int problems = 0;

            // list of preprint servers (will be coloured green instead of yellow)

            string[] preprints = { "arxiv", "biorxiv", "engrxiv", "chemrxiv", "socarxiv", "psyarxiv", "agrxiv", "paleorxiv", "sportrxiv", "lawarxiv" };

            // using the JCR May 2017 list (about 10,000 high quality peer reviewed journals); can be modified as needed - also replaces ' & ' with ' and ' for consistency

            var jcrList = Properties.Resources.jcrlist.Unidecode().ToLowerInvariant().Replace(" & ", " and ")
                 .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // using the DOAJ June 2017 list (about 10,000 high quality peer reviewed journals); can be modified as needed - also replaces ' & ' with ' and ' for consistency

            var doajList = Properties.Resources.doajlist.Unidecode().ToLowerInvariant().Replace(" & ", " and ")
                 .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // using journals manually copied from Beall's list - this would take many hours to do all by hand so the list is very incomplete (I got through about 5% of it) - also replaces ' & ' with ' and ' for consistency

            var predatoryList = Properties.Resources.predatorylist.Unidecode().ToLowerInvariant().Replace(" & ", " and ")
                 .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var journalsList = jcrList.Concat(doajList).ToArray();

            string fileContents = File.ReadAllText(bibFileName);

            Console.WriteLine("Issues detected in " + Path.GetFileName(bibFileName) + ":\n\n");
            Console.WriteLine("Key".PadRight(26) + "Journal");
            Console.WriteLine(new string('-', 77));

            // @article{([^,]+) || match between '@article{' and ','
            // (?:(?!@[a-z]+{).)+ || continue upto 'journal = {' unless not found before next @ entry (e.g. '@article{'); if so then ignore
            // journal = {(the )?([^}]+) || match after 'journal = {' until '}' (remove leading 'the' if present)
            
            foreach (Match match in Regex.Matches(fileContents.Unidecode(), @"@article{([^,]+)(?:(?!@[a-z]+{).)+journal = {(the )?(.+?(?=},))", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                entries++;

                // replace any : or - (with optional white space padding) with - (no white space), and ' & ' with ' and ', and remove LaTeX escape characters (yes, it's a mess)

                string pattern = @"(\s*)?(:|-)(\s*)?";
                string replacement = "-";
                Regex rgx = new Regex(pattern);
                string[] matchFormattedL = Regex.Split(match.Groups[3].ToString().ToLowerInvariant()
                    .Replace(",", "").Replace("{", "").Replace("}", "").Replace(@"\c", "").Replace(@"\~", "").Replace(@"\`", "").Replace(@"\'", "")
                    .Replace(@"\^", "").Replace("\\\"", "").Replace(@"\H", "").Replace(@"\k", "").Replace(@"\l", "l").Replace(@"\=", "").Replace(@"\b", "")
                    .Replace(@"\.", "").Replace(@"\d", "").Replace(@"\r", "").Replace(@"\u", "").Replace(@"\v", "").Replace(@"\t", "").Replace(@"\o", "o").Replace(" & ", " and "), pattern);
                string matchFormatted = rgx.Replace(match.Groups[3].ToString(), replacement).ToLowerInvariant()
                    .Replace(",", "").Replace("{", "").Replace("}", "").Replace(@"\c", "").Replace(@"\~", "").Replace(@"\`", "").Replace(@"\'", "")
                    .Replace(@"\^", "").Replace("\\\"", "").Replace(@"\H", "").Replace(@"\k", "").Replace(@"\l", "l").Replace(@"\=", "").Replace(@"\b", "")
                    .Replace(@"\.", "").Replace(@"\d", "").Replace(@"\r", "").Replace(@"\u", "").Replace(@"\v", "").Replace(@"\t", "").Replace(@"\o", "o").Replace(" & ", " and ");

                if (!journalsList.Contains(matchFormatted) && !journalsList.Contains(matchFormattedL[0]))
                {
                    problems++;

                    Console.Write(match.Groups[1].Value.PadRight(26));
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    if (preprints.Contains(match.Groups[3].Value.ToLower()))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }else if (predatoryList.Contains(match.Groups[3].Value.ToLower()))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }

                    Console.WriteLine(match.Groups[3].Value.Replace(",", "").Replace("{", "").Replace("}", "").Replace(@"\c", "").Replace(@"\~", "").Replace(@"\`", "").Replace(@"\'", "")
                    .Replace(@"\^", "").Replace("\\\"", "").Replace(@"\H", "").Replace(@"\k", "").Replace(@"\l", "l").Replace(@"\=", "").Replace(@"\b", "")
                    .Replace(@"\.", "").Replace(@"\d", "").Replace(@"\r", "").Replace(@"\u", "").Replace(@"\v", "").Replace(@"\t", "").Replace(@"\o", "o"));

                }

                Console.ResetColor();

            }

            // set up random tips

            string[] tips = { "Check Journal spellings and re-run this scan.",
                "Out of print or forked journals might be flagged erroneously.",
                "Just because a journal's flagged doesn't mean it's not credible; you should use your own judgement.",
                "This program's only as infallible as the person who made it ;)",
                "Use your own judgement; good journals sometimes publish bad papers too.",
                "This is only a guide; predatory journals sometimes steal the name of legitimate journals.",
                "Currently only ~5% of predatory journals on Beall's list are checked by this program."};
            Random rnd = new Random();
            int randomTip = rnd.Next(0, tips.Length);

            if (problems != 0)
            {
                Console.WriteLine(new string('-', 77));

                Console.WriteLine("\n" + problems + " out of " + entries + " entries are potentially problematic.");
                Console.WriteLine("Extra care should be taken when assessing these journals to ensure their quality.\n");

                Console.Write("Key: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Preprint server ");
                Console.ResetColor();
                Console.Write("| ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Journal not listed by JCR or DOAJ ");
                Console.ResetColor();
                Console.Write("| ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Predatory journal (listed by Beall)\n");
                Console.ResetColor();

                Console.WriteLine("Tip: " + tips[randomTip] + "\n");
            }
            else
            {
                Console.Clear();
                Console.WriteLine("No issues found! Entries scanned: " + entries + "\n");
            }

            // check if program is run with argument - if not, wait for ESC to exit
            
            if (inConsole)
            {
                Environment.Exit(0);
            }

            Console.WriteLine("Press ESC to exit");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape);
            {
                // nothing to see here folks
            }

            Environment.Exit(0);
        }
    }
}