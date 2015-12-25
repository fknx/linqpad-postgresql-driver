using System;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Threading;

namespace DynamicLinqPadPostgreSqlDriver.Extensions
{
   internal static class StringExtensions
   {
      private static readonly PluralizationService _pluralizationService = PluralizationService.CreateService(CultureInfo.GetCultureInfo("en"));

      public static string Capitalize(this string s)
      {
         if (s == null)
            throw new ArgumentNullException(nameof(s));

         var cultureInfo = Thread.CurrentThread.CurrentCulture;
         var textInfo = cultureInfo.TextInfo;

         return textInfo.ToTitleCase(s);
      }

      public static string Pluralize(this string s)
      {
         if (s == null)
            throw new ArgumentNullException(nameof(s));

         return !_pluralizationService.IsPlural(s) ? _pluralizationService.Pluralize(s) : s;
      }

      public static string Singularize(this string s)
      {
         if (s == null)
            throw new ArgumentNullException(nameof(s));

         return !_pluralizationService.IsSingular(s) ? _pluralizationService.Singularize(s) : s;
      }
   }
}
