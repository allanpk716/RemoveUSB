using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RemoveUSB;
using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace RemoveUSB.Tests
{
    [TestClass()]
    public class ReuSetTests
    {
        [TestMethod()]
        public void QueryTest()
        {
            ReuSet rs = new ReuSet();
            List<Wox.Plugin.Result> sd = rs.Query(string.Empty);
        }
    }
}
