using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wox.Plugin;

namespace RemoveUSB
{
    public class Main : IPlugin
    {
        private static ReuSet reuSet = new ReuSet();
        public List<Result> Query(Query query)
        {
            return reuSet.Query(query.Search);
        }

        public void Init(PluginInitContext context)
        {
            reuSet.Load(context.CurrentPluginMetadata.PluginDirectory);
        }
    }
}
