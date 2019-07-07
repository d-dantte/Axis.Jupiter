using System;
using System.ComponentModel.DataAnnotations;

namespace Axis.Jupiter.EFCore.ConsoleTest.Entities
{
    public class UserRole: BaseEntity<string>
    {
        private Guid? _userId;
        private Guid? _roleId;

        public virtual User User { get; set; }
        public virtual Role Role { get; set; }
               
        public Guid UserId
        {
            set => _userId = value;
            get
            {
                if (_userId != null)
                    return _userId.Value;

                else if (User != null)
                    return User.Id;

                else return Guid.Empty;
            }
        }

        public Guid RoleId
        {
            set => _roleId = value;
            get
            {
                if (_roleId != null)
                    return _roleId.Value;

                else if (User != null)
                    return User.Id;

                else return Guid.Empty;
            }
        }

        public override string Id
        {
            set => throw new InvalidOperationException("Cannot set the id");
            get
            {
                if (UserId == Guid.Empty || RoleId == Guid.Empty)
                    return null;

                else return $"{UserId}::{RoleId}";
            }
        }
    }
}
