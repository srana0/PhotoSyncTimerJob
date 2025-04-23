
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SharePoint;
using Microsoft.Office.Server;
using Microsoft.Office.Server.UserProfiles;
using Microsoft.SharePoint.Administration;
using System.Xml;


namespace CC_PhotoSyncTimerJob
{

    class ProfilePictureSync : SPJobDefinition
    {
           public ProfilePictureSync():base()
       {
       }
       public ProfilePictureSync(string jobName,SPService service,SPServer server,SPJobLockType targetType) 
           : base(jobName,service,server,targetType)
       {
           this.Title = jobName;
       }

       public ProfilePictureSync(string jobName, SPWebApplication webApplication)
           : base(jobName,webApplication,null,SPJobLockType.ContentDatabase)
       {
           this.Title = jobName;
       }


       public override void Execute(Guid targetInstanceId)
       {
           string siteUrl = string.Empty;
           string path = string.Empty;
           int threshold_LowerLimit = 0;
           int threshold_UpperLimit = 0;
           string strListName = string.Empty;
           string objListsServiceUrl = string.Empty;
           int timeSpanInDaysLogic = 0;


           base.Execute(targetInstanceId);
           SPWebApplication webApplication = this.Parent as SPWebApplication;
           SPContentDatabase contentDb = webApplication.ContentDatabases[targetInstanceId];
           using (SPWeb web = contentDb.Sites[0].RootWeb)
           {
               if (web.AllProperties.ContainsKey("DestinationSiteUrl"))
               {
                   siteUrl = web.AllProperties["DestinationSiteUrl"].ToString();

               }
               if (web.AllProperties.ContainsKey("LocalFolderName"))
               {
                   path = web.AllProperties["LocalFolderName"].ToString();
               }

               if (web.AllProperties.ContainsKey("threshold_LowerLimit"))
               {
                   threshold_LowerLimit = Convert.ToInt32(web.AllProperties["threshold_LowerLimit"]);
               }
               if (web.AllProperties.ContainsKey("threshold_UpperLimit"))
               {
                   threshold_UpperLimit = Convert.ToInt32(web.AllProperties["threshold_UpperLimit"]);
               }                            

               if (web.AllProperties.ContainsKey("strListName"))
               {
                   strListName = web.AllProperties["strListName"].ToString();
               }

               if (web.AllProperties.ContainsKey("objListsServiceUrl"))
               {
                   objListsServiceUrl = web.AllProperties["objListsServiceUrl"].ToString();
               }

               if (web.AllProperties.ContainsKey("timeSpanInDaysLogic"))
               {
                   timeSpanInDaysLogic =Convert.ToInt32(web.AllProperties["timeSpanInDaysLogic"]);
               }

           }

          
           string strURL = "";
           string strFileName = "";
           string modifiedDate = "";
           

           com.mercer.mysites.Lists.Lists objListsService = new com.mercer.mysites.Lists.Lists();          
           objListsService.Url = objListsServiceUrl;
           objListsService.Credentials = System.Net.CredentialCache.DefaultCredentials;
           Mercer_MySite_DownloadHelper helper = new Mercer_MySite_DownloadHelper();

           try
           {
               //get the Data from the List  
               System.Xml.XmlDocument xdListData = new System.Xml.XmlDocument();
               System.Xml.XmlNode xnQuery = xdListData.CreateElement("Query");
               System.Xml.XmlNode xnViewFields = xdListData.CreateElement("ViewFields");
               System.Xml.XmlNode xnQueryOptions = xdListData.CreateElement("QueryOptions");
               //List View Threshold is 5000 by default for mercer account
               xnQuery.InnerXml = "<OrderBy xmlns=\"http://schemas.microsoft.com/sharepoint/soap/\">"
                                        + "<FieldRef Name=\"FileLeafRef\" />"
                                   + "</OrderBy>"
                                   + "<Where xmlns=\"http://schemas.microsoft.com/sharepoint/soap/\">"
                                       + "<And>"
                                          + "<Geq>"
                                              + "<FieldRef Name=\"ID\" /><Value Type=\"Counter\">" + threshold_LowerLimit + "</Value>"
                                          + "</Geq>"
                                          + "<Leq>"
                                               + "<FieldRef Name=\"ID\" /><Value Type=\"Counter\">" + threshold_UpperLimit + "</Value>"
                                           + "</Leq>"
                                       + "</And>"
                                   + "</Where>";

               xnViewFields.InnerXml = "";
               xnQueryOptions.InnerXml = "<IncludeAttachmentUrls>TRUE</IncludeAttachmentUrls>";
               //Calculate Today's Date
               DateTime todaysDate = DateTime.Now;

               System.Xml.XmlNode xnListData = objListsService.GetListItems(strListName, null, xnQuery, xnViewFields, null, xnQueryOptions, null);
               XmlNodeList oNodes = xnListData.ChildNodes;
               foreach (XmlNode node in oNodes)
               {
                   XmlNodeReader objReader = new XmlNodeReader(node);
                   while (objReader.Read())
                   {
                       if (objReader["ows_Modified"] != null)
                       {
                           //Get Modified Date of the Pictures
                           modifiedDate = objReader["ows_Modified"].ToString();

                           //Calculate (Todays Date-Modified Date)
                           TimeSpan timeSpan = Convert.ToDateTime(todaysDate) - Convert.ToDateTime(modifiedDate);
                           int timeSpanInDays = timeSpan.Days;

                           //Download Photos only if (Todays's Date- Modified date)<=7 days below original                             
                           if (timeSpanInDays <= timeSpanInDaysLogic)
                            //if (timeSpanInDays >= timeSpanInDaysLogic)
                           {
                               if (objReader["ows_EncodedAbsUrl"] != null && objReader["ows_LinkFilename"] != null)
                               {
                                   strURL = objReader["ows_EncodedAbsUrl"].ToString();
                                   strFileName = objReader["ows_LinkFilename"].ToString();

                                   //Download Pictures
                                   helper.DownLoadAttachment(strURL, strFileName, path);
                               } 
                           }               
                       }                    
                   }
               }


               //Upload Photos to User Profile Store Picture library
               helper.UploadProfileImages(siteUrl, path, strListName);

               //Delete Photos from Local Folder                
               helper.DeletePhotosFromLocalFolder(path);

           }//try

           catch (Exception ex)
           {
               Microsoft.Office.Server.Diagnostics.PortalLog.LogString("ProfilePictureSync-Execute:Exception: {0} ::: {1}", ex.Message, ex.StackTrace);
           }//catch
       } // end of execute Method

    }//class
}
