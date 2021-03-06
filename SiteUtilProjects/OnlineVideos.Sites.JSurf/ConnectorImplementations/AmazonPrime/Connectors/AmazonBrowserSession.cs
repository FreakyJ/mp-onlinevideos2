﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors
{
    public class AmazonBrowserSession : BrowserSessionBase
    {
        private Boolean _isLoggedIn;
        private long _lastLogin;
        private string _lastCulture;
        private string _userAgent = "Mozilla/5.0 (Windows NT 6.1; Trident/7.0; rv:11.0) like Gecko";

        public AmazonBrowserSession()
            : base()
        {
            Log.Info("New Browser session");
            _isLoggedIn = false;
            _lastLogin = 0;
            _lastCulture = "";
            userAgent = _userAgent;
        }

        public void Login(string username, string password, bool forceLogin = false)
        {
            string culture = Properties.Resources.Culture.ToString();
            long day = DateTime.Now.Ticks / TimeSpan.TicksPerDay;
            
            if (day > _lastLogin || culture != _lastCulture)
            {
                forceLogin = true;
            }

            if (!_isLoggedIn || forceLogin)
            {
                Log.Info("Login to Amazon " + Properties.Resources.AmazonRootUrl);
                _isLoggedIn = false;
                _lastCulture = culture;
                var loginDoc = Load(Properties.Resources.AmazonLoginUrl);
                var formElements = new FormElementCollection(loginDoc);
                string postUrl = loginDoc.GetElementbyId("ap_signin_form").Attributes["action"].Value;
                formElements["email"] = username;
                formElements["password"] = password;
                formElements["create"] = "0";
                Thread.Sleep(1500);
                string login = GetWebData(postUrl, formElements.AssemblePostPayload(), cc, null, null, false, false, userAgent);
                // TODO add login check
                _isLoggedIn = true;
                _lastLogin = day;
                Log.Info("Login complete");
            }
        }

    }
}
