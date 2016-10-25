﻿namespace AspnetIdentitySample.Models
{
    using Microsoft.AspNet.Identity.EntityFramework;

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Web;

    /// <summary>
    /// role view model
    /// </summary>
    public class RoleViewModel
    {
        public string Id { get; set; }
        [Required]
        [Display(Name="RoleName")]
        public string Name { get; set; }
    }
}