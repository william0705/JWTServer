using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JWTServer.Entities
{
    public class LibraryDbContext: IdentityDbContext<User,Role, string>
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> dbContext) : base(dbContext)
        {
            //Package manager console
            //Add-Migration AddIdentity
            //update-database
        }
    }
}
