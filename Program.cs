using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using HtmlAgilityPack;
using PuppeteerSharp;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;

namespace BeerFinder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<string> beers = new();
            string content = await GetHtml();
            bool isTremens = false;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(content);

            //get the beer items from the menu of the html
            var beerList = doc.DocumentNode.SelectNodes("//span[@class='beer-name'] | //div[@class='heading']");

            foreach (var beer in beerList)
            {
                if (beer.InnerText != "WHITE or ROSE" && beer.InnerText != "RED")
                {
                    Console.WriteLine(beer.GetAttributeValue("class", "default"));
                    Console.WriteLine(beer.InnerText);

                    if (beer.GetAttributeValue("class", "default") == "beer-name")
                    {
                        beers.Add(beer.InnerText);
                    }
                    else if (beer.GetAttributeValue("class", "default") == "heading")
                    {
                        beers.Add("\n" + beer.InnerText);
                    }
                }
            }

            Console.WriteLine("\n-----------------------------\n");

            //Cut and replace certain unnecessary parts of the inner html strings 
            for (int i = 0; i < beers.Count; i++)
            {
                if (beers[i] != "")
                {
                    if (beers[i].Contains("&amp;") || beers[i].Contains("\t") || beers[i].Contains("\n"))
                    {
                        string patternAmp = "&amp;";
                        string patternNewLine = "\n";
                        string patternTab = "\t";
                        string patternQuestionMark = @"[^\u0000-\u007F]";
                        string patternWeirdOne = "&nbsp;";

                        Regex rgxAmp = new(patternAmp);
                        beers[i] = rgxAmp.Replace(beers[i], "&");

                        Regex rgxNewLine = new(patternNewLine);
                        beers[i] = rgxNewLine.Replace(beers[i], "");

                        Regex rgxTab = new(patternTab);
                        beers[i] = rgxTab.Replace(beers[i], "");

                        Regex rgxQuestionMark = new(patternQuestionMark);
                        beers[i] = rgxQuestionMark.Replace(beers[i], " ");

                        Regex rgxWeirdOne = new(patternWeirdOne);
                        beers[i] = rgxWeirdOne.Replace(beers[i], " ");
                    }
                }
            }

            string textMessage = null;

            //Add each item in the beers list to the text message string
            //and check the existing .txt file for repeated list items
            foreach (var beer in beers)
            {
                bool isUpper = true;

                foreach (var letter in beer)
                {
                    if (!char.IsUpper(letter) && char.IsLetter(letter))
                    {
                        isUpper = false;
                        break;
                    }
                }

                if (isUpper)
                {
                    textMessage += "\n\n" + beer;
                }
                else
                {
                    textMessage += "\n" + beer;
                    isUpper = true;
                }

                if (beer.Contains("Tremens"))
                {
                    isTremens = true;
                }
            }

            //Sets the beginning message of the text message to communicate 
            //whether tremens is on the menu or not
            if (isTremens)
            {
                textMessage = "They have the Tremens. Get on it.\n" + textMessage;
            }
            else
            {
                textMessage = "They aint got it. Maybe tomorrow.\n" + textMessage;
            }

            Console.WriteLine(textMessage);

            SendTextMessage(textMessage);

            Environment.Exit(0);
        }

        static async Task<string> GetHtml()
        {
            Console.WriteLine("Running...");
            const string url = "https://www.barrelsbottles.com/drinks";

            await new BrowserFetcher().DownloadAsync();
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                DefaultViewport = null
            });

            var page = await browser.NewPageAsync();
            await page.GoToAsync(url);

            string content = await page.GetContentAsync();

            //Console.WriteLine(content);

            return content;
        }

        private static void SendTextMessage(string textMessage)
        {
            // Find your Account SID and Auth Token at twilio.com/console
            // and set the environment variables. See http://twil.io/secure
            var accountSid = "********************************";
            string authToken = "********************************";

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
                body: textMessage,
                from: new PhoneNumber("+15163364142"),
                to: new PhoneNumber("+18122677908")
            );

            Console.WriteLine(message.Sid);
        }
    }
}
