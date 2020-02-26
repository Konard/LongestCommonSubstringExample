using System.Collections.Generic;
using Platform.Data.Doublets;

namespace LongestCommonSubstringExample
{
    public static class LinksExtensions
    {
        public static string Format<TLink>(this ILinks<TLink> links, IList<TLink> link)
        {
            var constants = links.Constants;
            return $"({link[constants.IndexPart]}: {link[constants.SourcePart]}->{link[constants.TargetPart]})";
        }

        public static string Format<TLink>(this ILinks<TLink> links, TLink link) => $"({link}: {links.GetSource(link)}->{links.GetTarget(link)})";
    }
}
