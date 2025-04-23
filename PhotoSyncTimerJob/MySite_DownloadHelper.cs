

#region UserDirectives
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using Microsoft.SharePoint;
using System.Reflection;
using Microsoft.Office.Server.UserProfiles;
using System.Drawing;
#endregion UserDirectives

namespace CC_PhotoSyncTimerJob
{
    class Mercer_MySite_DownloadHelper
    {

        #region DeletePhotosFromLocalFolder
        /// <summary>
        /// Deletes the downloaded pics from local folder
        /// </summary>
        /// <param name="path"></param>
        public void DeletePhotosFromLocalFolder(string path)
        {
            //Get files from local folder
            DirectoryInfo di = new DirectoryInfo(path);
            FileInfo[] finfos = di.GetFiles();
            try
            {
                foreach (FileInfo fi in finfos)
                {
                    fi.Delete();
                }
            }
            catch (System.IO.IOException ex)
            {
                Microsoft.Office.Server.Diagnostics.PortalLog.LogString("Mercer_MySite_DownloadHelper-DeletePhotosFromLocalFolder:: Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
            }

        }//DeletePhotosFromLocalFolder
        #endregion DeletePhotosFromLocalFolder

        #region DownLoadAttachment
        /// <summary>
        /// Download Images
        /// </summary>
        /// <param name="strURL"></param>
        /// <param name="strFileName"></param>
        /// <param name="path"></param>
        public void DownLoadAttachment(string strURL, string strFileName, string path)
        {
            HttpWebRequest request;
            HttpWebResponse response = null;
            try
            {

                request = (HttpWebRequest)WebRequest.Create(strURL);
                request.Credentials = System.Net.CredentialCache.DefaultCredentials;
                request.Timeout = 10000;
                request.AllowWriteStreamBuffering = false;
                response = (HttpWebResponse)request.GetResponse();
                Stream s = response.GetResponseStream();
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                FileStream fs = new FileStream(path + strFileName, FileMode.Create);
                byte[] read = new byte[256];
                int count = s.Read(read, 0, read.Length);
                while (count > 0)
                {
                    fs.Write(read, 0, count);
                    count = s.Read(read, 0, read.Length);
                }
                fs.Close();
                s.Close();
                response.Close();
            }//Try
            catch (Exception ex)
            {
                Microsoft.Office.Server.Diagnostics.PortalLog.LogString("Mercer_MySite_DownloadHelper-DownLoadAttachmen:: Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
            } //catch

        }//DownLoadAttachment
        #endregion DownLoadAttachment

        #region UploadProfileImages
        /// <summary>
        /// Upload Profile Images
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <param name="path"></param>
        public void UploadProfileImages(string siteUrl, string path, string fname)
        {
            string indivisualUserName = string.Empty;
            string userNamefromImage = string.Empty;
            string userNamefromrofile = string.Empty;

            string _FirstNameUserProfile = string.Empty;
            string _LastNameUserProfile = string.Empty;
            string _DomainNameImage = string.Empty;

         
            string _AliasNameImage = string.Empty;
            string _AccountNameImage = string.Empty;
            string accountName=string.Empty;

            using (SPSite site = new SPSite(siteUrl))
            {
                SPServiceContext serverContext = SPServiceContext.GetContext(site);
                UserProfileManager userProfileManager = new UserProfileManager(serverContext);    
                using (SPWeb web = site.RootWeb)
                {
                    try
                    {
                        SPFolder subfolderForPictures = web.Folders[fname];
                        DirectoryInfo dir = new DirectoryInfo(path);
                        FileInfo[] Images = dir.GetFiles();
                        foreach (FileInfo img in Images)
                        {
                            string imageFilePath = path + "\\" + img.Name;                           
                            char[] delimiterChars = { '_', '.' };
                            string text = img.Name;
                            string[] words = text.Split(delimiterChars);
                            _DomainNameImage = words[0].ToUpper();
                            _AliasNameImage = words[1].ToUpper();                                                      
                            _AccountNameImage = _DomainNameImage + "\\" + _AliasNameImage;
                            try
                            {
                                if (userProfileManager.UserExists(_AccountNameImage))
                                {    
                                    //changes done on 1/4/2014 by Subha
                                   // UploadPhoto(text, imageFilePath, subfolderForPictures);
                                   // SetPictureUrl(text, subfolderForPictures, userProfileManager.GetUserProfile(_AccountNameImage));
                                    UploadPhoto(_AccountNameImage, imageFilePath, subfolderForPictures);
                                    SetPictureUrl(_AccountNameImage, subfolderForPictures, userProfileManager.GetUserProfile(_AccountNameImage));
                                }
                                else
                                {
                                    Microsoft.Office.Server.Diagnostics.PortalLog.LogString("User {0} does not exist ::", _AccountNameImage);

                                }
                            }
                            catch(Exception ex)
                            {
                                Microsoft.Office.Server.Diagnostics.PortalLog.LogString(" Mercer_MySite_DownloadHelper-UploadProfileImages::Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
                            }
                           
                        }// foreach (FileInfo img in Images)
                    }//try

                    catch (Exception ex)
                    {
                        Microsoft.Office.Server.Diagnostics.PortalLog.LogString(" Mercer_MySite_DownloadHelper-UploadProfileImages::Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
                    }

                }//SPWeb
            }//SPSIte
        } // end of function UploadProfileImages
        #endregion UploadProfileImages

        #region UploadPhoto
        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="imageFilePath"></param>
        /// <param name="subfolderForPictures"></param>
        public static void UploadPhoto(string accountName, string imageFilePath, SPFolder subfolderForPictures)
        {
            if (!File.Exists(imageFilePath) || Path.GetExtension(imageFilePath).Equals(".gif"))
            {

                Microsoft.Office.Server.Diagnostics.PortalLog.LogString("File '{0}' does not exist or has invalid extension", imageFilePath);
            }
            else
            {
                try
                {
                FileStream file = File.Open(imageFilePath, FileMode.Open);
                BinaryReader reader = new BinaryReader(file);

                if (subfolderForPictures != null)
                {
                    // try casting length (long) to int
                    byte[] buffer = reader.ReadBytes((int)file.Length);

                    int largeThumbnailSize = 300;
                    int mediumThumbnailSize = 72;
                    int smallThumbnailSize = 48;

                    using (MemoryStream stream = new MemoryStream(buffer))
                    {
                        using (Bitmap bitmap = new Bitmap(stream, true))
                        {
                            CreateThumbnail(bitmap, largeThumbnailSize, largeThumbnailSize, subfolderForPictures, accountName + "_LThumb.jpg");
                            CreateThumbnail(bitmap, mediumThumbnailSize, mediumThumbnailSize, subfolderForPictures, accountName + "_MThumb.jpg");
                            CreateThumbnail(bitmap, smallThumbnailSize, smallThumbnailSize, subfolderForPictures, accountName + "_SThumb.jpg");
                        }
                    }
                }
                file.Close();
                Microsoft.Office.Server.Diagnostics.PortalLog.LogString("Uploading image '{0}' for user '{1}'", imageFilePath, accountName);
            }
                catch(Exception ex)
                {
                    Microsoft.Office.Server.Diagnostics.PortalLog.LogString(" Mercer_MySite_DownloadHelper-UploadPhoto::Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
                }
            }
        } // end of function
        #endregion UploadPhoto

        #region CreateThumbnail
        /// <summary>
        /// Get sealed function to generate new thumbernails
        /// </summary>
        /// <param name="original"></param>
        /// <param name="idealWidth"></param>
        /// <param name="idealHeight"></param>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static SPFile CreateThumbnail(Bitmap original, int idealWidth, int idealHeight, SPFolder folder, string fileName)
        {
           
             SPFile file = null;
            Assembly userProfilesAssembly = typeof(UserProfile).Assembly;
            Type userProfilePhotosType = userProfilesAssembly.GetType("Microsoft.Office.Server.UserProfiles.UserProfilePhotos");
            MethodInfo[] mi_methods = userProfilePhotosType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static);

            MethodInfo mi_CreateThumbnail = mi_methods[0];
            if (mi_CreateThumbnail != null)
            {
                file = (SPFile)mi_CreateThumbnail.Invoke(null, new object[] { original, idealWidth, idealHeight, folder, fileName, null });
            }

             return file;
        
          
        } // end of functionCreateThumbnail
        #endregion CreateThumbnail

        #region SetPictureUrl
        /// <summary>
        /// Set the Picture url
        /// </summary>
        /// <param name="accountName"></param>
        /// <param name="subfolderForPictures"></param>
        /// <param name="userProfile"></param>
        public static void SetPictureUrl(string accountName, SPFolder subfolderForPictures, UserProfile userProfile)
        {
            try 
            {
                string account = accountName.Substring(0, accountName.IndexOf("_")) + "\\" + accountName.Substring(accountName.IndexOf("_") + 1);
                string pictureUrl = String.Format("{0}/{1}/{2}_MThumb.jpg", subfolderForPictures.ParentWeb.Site.Url, subfolderForPictures.Url, accountName);
                userProfile["PictureUrl"].Value = pictureUrl;
                userProfile.Commit();
            }
            catch(Exception ex)
            {
                Microsoft.Office.Server.Diagnostics.PortalLog.LogString("ProfilePictureSync-SetPictureUrl:Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
            }
            
        } // end of function  SetPictureUrl
        #endregion SetPictureUrl
    }
}
