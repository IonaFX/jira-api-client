///////////////////////////////////////////////////////////////////////////////
// PRE-REQUISITES:
//    Install the Newtonsoft.Json.dll into the same directory as this source code file.
// 
// TO COMPILE:
//    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /reference:Newtonsoft.Json.dll /out:Automate.exe Automate.cs
// 
// TO EXECUTE:
//    Automate.exe
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Net;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ionafx
{
    /// <summary>
    /// The CsharpApiClient program (called Automate in the accompanying 
    /// video) is a simple API client for the Atlassian Jira APIs.
    ///
    /// It performs five functions in the following order:
    ///   1. Log into Jira
    ///   2. Parse and save the JSESSIONID
    ///   3. Call a Jira API to get data (using the JSESSIONID to authenticate)
    ///   4. Convert the returned data from JSON into CSV format
    ///   5. Save the CSV to a file
    ///
    /// Link to accompanying video series: 
    ///   https://www.youtube.com/playlist?list=PLO2rlkUncpGI8HbiNexafnDDspCyxZLdV
    /// </summary>
    class Automate
    {
        /// <summary>
        ///   This is the main method.  It creates the variables used through 
        ///   the calls to the five functions and calls them in the proper  
        ///   order.  It also handles any errors and writes a final SUCCESS 
        ///   or FAILURE message.
        /// </summary>
        private static void Main(string[] args)
        {
            String baseURL = "http://your_server_address:your_server_port/jira/rest/";
            String loginAPI = "auth/1/session";
            String biExportAPI = "getbusinessintelligenceexport/1.0/message";
            String analysisStartDate = "01-DEC-18";
            String analysisEndDate = "31-DEC-18";
            String loginUserName = "admin";
            String loginPassWord = "admin";
            String exportDir = "./downloads/";
            Console.WriteLine("Started");
            JiraRequest req = new JiraRequest(baseURL, loginAPI, biExportAPI, analysisStartDate, analysisEndDate, loginUserName, loginPassWord, exportDir);
            Console.WriteLine("Finished");
        }
    }
    
    
    /// <summary>
    ///   This class receives the necessary information from the caller and
    ///   uses it to request, receive, package, and write the requested data.
    /// </summary>
    public class JiraRequest{
        
        // ////////////////////////////////////////////////////////////////////
        // Properties
        // ////////////////////////////////////////////////////////////////////
        
        private String loginResponse;
        private String jSessionId;
        private String jsonData;
        private String csvData;
        private String writeToFileOutput;
        private String baseURL;
        private String loginAPI;
        private String biExportAPI;
        private String analysisStartDate;
        private String analysisEndDate;
        private String loginUserName;
        private String loginPassWord;
        private bool errorsOccurred;
        private String exportDir;
        
        
        // ////////////////////////////////////////////////////////////////////
        // Constructor
        // ////////////////////////////////////////////////////////////////////
        
        /// <summary>
        ///   This constructor is responsible for initializing all the 
        ///   properties of the class.  It then calls each the five 
        ///   basic processing functions in the proper order.  Of course,
        ///   it also handles any errors and writes a final SUCCESS 
        ///   or FAILURE message.
        /// </summary>
        public JiraRequest(String newBaseURL, String newLoginAPI, String newBiExportAPI, String newAnalysisStartDate, String newAnalysisEndDate, String newLoginUserName, String newLoginPassWord, String newExportDir){
            this.baseURL = newBaseURL;
            this.loginAPI = newLoginAPI;
            this.biExportAPI = newBiExportAPI;
            this.analysisStartDate = newAnalysisStartDate;
            this.analysisEndDate = newAnalysisEndDate;
            this.loginUserName = newLoginUserName;
            this.loginPassWord = newLoginPassWord;
            this.loginResponse = "";
            this.jSessionId = "";
            this.jsonData = "";
            this.csvData = "";
            this.writeToFileOutput = "";
            this.errorsOccurred = false;
            this.exportDir = newExportDir;

            
            if(!errorsOccurred)
            {
                loginToJira();
            }
            if(!errorsOccurred)
            {
                parseJSessionID();
            }
            if(!errorsOccurred)
            {
                getJsonData();
            }
            if(!errorsOccurred)
            {
                formatAsCSV();
            }
            if(!errorsOccurred)
            {
                writeToFile();
            }
        }


        // ////////////////////////////////////////////////////////////////////
        // Methods
        // ////////////////////////////////////////////////////////////////////
        
        /// <summary>
        ///     This method takes the user's credentials and uses them to make a 
        ///     request to log into a given Jira instance.  It writes the response 
        ///     into the loginResopnseProperty.
        /// </summary>
        public void loginToJira(){
            try {
                WebRequest request = WebRequest.Create(this.baseURL + this.loginAPI);
                String postData = "{\"username\":\"" + this.loginUserName + "\",\"password\":\"" + this.loginPassWord + "\"}";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.ContentLength = byteArray.Length;
                
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                
                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                String responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                
                this.loginResponse = responseFromServer;
                Console.WriteLine("\nloginResopnse:");
                Console.WriteLine(this.loginResponse);
                
            } catch (Exception ex) {
                Console.WriteLine("Error in loginToJira: " + ex);
                this.errorsOccurred = true;
            }
        }
        
        
        /// <summary>
        ///     This method takes the response from a Jira login request and 
        ///     parses out the JSESSIONID which will be saved and used to 
        ///     authenticate future requests.
        /// </summary>
        public void parseJSessionID(){
            try {
                var dynObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(this.loginResponse);
                this.jSessionId = dynObj["session"]["value"].Value;
                Console.WriteLine("\njSessionId:");
                Console.WriteLine(this.jSessionId);
            } catch (Exception ex) {
                Console.WriteLine("Error in parseJSessionID: " + ex);
                this.errorsOccurred = true;
            }
        }


        /// <summary>
        ///     This method calls a given Jira API (using a the JSESSIONID 
        ///     property set by the call to the parseJSessionID method)
        ///     to authenticate the request), then writes the resulting 
        ///     response into the jsonData property.
        /// </summary>
        public void getJsonData(){
            try {
                String url = this.baseURL + this.biExportAPI + "?startDate=" + this.analysisStartDate + "&endDate=" + this.analysisEndDate;
                //String url = this.baseURL + "api/2/user?username=alexA";
                //String url = this.baseURL + "api/2/issue/picker" + "?currentJQL=assignee%3Dadmin";
                
                WebRequest request = WebRequest.Create(url);
                request.Headers["Cookie"] = "JSESSIONID=" + this.jSessionId;
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                this.jsonData = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();

                Console.WriteLine("\njsonData:");
                Console.WriteLine(this.jsonData);
            } catch (Exception ex) {
                Console.WriteLine("Error in getJsonData: " + ex);
                this.errorsOccurred = true;
            }
        }
        
        
        /// <summary>
        ///     This method calls a takes the jsonData property which
        ///     contains the Jira server's response in JSON format and 
        ///     converts it into CSV format.
        /// </summary>
        public void formatAsCSV(){
            try {
                var dynObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(this.jsonData);
                String records = dynObj["records"].ToString();
                JArray a = JArray.Parse(records);
                List<String> colNames = new List<String>() {"recordType","project","projectId","projectName","projectLeadUser","issueKey","issueId","issueCreated","issueUpdated","issueCreatorUserName","issueDueDate","issueRemainingEstimate","issueOriginalEstimate","issuePriority","issueReporter","issueStatus","issueTotalTimeSpent","issueVotes","issueWatches","issueResolution","issueResolutionDate","commentId","commentAuthor","commentAuthorKey","commentCreated","commentUpdated","commentUpdateAuthor","worklogId","worklogAuthor","worklogAuthorKey","worklogCreated","worklogStarted","worklogUpdated","worklogTimeSpent","commentText","worklogText"};
                
                String headerRow = "";
                foreach(String colName in colNames)
                {
                    headerRow += "\"" + colName + "\",";
                }
                headerRow = headerRow.TrimEnd(',');
                headerRow += "\n";
                
                String dataRows = "";
                foreach(var record in a)
                {
                    String thisRecord = "";
                    foreach(String colName in colNames)
                    {
                        thisRecord += "\"" + record[colName] + "\",";
                    }
                    thisRecord = thisRecord.TrimEnd(',');
                    thisRecord += "\n";
                    dataRows += thisRecord;
                }
                
                this.csvData = headerRow + dataRows;
                
                Console.WriteLine("\ncsvData:");
                Console.WriteLine(this.csvData);
            } catch (Exception ex){
                Console.WriteLine("Error in formatAsCSV: " + ex);
                this.errorsOccurred = true;
            }
        }
        
        
        /// <summary>
        ///     This method builds a timestamp for the uses it to create
        ///     a filename for the CSV data generated by the previous 
        ///     methods.  Then, it writes that CSV data into a CSV file 
        ///     in a directory specified by the exportDirectory
        ///     property.
        /// </summary>
        public void writeToFile(){
            try {
                String timeStamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                String fileName = this.exportDir + "records_" + timeStamp + ".csv";
                File.WriteAllText(fileName, this.csvData);
                String fullPath = Path.GetFullPath(fileName);
                FileInfo f = new FileInfo(fileName);
                long bytesWritten = f.Length;
                this.writeToFileOutput = "SUCCESS: " + bytesWritten + " bytes written to " + fullPath;
                Console.WriteLine("\nwriteToFileOutput:");
                Console.WriteLine(this.writeToFileOutput);
            } catch (Exception ex) {
                Console.WriteLine("Error in writeToFile: " + ex);
                this.errorsOccurred = true;
            }
        }
        
        
    }
}