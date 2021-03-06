﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Hoster;
using System.Net;
using OnlineVideos.Sites;
using System.Text.RegularExpressions;

namespace OnlineVideos.Hoster
{
    public class Veoh : HosterBase
    {
        public override string GetHosterUrl()
        {
            return "Veoh.com";
        }

        public override string GetVideoUrl(string url)
        {
            string videoId = Regex.Match(url, @"permalinkId=(?<value>[^&]+)&").Groups["value"].Value;
            if (String.IsNullOrEmpty(videoId))
                videoId = Regex.Match(url, @"http://www.veoh.com/(tv/)?watch/(?<value>[^$]*)$").Groups["value"].Value;
            CookieContainer cc = new CookieContainer();
            string page = WebCache.Instance.GetWebData("http://www.veoh.com/rest/v2/execute.xml?apiKey=5697781E-1C60-663B-FFD8-9B49D2B56D36&method=veoh.video.findByPermalink&permalink=" + videoId + "&", cookies: cc);

            Match n = Regex.Match(page, @"previewUrl=""(?<url>[^""]+)""");
            if (n.Success)
            {
                return n.Groups["url"].Value;
            }
            return String.Empty;
        }
    }
}
