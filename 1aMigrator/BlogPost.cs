//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace _1aMigrator
{
    using System;
    using System.Collections.Generic;
    
    public partial class BlogPost
    {
        public BlogPost()
        {
            this.StatsBlogPostViews = new HashSet<StatsBlogPostView>();
        }
    
        public int BlogPostId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Intro { get; set; }
        public string Body { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public System.DateTime CreatedDate { get; set; }
    
        public virtual UserProfile UserProfile { get; set; }
        public virtual ICollection<StatsBlogPostView> StatsBlogPostViews { get; set; }
    }
}
