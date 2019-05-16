#!/usr/bin/env python
# coding: utf-8

###############################################################################
# Import libraries
###############################################################################
import requests
import json
import pandas as pd
import time
import datetime


###############################################################################
# Configuration
###############################################################################

# Change the baseURL to your own jira server's address and port number
# Note: adding "rest/" at the end may not be necessary in your 
#       environment
baseURL = 'http://ec2-18-235-248-253.compute-1.amazonaws.com:2990/jira/rest/'

loginAPI = 'auth/1/session'

# If you wish to use this API, change the query string to match a user in your organization
userAPI = 'api/2/user?username=alexA'

# If you wish to use this API, change the 'admin' user to a user in your organizaiton
issuePickerAPI = 'api/2/issue/picker?currentJQL=assignee%3Dadmin'

biExportAPI = 'getbusinessintelligenceexport/1.0/message'

# The loginUserName and loginPassWord are the credentials for a user
# who has permission to view the issues that you wish to export.
loginUserName = 'admin'
loginPassWord = 'admin'

# The analysisStartData and analysisEndDate specify an inclusive
# period over time over which you want to extract issues that 
# have been either added or updated.
analysisStartDate = '01-FEB-19'
analysisEndDate = '31-FEB-19'

exportDirectory = './downloads/'


###############################################################################
# Login to Jira
###############################################################################
loginURL = baseURL + loginAPI

loginData = {"username": loginUserName, "password": loginPassWord}

loginResponse = requests.post(loginURL, json=loginData)

if loginResponse.status_code != 200:
    raise Exception('POST ' + loginURL + ' {}'.format(loginResonse.status_code))
else:
    myJSessionID = loginResponse.cookies['JSESSIONID']
print('JSESSIONID: ' + myJSessionID)


###############################################################################
# Request Data
###############################################################################
url = baseURL + userAPI
url = baseURL + issuePickerAPI
url = baseURL + biExportAPI + "?startDate=" + analysisStartDate + "&endDate=" + analysisEndDate

myCookie = dict(JSESSIONID=myJSessionID)

resp = requests.get(url, cookies=myCookie)

if resp.status_code != 200:
    raise Exception('GET ' + url + ' {}'.format(resp.status_code))
else:
    #print("resp: {}".format(resp.json()))
    myRecords = resp.json()["records"]
    #print("myRecords: " + json.dumps(myRecords))


###############################################################################
# Save to File
###############################################################################
df = pd.read_json(json.dumps(myRecords), orient='records')
myTimeStamp = datetime.datetime.fromtimestamp(time.time()).strftime('%Y-%m-%d_%H-%M-%S')
df.to_csv(exportDirectory + "records_" + myTimeStamp + ".csv")

