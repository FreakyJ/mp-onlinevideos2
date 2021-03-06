﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites;
using OnlineVideos.Sites.Entities;
using System.Windows.Forms;
using OnlineVideos.Helpers;
using OnlineVideos.Sites.Properties;
using System.Drawing;
using System.Threading;
using System.Diagnostics;

namespace OnlineVideos.Sites.BrowserUtilConnectors
{
    public class NetflixConnector : BrowserUtilConnector
    {

        private enum State
        {
            None,
            Login,
            ProfilesGate,
            SelectProfile,
            ReadyToPlay,
            Playing
        }

        private string _username;
        private string _password;
        private string _profile;
        private bool _showLoading = true;
        private bool _rememberLogin = false;
        private bool _enableNetflixOsd = false;
        private bool _disableLogging = false;

        private State _currentState = State.None;
        private bool _isPlayingOrPausing = false;

        private void SendKeyToBrowser(string key)
        {
            Cursor.Position = new System.Drawing.Point(Browser.FindForm().Location.X + 300, Browser.Location.Y + 300);
            Application.DoEvents();
            CursorHelper.DoLeftMouseClick();
            Application.DoEvents();
            System.Windows.Forms.SendKeys.Send(key);
        }

        public override void OnClosing()
        {
            //Process.GetProcessesByName("OnlineVideos.WebAutomation.BrowserHost").First().Kill();
            Process.GetCurrentProcess().Kill();
        }
        public override void OnAction(string actionEnumName)
        {
            if (_currentState == State.Playing && !_isPlayingOrPausing)
            {
                if (actionEnumName == "REMOTE_0" && _enableNetflixOsd)
                {
                    SendKeyToBrowser("^(%(+(d)))");
                }
                if (actionEnumName == "ACTION_MOVE_LEFT")
                {
                    SendKeyToBrowser("{LEFT}");
                }
                if (actionEnumName == "ACTION_MOVE_RIGHT")
                {
                    SendKeyToBrowser("{RIGHT}");
                }
            }
        }

 
        public override Entities.EventResult PerformLogin(string username, string password)
        {

            _rememberLogin = username.Contains("REMEMBERLOGIN");
            username = username.Replace("REMEMBERLOGIN", string.Empty);
            _disableLogging = username.Contains("DISABLELOGGING");
            username = username.Replace("DISABLELOGGING", string.Empty);
            _showLoading = username.Contains("SHOWLOADING");
            username = username.Replace("SHOWLOADING", string.Empty);
            _enableNetflixOsd = username.Contains("ENABLENETFLIXOSD");
            username = username.Replace("ENABLENETFLIXOSD", string.Empty);
            
            if (_showLoading)
                ShowLoading();
            string[] userProfile = username.Split('¥');
            _username = userProfile[0];
            _profile = userProfile[1];
            _password = password;
            _currentState = State.Login;
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = @"https://www.netflix.com/Login";
            return EventResult.Complete();
        }


        public override Entities.EventResult PlayVideo(string videoToPlay)
        {
            if (_showLoading)
                ShowLoading();
            ProcessComplete.Finished = false;
            ProcessComplete.Success = false;
            Url = videoToPlay;
            _currentState = State.Playing;
            return EventResult.Complete();
        }

        public override Entities.EventResult Play()
        {
            return PlayPause();
        }

        public override Entities.EventResult Pause()
        {
            return PlayPause();
        }

        private EventResult PlayPause()
        {
            if (_currentState != State.Playing || _isPlayingOrPausing || Browser.Document == null || Browser.Document.Body == null) return EventResult.Complete();
            _isPlayingOrPausing = true;
            SendKeyToBrowser(" ");
            _isPlayingOrPausing = false;
            return EventResult.Complete();
        }


        public override Entities.EventResult BrowserDocumentComplete()
        {
            string jsCode;
            if (!_disableLogging) MessageHandler.Info("Netflix. Url: {0}, State: {1}", Url, _currentState.ToString());
            switch (_currentState)
            {
                case State.Login:
                    if (Url.Contains("/Login?"))
                    {
                        jsCode = "document.getElementById('email').value = '" + _username + "'; ";
                        jsCode += "document.getElementById('password').value = '" + _password + "'; ";
                        if (_rememberLogin)
                        {
                            jsCode += "document.getElementById('RememberMe').checked = true; ";
                        }
                        else
                        {
                            jsCode += "document.getElementById('RememberMe').checked = false; ";
                        }
                        jsCode += "document.getElementById('login-form-contBtn').click();";
                        InvokeScript(jsCode);
                        _currentState = State.ProfilesGate;
                    }
                    else
                    {
                        Url = "https://www.netflix.com/ProfilesGate";
                        _currentState = State.SelectProfile;
                    }
                    break;
                case State.ProfilesGate:
                    Url = "https://www.netflix.com/ProfilesGate";
                    _currentState = State.SelectProfile;
                    break;
                case State.SelectProfile:
                    if (Url.Contains("/ProfilesGate"))
                    {
                        InvokeScript("document.querySelector('a[data-reactid*=" + _profile + "]').click();");
                        _currentState = State.ReadyToPlay;
                    }
                    break;
                case State.ReadyToPlay:
                    //Sometimes the profiles gate loads again
                    if (Url.Contains("/ProfilesGate"))
                    {
                        InvokeScript("document.querySelector('a[data-reactid*=" + _profile + "]').click();");
                    }
                    if (Url.Contains("/browse") || Url.ToLower().Contains("/kid"))
                    {
                        ProcessComplete.Finished = true;
                        ProcessComplete.Success = true;
                    }
                    break;
                case State.Playing:
                    if (_showLoading)
                        HideLoading();
                    _currentState = State.Playing;
                    ProcessComplete.Finished = true;
                    ProcessComplete.Success = true;
                    break;
            }
            return EventResult.Complete();
        }
    }
}
