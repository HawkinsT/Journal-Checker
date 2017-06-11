using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;

namespace JournalChecker
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Journal Checker (v1.02)";

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

            // using the JCR 2016 list (about 10,000 high quality peer reviewed journals); can be modified as needed
            var journalsList = Properties.Resources.jcrlist
                 .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var journalsListLower = journalsList.Select(s => s.ToLowerInvariant()).ToArray();

            string fileContents = File.ReadAllText(bibFileName);

            Console.WriteLine("Journals in " + Path.GetFileName(bibFileName) + " not listed by JCR (preprint servers are green):\n\n");
            Console.WriteLine("Key".PadRight(26) + "Journal");
            Console.WriteLine(new string('-', 77));

            // @article{([^,]+) || match between '@article{' and ','
            // (?:(?!@[a-z]+{).)+ || continue upto 'journal = {' unless not found before next @ entry (e.g. '@article{'); if so then ignore
            // journal = {(the )?([^}]+) || match after 'journal = {' until '}' (remove leading 'the' if present)
            foreach (Match match in Regex.Matches(fileContents, @"@article{([^,]+)(?:(?!@[a-z]+{).)+journal = {(the )?([^}]+)", RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                entries++;

                // replace any : or - (with optional white space padding) with - (no white space)
                string pattern = @"(\s*)?(:|-)(\s*)?";
                string replacement = "-";
                Regex rgx = new Regex(pattern);
                string[] matchFormattedL = Regex.Split(match.Groups[3].ToString().ToLowerInvariant().Replace(",", ""), pattern);
                string matchFormatted = rgx.Replace(match.Groups[3].ToString(), replacement).ToLowerInvariant().Replace(",", "");

                if (!journalsListLower.Contains(matchFormatted) && !journalsListLower.Contains(matchFormattedL[0]))
                {
                    problems++;

                    Console.Write(match.Groups[1].Value.PadRight(26));
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    if (preprints.Contains(match.Groups[3].Value.ToLower()))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }

                    Console.WriteLine(match.Groups[3].Value);
                }

                Console.ResetColor();

            }

            if (problems != 0)
            {
                Console.WriteLine("\n" + problems + " out of " + entries + " entries not listed in JCR (or are spelling variants).\nExtra care should be taken when assessing these journals to ensure their quality.\n");
            }
            else
            {
                Console.Clear();
                Console.WriteLine("No issues found! Entries scanned: " + entries + "\n");
            }

            // check if program run from console - if not, wait for ESC to exit.
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
