using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace CyberHub.Foundry.Database.API
{
    [Serializable]
    public class JSONErrorResponse
    {
        /// Http status code
        public int code;
        /// Http status code text
        public string message;
        /// Detailed error information
        public ErrorItem[] errors;

        /// Returns the first error in the errors array.
        public ErrorItem Error => errors.Length > 0 ? errors[0] : null;
        
        [Serializable]
        public class ErrorItem
        {
            public struct Links {
                /// Link to more information about the error, may be null
                public string about;
                /// Link to the documentation about the error, may be null
                public string type;
            }
            
            /// Links to more information about the error, if available
            private Links[] links;
            /// Default http status code text
            public string status;
            /// Detail message
            public string detail;
        }
    }
    
    
    
    #region /auth
    
    #region /auth/signup
    
    [Serializable]
    public class AuthSignupRequest
    {
        public string app_key;
        public string username;
        public string password;
        public string email;
    }
    
    [Serializable]
    public class AuthSignupResponse
    {
        public string messgae;
    }

    #endregion
    
    #region /auth/login
    
    [Serializable]
    public class AuthLoginRequest
    {
        public string app_key;
        public string username;
        public string password;
    }
    
    [Serializable]
    public class TokenResponse
    {
        public string token;
        [FormerlySerializedAs("expires")] public uint expires_at;
    }
    
    [Serializable]
    public class AuthLoginResponse
    {
        public TokenResponse session_token;
        public TokenResponse refresh_token;
    }
    
    #endregion

    #region /auth/reset

    [Serializable]
    public class ResetPasswordRequest {
        public string app_key;
        public string email;
    }

    public class ResetPasswordResponse{
        public string message;
    }
    #endregion /auth/reset

    #region /auth/reset_code

    public class ResetCodeRequest {
        public string reset_code;
        public string new_password;
        public string app_key;

    }

    // Reset code response uses ResetPasswordResponse

    #endregion /auth/reset_code 
    
    #region /auth/change_pass 
    
    [Serializable]
    public class AuthChangePassRequest
    {
        public string old_password;
        public string new_password;
    }
    
    #endregion /auth/change_pass
    
    #region /auth/refresh
    
    [Serializable]
    public class AuthRefreshRequest
    {
        public string refresh_token;
    }
    
    // Refresh uses AuthLoginResponse as response class
    
    #endregion
    
    #region /auth/logout
    
    /// This is a post request, with no parameters, it will log the user out.
    
    #endregion 
    
    #endregion /auth
    
    #region /user
    
    // /user is a get request, with one optional query parameter: user_id, if left blank, it will return the user object of the currently logged in user.

    [Serializable]
    public class UserDoc
    {
        public string _id;
        public string username;
        public string email;
        public List<string> roles;
        public string created_at;
    }
    
    #region /user/search
    
    // /user/search is a get request, with the following optional query parameters:
    // username, email, roles, start_index, count
    
    [Serializable]
    public class UserSearchResponse
    {
        public List<UserSearchResponseItem> users;
    }
    
    [Serializable]
    public class UserSearchResponseItem
    {
        public string _id;
        public string username;
    }
    
    #endregion /user/search
    
    #region /user/update
    
    [Serializable]
    public class UserUpdateRequest
    {
        /// Optional, if left null, it will update the currently logged in user.
        public string user_id;
        public string username;
        public string email;
        
        public bool ShouldSerializeuser_id() { return !string.IsNullOrWhiteSpace(user_id); }
        public bool ShouldSerializeusername() { return !string.IsNullOrWhiteSpace(username); }
        public bool ShouldSerializeemail() { return !string.IsNullOrWhiteSpace(email); }
    }
    
    [Serializable]
    public class UserUpdateResponse
    {
        public string message;
    }
    
    #endregion /user/update
    
    #region /user/roles
    
    #region /user/roles/add
    
    [Serializable]
    struct UserRolesAddRequest
    {
        public string user_id;
        public List<string> roles;
        
        public bool ShouldSerializeuser_id() { return !string.IsNullOrWhiteSpace(user_id); }
    }
    
    [Serializable]
    struct UserRolesAddResponse
    {
        public string message;
    }
    
    #endregion /user/roles/add
    
    #region /user/roles/remove 
    
    [Serializable]
    struct UserRolesRemoveRequest
    {
        public string user_id;
        public List<string> roles;
        
        public bool ShouldSerializeuser_id() { return !string.IsNullOrWhiteSpace(user_id); }
    }
    
    [Serializable]
    struct UserRolesRemoveResponse
    {
        public string message;
    }
    
    #endregion /user/roles/remove
    
    #region /user/roles/define
    
    [Serializable]
    public class UserRoleDef
    {
        public List<string> permissions;
        
        public UserRoleDef Clone()
        {
            return new UserRoleDef
            {
                permissions = new List<string>(permissions)
            };
        }
    }
    
    [Serializable]
    public class UserRolesDefineRequest
    {
        public Dictionary<string, UserRoleDef> define;
        public List<string> delete;
        
        public bool ShouldSerializedefine() { return define != null && define.Count != 0; }
        public bool ShouldSerializedelete() { return delete != null; }
    }
    
    #endregion /user/roles/define
    
    #region /user/roles/defs
    
    // This api call is a get request, with no parameters, it will return all the roles defined in the database this user can see.
    
    [Serializable]
    public class UserRolesDefsResponse
    {
        public Dictionary<string, UserRoleDef> defs;
    }
    
    
    #endregion /user/roles/defs
    
    #endregion /user/roles
    
    #region /user/props
    
    #region /user/props/get
    
    [Serializable]
    public class UserPropsGetRequest
    {
        public string user_id;
        public List<string> props;
        public bool permissions = false;
        
        public bool ShouldSerializeuser_id() { return !string.IsNullOrWhiteSpace(user_id); }
        public bool ShouldSerializeprops() { return props != null && props.Count != 0; }
        public bool ShouldSerializepermissions() { return permissions; }
    }
    
    [Serializable]
    public class UserPropsGetResponse
    {
        public Dictionary<string, object> props;
    }
    
    #endregion /user/props/get
    
    #region /user/props/set

    [Serializable]
    public class UserPropertyDef
    {
        public List<string> get_roles;
        public List<string> set_roles;

        public UserPropertyDef Clone()
        {
            return new UserPropertyDef
            {
                get_roles = new List<string>(get_roles),
                set_roles = new List<string>(set_roles)
            };
        }
    }
    
    [Serializable]
    public class UserPropsSetRequest
    {
        public string user_id;
        /// null JValues will be treated as deletions
        public Dictionary<string, object> props;
        
        public bool ShouldSerializeuser_id() { return !string.IsNullOrWhiteSpace(user_id); }
    }
    
    [Serializable]
    public class UserPropsSetResponse
    {
        public string message;
    }
    
    #endregion /user/props/set
    
    #region /user/props/define
    
    [Serializable]
    public class UserPropsDefineRequest
    {
        public Dictionary<string, UserPropertyDef> define;
        public List<string> delete;
        
        public bool ShouldSerializedefine() { return define != null && define.Count != 0; }
        public bool ShouldSerializedelete() { return delete != null; }
    }
    
    [Serializable]
    public class UserPropsDefineResponse
    {
        public string message;
    }
    
    #endregion /user/props/define
    
    #region /user/props/defs
    
    [Serializable]
    public class UserPropsDefsRequest
    {
        public List<string> filter;
        
        public bool ShouldSerializefilter() { return filter != null && filter.Count != 0; }
    }
    
    [Serializable]
    public class UserPropsDefsResponse
    {
        public Dictionary<string, UserPropertyDef> defs;
    }
    
    #endregion /user/props/defs
    
    #endregion /user/props
    
    #endregion /user
    
    
}
