using System;
using System.Collections.Generic;

namespace Models.DTO
{
    public class UserDTO
    {
        public UserDTO(string fullName, string id, string email, string userName, DateTime dateCreated,
            List<string> roles, string quantityNotes)
        {
            FullName = fullName;
            Email = email;
            UserName = userName;
            Id = id;
            DateCreated = dateCreated;
            Roles = roles;
            QuantityNotes = quantityNotes;
        }

        public string FullName { get; set; }
        public string Email { get; set; }
        public string Id { get; set; }
        public string UserName { get; set; }
        public DateTime DateCreated { get; set; }
        public string Token { get; set; }
        public List<string> Roles { get; set; }
        public string QuantityNotes { get; set; }
    }
}