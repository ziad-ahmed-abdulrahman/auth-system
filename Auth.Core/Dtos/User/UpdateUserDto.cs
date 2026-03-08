using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Dtos.User
{
    public class UpdateUserDto : UpdateMeDto
    {
        public bool? IsActive { get; set; }
    }
}
