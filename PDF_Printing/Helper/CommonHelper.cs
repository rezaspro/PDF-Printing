using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PDF_Printing.Helper
{
    public static class CommonHelper
    {
        public static string Resources = "Resources";
        public static string Stampe = "Stampe";
        public static DirectoryInfo GetRequiredDirectory(RequiredDirectory value = RequiredDirectory.Base)
        {
            DirectoryInfo returnDir = null;
            try
            {
                //init     
                DirectoryInfo baseDir = new DirectoryInfo(Application.StartupPath);

                if (value == RequiredDirectory.Base)
                {
                    returnDir = baseDir;
                }
                else if (value == RequiredDirectory.ResourcesDirectory)
                {
                    returnDir = new DirectoryInfo(baseDir.FullName + "\\" + Resources);
                }
                else if (value == RequiredDirectory.StampeDirectory)
                {
                    returnDir = new DirectoryInfo(baseDir.FullName + "\\" + Stampe);
                }

                //create new directory if directory is not exist
                if (returnDir != null && returnDir.Exists == false)
                {
                    returnDir.Create();
                }
            }
            catch (Exception ex)
            {
                //Log.Error(ex);
            }
            return returnDir;
        }

        public enum RequiredDirectory
        {
            Base,
            ResourcesDirectory,
            StampeDirectory,
        }
    }
}
