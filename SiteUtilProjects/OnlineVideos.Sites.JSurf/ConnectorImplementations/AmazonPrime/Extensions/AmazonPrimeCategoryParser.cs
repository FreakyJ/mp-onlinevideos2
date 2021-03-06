﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OnlineVideos.Sites.JSurf.Extensions;
using System.Threading;
using OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Connectors;
using System.Text.RegularExpressions;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites.JSurf.ConnectorImplementations.AmazonPrime.Extensions
{
    public static class AmazonPrimeCategoryParser
    {

        /// <summary>
        /// Get the categories from the supplied Url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static List<Category> LoadAmazonPrimeCategoriesFromUrl(this string url, Category parent, AmazonBrowserSession session)
        {
            HtmlDocument doc;
            var result = new List<Category>();
            List<HtmlNode> nodes = null;
            var variedUserAgent = string.Empty;
            var tmpWeb = session; //HtmlWeb { UseCookies = true };
            // Attempt the URL up to 15 times as amazon wants us to use the api!
            for (int i = 0; i <= 15; i++)
            { 
                doc = tmpWeb.Load(url.Replace("{RANDOMNUMBER}", new Random().Next(1000000, 2000000).ToString()));
                nodes = doc.DocumentNode.GetNodesByClass("collections-element");

                if (nodes == null)
                    Thread.Sleep(400);
                else 
                    break;
            }

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var tmpCateg = new Category();// { HasSubCategories = false, SubCategoriesDiscovered = true };
                    var descNode = node.NavigatePath(new[] { 0, 1, 0, 0 });
                    if (descNode != null)
                    {
                        tmpCateg.Other = "V~" + descNode.Attributes["href"].Value.Replace("&amp;", "&");
                        tmpCateg.Name = descNode.InnerText.Replace("&amp;", "&");
                    }
                    else
                    {
                        // In Editor's picks some categories have no description, we use the image name
                        tmpCateg.Other = "V~" + node.NavigatePath(new[] { 0, 0 }).Attributes["href"].Value.Replace("&amp;", "&");
                        tmpCateg.Name = node.NavigatePath(new[] { 0, 0, 0 }).Attributes["src"].Value;
                        MatchCollection matchName = Regex.Matches(tmpCateg.Name, "^.*/([a-zA-Z_-]+)([^/]*)$", RegexOptions.None);
                        if (matchName.Count > 0)
                        {
                            tmpCateg.Name = matchName[0].Groups[1].Value.Replace("_", " ").ToUpper();
                        }
                        else
                        {
                            tmpCateg.Name = "(No Description)";
                        }
                    }
                    // Ugly hack, if not included some pages have a different html layout
                    if (!tmpCateg.Other.ToString().Contains("sort="))
                    {
                        tmpCateg.Other = tmpCateg.Other.ToString() + "&sort=popularity-rank";
                    }
                    tmpCateg.Name = StringUtils.PlainTextFromHtml(tmpCateg.Name.Replace("\n", String.Empty).Trim());
                    tmpCateg.Thumb = node.NavigatePath(new[] { 0, 0, 0 }).Attributes["src"].Value;
                    tmpCateg.ParentCategory = parent;
                    tmpCateg.HasSubCategories = true;
                    tmpCateg.SubCategoriesDiscovered = false;
                    result.Add(tmpCateg);
               }
            }
            return result;
        }

        /// <summary>
        /// Load the prime videos as categories because we can't get the description until we drill in to the video itself
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static List<Category> LoadAmazonPrimeVideosAsCategoriesFromUrl(this string url, Category parent, AmazonBrowserSession session)
        {
            var results = new List<Category>();
            var nextPage = string.Empty;
            HtmlDocument doc = null;
            var tmpWeb = session;

            List<HtmlNode> listItems = null;

            // Attempt the URL up to 10 times as amazon wants us to use the api!
            for (int i = 0; i <= 10; i++)
            {
                doc = tmpWeb.Load(url);
                listItems = doc.DocumentNode.GetNodesByClass("result-item");

                if (listItems == null)
                {
                    Thread.Sleep(200);
                }
                else
                    break;
            }

            if (listItems != null)
            {
                    listItems = listItems.Where(x => x.OriginalName.ToLower() == "li" && x.Id.StartsWith("result_")).ToList();

                    // These are the movies - parse them into categories
                    foreach (var item in listItems)
                    {
                        var tmpCateg = new Category();
                        tmpCateg.ParentCategory = parent;
                        tmpCateg.HasSubCategories = false;
                        var link = item.GetNodeByClass("s-access-detail-page");                        
                        tmpCateg.Name = link.GetAttribute("title");
                        tmpCateg.Name = StringUtils.PlainTextFromHtml(tmpCateg.Name.Replace("\n", String.Empty).Trim());
                        tmpCateg.Other = link.GetAttribute("href"); ;
                        tmpCateg.Thumb = item.GetNodeByClass("s-access-image").GetAttribute("src");
                        var released = link.ParentNode.NavigatePath(new int[] { 3 }).GetInnerText();
                        var score = item.GetNodeByClass("a-icon-star") == null ? String.Empty : item.GetNodeByClass("a-icon-star").FirstChild.GetInnerText();
                        tmpCateg.Description = StringUtils.PlainTextFromHtml("Released: " + released + "\r\nReview Score: " + score);
                        results.Add(tmpCateg);
                    }

                    var nextPageCtrl = doc.GetElementById("pagnNextLink");

                    if (nextPageCtrl != null)
                    {
                        nextPage = Properties.Resources.AmazonRootUrl + nextPageCtrl.Attributes["href"].Value.Replace("&amp;", "&");
                        if (!string.IsNullOrEmpty(nextPage))
                            results.Add(new NextPageCategory() { ParentCategory = parent, Url = nextPage, SubCategories = new List<Category>() });
                    }
            }

            return results;
        }

        /// <summary>
        /// Load the prime videos as categories because we can't get the description until we drill in to the video itself
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static List<Category> LoadAmazonPrimeWatchlistAsCategoriesFromUrl(this string url, Category parent, AmazonBrowserSession session)
        {
            var results = new List<Category>();
            var nextPage = string.Empty;
            HtmlDocument doc = null;
            var tmpWeb = session;

            List<HtmlNode> listItems = null;

            // Attempt the URL up to 10 times as amazon wants us to use the api!
            for (int i = 0; i <= 10; i++)
            {
                doc = tmpWeb.Load(url);
                listItems = doc.DocumentNode.GetNodesByClass("packshot-wrapper");//ilo2");

                if (listItems == null)
                {
                   Thread.Sleep(200);
                }
                else
                    break;
            }



            if (listItems != null)
            {
                    foreach (var item in listItems)
                    {
                        var tmpCateg = new Category();
                        tmpCateg.ParentCategory = parent;
                        tmpCateg.HasSubCategories = false;
                        tmpCateg.Name = item.NavigatePath(new[] { 0, 0 }).Attributes["alt"].Value; //item.GetNodeByClass("watchlist-row-link").Attributes["title"].Value;
                        tmpCateg.Name = StringUtils.PlainTextFromHtml(tmpCateg.Name.Replace("\n", String.Empty).Trim());
                        tmpCateg.Other = item.NavigatePath(new[] { 0 }).Attributes["href"].Value.Replace("&amp;", "&"); ;
                        tmpCateg.Thumb = item.NavigatePath(new[] { 0, 0 }).Attributes["src"].Value;
                        //var released = (item.GetNodeByClass("reg subt") == null ? string.Empty : item.GetNodeByClass("reg subt").FirstChild.GetInnerText());
                        //var score = (item.GetNodeByClass("asinReviewsSummaryNoPopover") == null ? string.Empty : (item.GetNodeByClass("asinReviewsSummaryNoPopover").FindFirstChildElement() == null ? string.Empty : item.GetNodeByClass("asinReviewsSummaryNoPopover").FindFirstChildElement().Attributes["alt"].Value));
                        tmpCateg.Description = tmpCateg.Name; //"Released: " + released + "\r\nReview Score: " + score;
                        results.Add(tmpCateg);
                    }
 
                    var nextPageCtrl = doc.GetElementById("pagnNextLink");

                    if (nextPageCtrl != null)
                    {
                        nextPage = Properties.Resources.AmazonRootUrl + nextPageCtrl.Attributes["href"].Value.Replace("&amp;", "&");
                        if (!string.IsNullOrEmpty(nextPage))
                            results.Add(new NextPageCategory() { ParentCategory = parent, Url = nextPage, SubCategories = new List<Category>() });
                    }
            }

            return results;
        }


        public static List<SearchResultItem> LoadAmazonPrimeSearchAsCategoriesFromUrl(this string url, string query, AmazonBrowserSession session)
        {
            url = url.Replace("{QUERY}", Uri.EscapeDataString(query));
            return url.LoadAmazonPrimeVideosAsCategoriesFromUrl(null, session).Cast<SearchResultItem>().ToList();
        }

    }
}
