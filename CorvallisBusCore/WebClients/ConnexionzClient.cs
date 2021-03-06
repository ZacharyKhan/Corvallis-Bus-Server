﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;
using System.Xml.Linq;
using API.Models.Connexionz;
using API.Models;
using System.Threading.Tasks;
using System.Net.Http;

namespace API.WebClients
{
    /// <summary>
    /// Exposes methods for getting transit data from Connexionz.
    /// </summary>
    public static class ConnexionzClient
    {
        private const string BASE_URL = "http://www.corvallistransit.com/rtt/public/utility/file.aspx?contenttype=SQLXML";

        /// <summary>
        /// Gets and deserializes XML from the specified Connexionz/CTS endpoints.
        /// </summary>
        private static T GetEntity<T>(string url) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));

            using (var client = new WebClient())
            {
                string s = client.DownloadString(url);

                var reader = new StringReader(s);

                return serializer.Deserialize(reader) as T;
            }
        }
        /// <summary>
        /// Gets and deserializes XML from the specified Connexionz/CTS endpoints.
        /// </summary>
        private static async Task<T> GetEntityAsync<T>(string url) where T : class
        {
            var serializer = new XmlSerializer(typeof(T));

            string s = string.Empty;

            using (var client = new WebClient())
            {
                s = await client.DownloadStringTaskAsync(new Uri(url));
            }

            var reader = new StringReader(s);

            return serializer.Deserialize(reader) as T;
        }

        /// <summary>
        /// Downloads static Connexionz Platforms (Stops) info.
        /// </summary>
        public static List<ConnexionzPlatform> LoadPlatforms()
        {
            using (var client = new WebClient())
            {
                string s = client.DownloadString(BASE_URL + "&Name=Platform.rxml");

                XDocument document = XDocument.Parse(s);

                return document.Element("Platforms")
                    .Elements("Platform")
                    .Select(e => new ConnexionzPlatform(e))
                    .ToList();
            }
        }

        /// <summary>
        /// Downloads static Connexionz Route (e.g. Route 1, Route 8, etc) info.
        /// </summary>
        public static List<ConnexionzRoute> LoadRoutes()
        {
            RoutePattern routePattern = GetEntity<RoutePattern>(BASE_URL + "&Name=RoutePattern.rxml");

            var routePatternProject = routePattern.Items.Skip(1).FirstOrDefault() as RoutePatternProject;

            return routePatternProject.Route.Select(r => new ConnexionzRoute(r)).ToList();
        }

        /// <summary>
        /// Gets the Connexionz-estimated time of arrival for a given stop.
        /// </summary>
        public static async Task<ConnexionzPlatformET> GetPlatformEta(int platformTag)
        {
            RoutePosition position = await GetEntityAsync<RoutePosition>(BASE_URL + "&Name=RoutePositionET.xml&PlatformTag=" + platformTag.ToString());

            var positionPlatform = position.Items.FirstOrDefault(p => p is RoutePositionPlatform) as RoutePositionPlatform;

            return positionPlatform != null ?
                new ConnexionzPlatformET(positionPlatform) :
                null;
        }
    }
}