using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using MarketModels;

namespace EMA.Helpers
{
    public static class HtmlHelpers
    {
        /// <summary>
        /// Returns a raw without quatations
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="target"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static MvcHtmlString EnumTypeToJavascript<T>(this HtmlHelper helper)
        {
            var ths = Types.GetUnionCaseNames<T>();
            var thsjs = JsonConvert.SerializeObject(ths);
            var res = helper.Raw(thsjs);
            return MvcHtmlString.Create(thsjs);
        }

    }
}