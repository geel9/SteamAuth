using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamAuth.Helpers
{
    internal class QueryStringBuilder : List<KeyValuePair<string, object>>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryStringBuilder" /> class.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        public QueryStringBuilder(IEnumerable<KeyValuePair<string, object>> collection) : base(collection)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueryStringBuilder" /> class.
        /// </summary>
        public QueryStringBuilder()
        {
        }

        /// <summary>
        ///     Returns the values saved in this instance as a Url compatible and encoded string.
        /// </summary>
        /// <returns>
        ///     A <see cref="System.String" /> that represents this instance's saved values.
        /// </returns>
        public override string ToString()
        {
            return string.Join("&",
                this.Select(kvp =>
                    string.Concat(Uri.EscapeDataString(kvp.Key), "=", Uri.EscapeDataString(kvp.Value.ToString()))));
        }

        /// <summary>
        ///     Adds a new value to this instance.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The actual value.</param>
        public void Add(string name, object value)
        {
            Add(new KeyValuePair<string, object>(name, value));
        }

        /// <summary>
        ///     Appends this instance to an URL as query string.
        /// </summary>
        /// <param name="baseUrl">The base URL to append.</param>
        /// <returns>The new URL containing the query string representing the values saved in this instance.</returns>
        public string AppendToUrl(string baseUrl)
        {
            return baseUrl + (baseUrl.Contains("?") ? "&" : "?") + ToString();
        }

        /// <summary>
        ///     Creates a new instance containing the values of this instance and a new instance.
        /// </summary>
        /// <param name="collection">The second collection.</param>
        /// <returns>A new instance containing values of both collections</returns>
        public QueryStringBuilder Concat(IEnumerable<KeyValuePair<string, object>> collection)
        {
            return new QueryStringBuilder(ToArray().Concat(collection));
        }
    }
}