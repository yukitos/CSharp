using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IniUtil
{
    public class IniFile
    {
        public IniFile()
        {
            this.Sections = new List<IniSection>();
            this.NameValueDelimiter = '=';
            this.DuplicatedKeyNameMode = DuplicatedKeyNameMode.Ignore;
            this.CommentChars = new[] { ';' };
            this.AllowInlineComment = false;
            this.AllowLineContinuation = false;
        }

        public IList<IniSection> Sections { get; private set; }

        public char NameValueDelimiter { get; set; }

        public DuplicatedKeyNameMode DuplicatedKeyNameMode { get; set; }

        public char[] CommentChars { get; set; }

        public bool AllowInlineComment { get; set; }

        public bool AllowLineContinuation { get; set; }

        public IniSection this[string sectionName]
        {
            get
            {
                return Sections
                    .Where(i => i.Name == sectionName)
                    .First();
            }
        }

        public IniSection Find(string sectionName)
        {
            return Sections
                .Where(i => i.Name == sectionName)
                .FirstOrDefault();
        }

        public void Load(string fileName)
        {
            Load(fileName, Encoding.UTF8);
        }

        public void Load(string fileName, Encoding encoding)
        {
            using (var reader = new StreamReader(fileName, encoding))
            {
                Load(reader);
            }
        }

        public void LoadString(string data)
        {
            LoadString(data, Encoding.UTF8);
        }

        public void LoadString(string data, Encoding encoding)
        {
            var bytes = encoding.GetBytes(data);
            using (var memoryStream = new MemoryStream(bytes))
            using (var reader = new StreamReader(memoryStream))
            {
                Load(reader);
            }
        }

        private void Load(StreamReader reader)
        {
            string currentSectionName = "global";
            Sections.Add(new IniSection(currentSectionName));

            int lineNumber = 0;
            bool lineContinue = false;
            string currentLine = null;

            while (!reader.EndOfStream)
            {
                lineNumber++;

                var line = reader.ReadLine();

                // Handle comment
                var index = line.IndexOfAny(CommentChars);
                if (index == 0) continue;
                if (AllowInlineComment && index > -1)
                {
                    bool continuation = false;
                    if (AllowLineContinuation && line[line.Length - 1] == '\\')
                    {
                        continuation = true;
                    }

                    line = line.Substring(0, index).TrimEnd();

                    if (continuation)
                    {
                        line += '\\';
                    }
                }

                if (AllowLineContinuation)
                {
                    if (line[line.Length - 1] == '\\')
                    {
                        if (lineContinue)
                        {
                            currentLine += line.TrimEnd(new[] { ' ', '\\' });
                        }
                        else
                        {
                            currentLine = line.TrimEnd(new[] { ' ', '\\' });
                        }
                        lineContinue = true;
                        continue;
                    }
                    else
                    {
                        lineContinue = false;
                        currentLine += line;
                    }
                }
                else
                {
                    currentLine = line;
                }

                currentLine = currentLine.Trim();

                // Skip empty line
                if (currentLine.Length == 0) continue;

                // Handle section
                if (currentLine[0] == '[' && currentLine[currentLine.Length - 1] == ']')
                {
                    currentSectionName = currentLine.Substring(1, currentLine.Length - 2);
                    var existingSection = Sections
                        .Where(i => i.Name.ToUpperInvariant() == currentSectionName.ToUpperInvariant())
                        .FirstOrDefault();
                    if (existingSection != null)
                    {
                        throw new InvalidDataException("Duplicate section is not allowed.");
                    }

                    Sections.Add(new IniSection(currentSectionName));
                    continue;
                }

                // Handle properties
                index = currentLine.IndexOf(NameValueDelimiter);
                KeyValuePair<string, string> item;

                if (index > -1)
                {
                    item = new KeyValuePair<string, string>(currentLine.Substring(0, index), currentLine.Substring(index + 1));
                }
                else
                {
                    item = new KeyValuePair<string, string>(currentLine, currentLine);
                }

                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    throw new InvalidDataException("Empty property name is not allowed. Line: " + lineNumber);
                }

                var currentSection = this[currentSectionName];
                var existingProperty = currentSection.Properties
                    .Where(i => i.Key.ToUpperInvariant() == item.Key.ToUpperInvariant())
                    .FirstOrDefault();
                if (string.IsNullOrEmpty(existingProperty.Key))
                {
                    currentSection.Properties.Add(item);
                }
                else
                {
                    switch (DuplicatedKeyNameMode)
                    {
                        case DuplicatedKeyNameMode.Abort:
                            throw new InvalidDataException("Duplicated property is not allowed. Line: " + lineNumber);
                        case DuplicatedKeyNameMode.Ignore:
                            // Do nothing here.
                            break;
                        case DuplicatedKeyNameMode.Allow:
                            // Concatenate values separated by semicolons.
                            var currentValue = currentSection.Properties[item.Key];
                            currentSection.Properties[item.Key] = currentValue + ";" + item.Value;
                            break;
                    }
                }

                currentLine = null;
            }
        }
    }
}
