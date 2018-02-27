using System;
using System.Management;
using System.Globalization;

namespace Aperta_NodalInstaller
{
    class ClassFileShare
    {
        public string ShareFolder(string FolderPath, string ShareName, string Description)
        {
            string strSharePath = FolderPath;
            string strShareName = ShareName;
            string strShareDesc = Description;
            string msg = string.Empty;
            try
            {
                Directory.CreateDirectory(strSharePath);
                ManagementClass oManagementClass = new ManagementClass("Win32_Share");
                ManagementBaseObject inputParameters = oManagementClass.GetMethodParameters("Create");
                ManagementBaseObject outputParameters;
                inputParameters["Description"] = strShareDesc;
                inputParameters["Name"] = strShareName;
                inputParameters["Path"] = strSharePath;
                inputParameters["Type"] = 0x0;//disk drive 
                inputParameters["MaximumAllowed"] = null;
                inputParameters["Access"] = null;

                inputParameters["Password"] = null;


                outputParameters = oManagementClass.InvokeMethod("Create", inputParameters, null);

                if ((uint)(outputParameters.Properties["ReturnValue"].Value) != 0)
                {
                    msg = "There is a problem while sharing the directory.";
                    throw new Exception("There is a problem while sharing the directory.");
                }
                else
                {
                    msg = ("Share Folder has been created with the name :" + strShareName);
                }


            }
            catch (Exception ex)
            {
                msg = (ex.Message.ToString());
            }
            return msg;
        }
    }
}
