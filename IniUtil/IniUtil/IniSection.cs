using System.Collections.Generic;
using System.Linq;

namespace IniUtil
{
    public class IniSection
    {
        public IniSection(string name = null)
        {
            this.Name = name;
            this.Properties = new Dictionary<string, string>();
        }

        public string Name { get; set; }

        public IDictionary<string, string> Properties { get; private set; }

        public string this[string key]
        {
            get
            {
                return Properties
                    .Where(i => i.Key == key)
                    .Select(i => i.Value)
                    .FirstOrDefault();
            }
        }
    }
}
