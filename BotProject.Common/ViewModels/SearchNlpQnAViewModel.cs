using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotProject.Common.ViewModels
{
    public class SearchNlpQnAViewModel
    {
        public string _id { set; get; }
        public string question { set; get; }
        public string answer { set; get; }
        public string html { set; get; }
        public string field { set; get; }
        public int id { set; get; }
    }
    public class SearchSymptomViewModel
    {
        public string id { set; get; }
        public string name { set; get; }
        public string treatment { set; get; }
        public string description { set; get; }
        public string cause { set; get; }
        public string advice { set; get; }
    }
    public class SearchLawArticleViewModel
    {
        public string _id { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string legal_id { get; set; }
        public string id { get; set; }
        public string html { get; set; }
    }


    public class ArticleViewModel
    {
        public string ArticleID { get; set; }
        public string ArtTitle { get; set; }
        public string Contents { get; set; }
        public string ItemsID { get; set; }
        public string Idx { get; set; }
        public string IsDelete { get; set; }
        public string CreateDate { get; set; }
        public string IsEffect { get; set; }
        public string AgencyID { get; set; }
        public string LegalID { get; set; }
        public string LegalCode { get; set; }
        public string DocAttach { get; set; }
        public string DocName { get; set; }
        public string TotalLegalGuide { get; set; }
    }
    public class LegalVm
    {
        public string LegalID { get; set; }
        public string Title { get; set; }
        public string LegalCode { get; set; }
        public string IssuedDate { get; set; }
        public string EffectiveDate { get; set; }
        public string Descriptions { get; set; }
        public string CreateDate { get; set; }
        public string DocName { get; set; }
        public string AreaTitle { get; set; }
        public string StatusName { get; set; }
        public string Total { get; set; }
        public int RowIndex { get; set; }
        public DateTime IssuedDate2 { get; set; }
        public string CombineTextSearch { get; set; }
        public string TitleSearch { get; set; }
        public string StringSearchMap { get; set; }
        public List<ArticleViewModel> ArticleViewModels { set; get; }
    }
}
