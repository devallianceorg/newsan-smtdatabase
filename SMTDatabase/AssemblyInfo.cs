using System;
using System.Reflection;
using System.Xml;

namespace SMTDatabase
{
    class AssemblyInfo
    {
        public static string appInfo()
        {
            Assembly ass = Assembly.GetExecutingAssembly();
            string version;
            if (ass != null)
            {
                System.Diagnostics.FileVersionInfo FVI = System.Diagnostics.FileVersionInfo.GetVersionInfo(ass.Location);

                Version myVersion = GetPublishedVersion();
                version = String.Format("SMTDataBase V.{1:0}", FVI.ProductName, myVersion.ToString());
            }
            else
            {
                version = "Unknown";
            }

            return version;
        }

        public static Version GetPublishedVersion()
        {
            XmlDocument xmlDoc = new XmlDocument();
            Assembly asmCurrent = System.Reflection.Assembly.GetExecutingAssembly();
            string executePath = new Uri(asmCurrent.GetName().CodeBase).LocalPath;

            xmlDoc.Load(executePath + ".manifest");
            string retval = string.Empty;
            if (xmlDoc.HasChildNodes)
            {
                retval = xmlDoc.ChildNodes[1].ChildNodes[0].Attributes.GetNamedItem("version").Value.ToString();
            }
            return new Version(retval);
        }
    }
}
