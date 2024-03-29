﻿using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace TimeTracking.ReportGenerator.Bl.Impl.Heplers
{
    public static class QueryStringHelper
    {
        public static string ToQueryString(this NameValueCollection nvc)
        {
            var array = (
                from key in nvc.AllKeys
                from value in nvc.GetValues(key)
                select $"{HttpUtility.UrlEncode(key)}={HttpUtility.UrlEncode(value)}"
            ).ToArray();
            return "?" + string.Join("&", array);
        }
    }
}