# Share Directory and Set Share Permission Using C# in windows OS.
This Repository is having C# code which allows you to share a directory in C# and set share permission programmatically.

Run this as Administrator.

ShareFolder Method creates the directory and share's it but share permission are Read Only. To change this permission we will call method SetPermission.

Add Reference to System.Management.dll

```Csharp
using System;
using System.Management;
using System.IO;
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
 ```
 
This method will set permission.

Get System Domain.
```Csharp
string Domain = Environment.UserDomainName;
```

In AddPermission Method pass sharedFolderName same as shareName set by you in previous method or the share name of directory for which you want to change permission.
In userName field use name of account for which you want to set/change permission. Example - Everyone, Administrators etc.

```Csharp
using System;
using System.Management;
using System.Globalization;
 public void AddPermissions(string sharedFolderName, string domain, string userName)
        {

            // Step 1 - Getting the user Account Object
            ManagementObject sharedFolder = GetSharedFolderObject(sharedFolderName);
            if (sharedFolder == null)
            {
                System.Diagnostics.Trace.WriteLine("The shared folder with given name does not exist");
                return;
            }

            ManagementBaseObject securityDescriptorObject = sharedFolder.InvokeMethod("GetSecurityDescriptor", null, null);
            if (securityDescriptorObject == null)
            {
                System.Diagnostics.Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Error extracting security descriptor of the shared path {0}.", sharedFolderName));
                return;
            }
            int returnCode = Convert.ToInt32(securityDescriptorObject.Properties["ReturnValue"].Value);
            if (returnCode != 0)
            {
                System.Diagnostics.Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Error extracting security descriptor of the shared path {0}. Error Code{1}.", sharedFolderName, returnCode.ToString()));
                return;
            }

            ManagementBaseObject securityDescriptor = securityDescriptorObject.Properties["Descriptor"].Value as ManagementBaseObject;

            // Step 2 -- Extract Access Control List from the security descriptor
            int existingAcessControlEntriesCount = 0;
            ManagementBaseObject[] accessControlList = securityDescriptor.Properties["DACL"].Value as ManagementBaseObject[];

            if (accessControlList == null)
            {
                // If there aren't any entries in access control list or the list is empty - create one
                accessControlList = new ManagementBaseObject[1];
            }
            else
            {
                // Otherwise, resize the list to allow for all new users.
                existingAcessControlEntriesCount = accessControlList.Length;
                Array.Resize(ref accessControlList, accessControlList.Length + 1);
            }


            // Step 3 - Getting the user Account Object
            ManagementObject userAccountObject = GetUserAccountObject(domain, userName);
            ManagementObject securityIdentfierObject = new ManagementObject(string.Format("Win32_SID.SID='{0}'", (string)userAccountObject.Properties["SID"].Value));
            securityIdentfierObject.Get();

            // Step 4 - Create Trustee Object
            ManagementObject trusteeObject = CreateTrustee(domain, userName, securityIdentfierObject);

            // Step 5 - Create Access Control Entry
            ManagementObject accessControlEntry = CreateAccessControlEntry(trusteeObject, false);

            // Step 6 - Add Access Control Entry to the Access Control List
            accessControlList[existingAcessControlEntriesCount] = accessControlEntry;

            // Step 7 - Assign access Control list to security desciptor 
            securityDescriptor.Properties["DACL"].Value = accessControlList;

            // Step 8 - Assign access Control list to security desciptor 
            ManagementBaseObject parameterForSetSecurityDescriptor = sharedFolder.GetMethodParameters("SetSecurityDescriptor");
            parameterForSetSecurityDescriptor["Descriptor"] = securityDescriptor;
            sharedFolder.InvokeMethod("SetSecurityDescriptor", parameterForSetSecurityDescriptor, null);
        }

        /// <summary>
        /// The method returns ManagementObject object for the shared folder with given name
        /// </summary>
        /// <param name="sharedFolderName">string containing name of shared folder</param>
        /// <returns>Object of type ManagementObject for the shared folder.</returns>

        private static ManagementObject GetSharedFolderObject(string sharedFolderName)
        {
            ManagementObject sharedFolderObject = null;

            //Creating a searcher object to search 
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * from Win32_LogicalShareSecuritySetting where Name = '" + sharedFolderName + "'");
            ManagementObjectCollection resultOfSearch = searcher.Get();
            if (resultOfSearch.Count > 0)
            {
                //The search might return a number of objects with same shared name. I assume there is just going to be one
                foreach (ManagementObject sharedFolder in resultOfSearch)
                {
                    sharedFolderObject = sharedFolder;
                    break;
                }
            }
            return sharedFolderObject;
        }

        /// <summary>
        /// The method returns ManagementObject object for the user folder with given name
        /// </summary>
        /// <param name="domain">string containing domain name of user </param>
        /// <param name="alias">string containing the user's network name </param>
        /// <returns>Object of type ManagementObject for the user folder.</returns>

        private static ManagementObject GetUserAccountObject(string domain, string alias)
        {
            ManagementObject userAccountObject = null;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("select * from Win32_Account where Name = '{0}' and Domain='{1}'", alias, domain));
            ManagementObjectCollection resultOfSearch = searcher.Get();
            if (resultOfSearch.Count > 0)
            {
                foreach (ManagementObject userAccount in resultOfSearch)
                {
                    userAccountObject = userAccount;
                    break;
                }
            }
            return userAccountObject;
        }

        /// <summary>
        /// Returns the Security Identifier Sid of the given user
        /// </summary>
        /// <param name="userAccountObject">The user object who's Sid needs to be returned</param>
        /// <returns></returns>

        private static ManagementObject GetAccountSecurityIdentifier(ManagementBaseObject userAccountObject)
        {
            ManagementObject securityIdentfierObject = new ManagementObject(string.Format("Win32_SID.SID='{0}'", (string)userAccountObject.Properties["SID"].Value));
            securityIdentfierObject.Get();
            return securityIdentfierObject;
        }

        /// <summary>
        /// Create a trustee object for the given user
        /// </summary>
        /// <param name="domain">name of domain</param>
        /// <param name="userName">the network name of the user</param>
        /// <param name="securityIdentifierOfUser">Object containing User's sid</param>
        /// <returns></returns>

        private static ManagementObject CreateTrustee(string domain, string userName, ManagementObject securityIdentifierOfUser)
        {
            ManagementObject trusteeObject = new ManagementClass("Win32_Trustee").CreateInstance();
            trusteeObject.Properties["Domain"].Value = domain;
            trusteeObject.Properties["Name"].Value = userName;
            trusteeObject.Properties["SID"].Value = securityIdentifierOfUser.Properties["BinaryRepresentation"].Value;
            trusteeObject.Properties["SidLength"].Value = securityIdentifierOfUser.Properties["SidLength"].Value;
            trusteeObject.Properties["SIDString"].Value = securityIdentifierOfUser.Properties["SID"].Value;
            return trusteeObject;
        }


        /// <summary>
        /// Create an Access Control Entry object for the given user
        /// </summary>
        /// <param name="trustee">The user's trustee object</param>
        /// <param name="deny">boolean to say if user permissions should be assigned or denied</param>
        /// <returns></returns>

        private static ManagementObject CreateAccessControlEntry(ManagementObject trustee, bool deny)
        {
            ManagementObject aceObject = new ManagementClass("Win32_ACE").CreateInstance();

            aceObject.Properties["AccessMask"].Value = 0x1U | 0x2U | 0x4U | 0x8U | 0x10U | 0x20U | 0x40U | 0x80U | 0x100U | 0x10000U | 0x20000U | 0x40000U | 0x80000U | 0x100000U; // all permissions
            aceObject.Properties["AceFlags"].Value = 0x0U; // no flags
            aceObject.Properties["AceType"].Value = deny ? 1U : 0U; // 0 = allow, 1 = deny
            aceObject.Properties["Trustee"].Value = trustee;
            return aceObject;
        }
```
 
