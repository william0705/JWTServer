using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace JWTServer.Entities
{
    public class User: IdentityUser
    {
        public DateTimeOffset BirthDate { get; set; }
        public int SMlNo { get; set; }
    }
}
