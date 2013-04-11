using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using Everest;
using Everest.Headers;
using Everest.SystemNetHttp;

namespace MercuryLoadTest
{
    class Program
    {
        private const string UriName = "uri";
        private const string MethodName = "method";
        private const string CookieName = "cookie";
        private const string PayloadName = "payload";
        private const string ContentTypeName = "contentType";
        private const string RunCountName = "runCount";

        static void Main(string[] args)
        {
            // Ignore ssl errors
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true;

            var settings = ReadSettings(args);

            try
            {
                RunTest(settings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void RunTest(Dictionary<string, string> settings)
        {
            var uri = settings[UriName];
            var verb = settings[MethodName];
            var method = GetMethod(verb);
            var cookieValue = settings[CookieName];
            var payload = settings[PayloadName];
            var contentType = settings[ContentTypeName];
            var runCount = int.Parse(settings[RunCountName]);

            Console.WriteLine(UriName + ": " + uri);
            Console.WriteLine(MethodName + ": " + method);
            Console.WriteLine(CookieName + ": " + cookieValue);
            Console.WriteLine(PayloadName + ": " + payload);
            Console.WriteLine(ContentTypeName + ": " + contentType);
            Console.WriteLine(RunCountName + ": " + runCount);

            var cookie = new RequestHeader("Cookie", cookieValue);
            var content = new Everest.Content.StringBodyContent(payload, contentType);
            var client = new RestClient(cookie, new RequestHeader("X-Requested-With", "XMLHttpRequest"));

            var firstResponse = client.Send(method, uri, content);
            if (contentType == "application/json")
            {
                if (!firstResponse.Body.StartsWith("{"))
                {
                    throw new Exception("Epected JSON but got: " + firstResponse.Body);
                }
            }
            firstResponse.Dispose();

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < runCount; i++)
            {
                client.Send(method, uri, content).Dispose();
                Console.Write(".");
            }
            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine(uri);
            Console.WriteLine("Average time: " + new TimeSpan(stopwatch.Elapsed.Ticks/runCount));
        }

        private static HttpMethod GetMethod(string verb)
        {
            verb = verb.ToLowerInvariant();

            switch (verb)
            {
                case "get": return HttpMethod.Get;
                case "post": return HttpMethod.Post;
                default:
                    throw new Exception("Unknown verb: " + verb);
            }
        }

        private static Dictionary<string, string> ReadSettings(string[] args)
        {
            var keys = ConfigurationManager.AppSettings.AllKeys;

            var settings = keys.ToDictionary(x => x, x => ConfigurationManager.AppSettings[x]);

            for (var i = 0; i < args.Length; i += 2)
            {
                var name = args[i];

                if (!name.StartsWith("-"))
                {
                    throw new ArgumentException("format is -<name> <value>");
                }

                name = name.Substring(1);

                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException("missing value");
                }

                var value = args[i + 1];

                settings[name] = value;
            }
            return settings;
        }
    }
}
