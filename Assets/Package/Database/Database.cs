using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CyberHub.Foundry.Database.API;
using CyberHub.Foundry;
using Newtonsoft.Json;
using UnityEngine;

namespace CyberHub.Foundry.Database
{
    public class DatabaseSession
    {
        private static DatabaseSession _instance;

        public UserDoc LocalUser => localUser;
        private UserDoc localUser;
        
        public Dictionary<string, UserPropertyDef> UserPropertyDefs => userPropertyDefs;
        private Dictionary<string, UserPropertyDef> userPropertyDefs = new();
        
        private string sessionToken = "";
        public string SessionToken => sessionToken;
        private DateTime sessionTokenExpiresAt;
        
        private HttpClient httpClient;
        
        public FoundryCoreConfig Config => config;
        private FoundryCoreConfig config;
        
        public bool LoggedIn => !string.IsNullOrWhiteSpace(sessionToken);
        
        /// <summary>
        /// Gets the active database session, or creates a new one if none exists.
        /// This will also attempt to refresh a user session if it has expired and we have valid refresh tokens.
        /// Use DatabaseSession.LoggedIn to check if the user is logged after this if you need to.
        /// </summary>
        public static async Task<DatabaseSession> GetActive()
        {
            if (_instance != null)
                return _instance;
            var session = new DatabaseSession();
            _instance = session;
            session.config = FoundryApp.GetConfig<FoundryCoreConfig>();
            session.httpClient = new HttpClient();
            
            await session.TryRefreshSession();  
            
            return session;
        }
        
        /// <summary>
        /// Create an account with the given username, password and email.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <returns>Object containing a true value if the request was successful, and metadata about why it went wrong if if errored</returns>
        public async Task<ApiResult> CreateAccount(string username, string password, string email)
        {
            config = FoundryApp.GetConfig<FoundryCoreConfig>();
            Debug.Assert(!string.IsNullOrWhiteSpace(config.AppKey), "Foundry App Key not set!");
            httpClient.BaseAddress = new Uri(config.GetDatabaseUrl());
            var res =await Post<AuthSignupResponse>("/auth/signup", new AuthSignupRequest
            {
                app_key = config.AppKey,
                username = username,
                password = password,
                email = email
            });
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Log in with the given username and password.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>ApiResult</returns>
        public async Task<ApiResult> Login(string username, string password)
        {
            config = FoundryApp.GetConfig<FoundryCoreConfig>();
            httpClient.BaseAddress = new Uri(config.GetDatabaseUrl());
            Debug.Assert(!string.IsNullOrWhiteSpace(config.AppKey), "Foundry App Key not set!");
            var req = new AuthLoginRequest
            {
                app_key = config.AppKey,
                username = username,
                password = password
            };
            var response = await Post<AuthLoginResponse>("/auth/login", req);
            if (response.status == 200)
            {

                sessionToken = response.data.session_token.token;
                sessionTokenExpiresAt = DateTime.UnixEpoch.AddSeconds(response.data.session_token.expires_at);


                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);

                var res = await UpdateSessionVariables();
                if (!res.IsSuccess)
                    return res;
                
                PlayerPrefs.SetString("foundry_refresh_token", response.data.refresh_token.token);
                PlayerPrefs.SetInt("foundry_refresh_token_expires_at", (int)response.data.refresh_token.expires_at);
                PlayerPrefs.Save();
            }
            
            return new ApiResult
            {
                error_message = response.error_message,
                status = response.status
            };
        }

        /// <summary>
        /// Frictionless login using a token.
        /// </summary>
        /// <param name="token"></param>
        /// <returns>ApiResult</returns>
        public async Task<ApiResult> TokenLogin(string token)
        {
            config = FoundryApp.GetConfig<FoundryCoreConfig>();
            httpClient.BaseAddress = new Uri(config.GetDatabaseUrl());
            Debug.Assert(!string.IsNullOrWhiteSpace(config.AppKey), "Foundry App Key not set!");
            var req = new AuthTokenLoginRequest
            {
                app_key = config.AppKey,
                token = token
            };
            var response = await Post<AuthLoginResponse>("/auth/tokenlogin", req);
            if (response.status == 200)
            {

                sessionToken = response.data.session_token.token;
                sessionTokenExpiresAt = DateTime.UnixEpoch.AddSeconds(response.data.session_token.expires_at);


                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);

                var res = await UpdateSessionVariables();
                if (!res.IsSuccess)
                    return res;

                PlayerPrefs.SetString("foundry_refresh_token", response.data.refresh_token.token);
                PlayerPrefs.SetInt("foundry_refresh_token_expires_at", (int)response.data.refresh_token.expires_at);
                PlayerPrefs.Save();
            }

            return new ApiResult
            {
                error_message = response.error_message,
                status = response.status
            };
        }

        /// <summary>
        /// Ends the current user session on the server.
        /// </summary>
        /// <param name="clearRefreshTokens">If true, the stored refresh tokens will be deleted and auto-logins will be disabled until they are set again</param>
        public async Task Logout(bool clearRefreshTokens = false)
        {
            var res = await Post<ApiResult>("/auth/logout", null);
            if (res.status != 200)
                Debug.LogWarning("Failed to logout: " + res.error_message);
            
            sessionToken = null;
            sessionTokenExpiresAt = DateTime.UnixEpoch;
            httpClient.DefaultRequestHeaders.Authorization = null;
            if (clearRefreshTokens)
            {
                PlayerPrefs.DeleteKey("foundry_refresh_token");
                PlayerPrefs.DeleteKey("foundry_refresh_token_expires_at");
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Sends an email with a password reset code, if a corresponding account exists
        /// </summary>
        /// <param name="email">email for the code to be sent to</param>

        public async Task<ApiResult> RequestResetPassword(string email)
        {
            config = FoundryApp.GetConfig<FoundryCoreConfig>();
            Debug.Assert(!string.IsNullOrWhiteSpace(config.AppKey), "Foundry App Key not set!");
            var dbConfig = FoundryApp.GetConfig<FoundryCoreConfig>();
            httpClient.BaseAddress = new Uri(dbConfig.GetDatabaseUrl());

            var res = await Post<ResetPasswordResponse>("/auth/reset", new ResetPasswordRequest {
                app_key = config.AppKey,
                email = email
            });
            if (res.status != 200)
                Debug.LogWarning("Reset Password: " + res.error_message);
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        } 
        
        /// <summary>
        /// Attempts to reset the password of the account associated with the reset code.q
        /// </summary>
        /// <param name="resetCode">Code obtained from RequestResetPassword()</param>
        /// <param name="newPassword">New password</param>

        public async Task<ApiResult> TryResetPassword(string resetCode, string newPassword)
        {
            var coreConfig = FoundryApp.GetConfig<FoundryCoreConfig>();
            Debug.Assert(!string.IsNullOrWhiteSpace(coreConfig.AppKey), "Foundry App Key not set!");
            var dbConfig = FoundryApp.GetConfig<FoundryCoreConfig>();
            httpClient.BaseAddress = new Uri(dbConfig.GetDatabaseUrl());

            var res = await Post<ResetPasswordResponse>("/auth/reset_code", new ResetCodeRequest {
                reset_code = resetCode,
                new_password = newPassword,
                app_key = coreConfig.AppKey
            });
            if (res.status != 200)
                Debug.LogWarning("Reset Password: " + res.error_message);
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        }

        
        /// <summary>
        /// Update html template for reset emails
        /// </summary>
        /// <param name="reset_email">Html text for email</param>
        public async Task<ApiResult> UpdateResetEmail(string reset_email) {
            var res = await Put<UserRolesDefsResponse>("/user/props/update_reset", new UpdateResetEmailRequest
            {
                reset_email = reset_email
            });
            
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status,
            };
        }

         /// <summary>
        /// Gets the current value of the reset email template
        /// </summary>
        public async Task<ApiResult<string>> GetResetEmail() {
            var res = await Get<GetResetEmailResponse>("/user/props/get_reset");
            
            return new ApiResult<string>
            {
                error_message = res.error_message,
                status = res.status,
                data = res.data?.reset_email
            };
        }

        /// <summary>
        /// Change the current user's password.
        /// </summary>
        /// <param name="newPass">New password</param>
        /// <param name="oldPass">Old password</param>
        public async Task<ApiResult> ChangePassFromOld(string oldPass, string newPass)
        {
            return await Post("/auth/change_pass", new AuthChangePassRequest
            {
                old_password = oldPass,
                new_password = newPass,
            });
        }

        /// <summary>
        /// Attempts to refresh the current user session using the stored refresh token if they exist.
        /// </summary>
        /// <returns>Bool representing if a session was successfully created</returns>
        public async Task<bool> TryRefreshSession()
        {
            config = FoundryApp.GetConfig<FoundryCoreConfig>();
            httpClient.BaseAddress = new Uri(config.GetDatabaseUrl());
            if (!PlayerPrefs.HasKey("foundry_refresh_token") || !PlayerPrefs.HasKey("foundry_refresh_token_expires_at"))
                return false;
            if (PlayerPrefs.GetInt("foundry_refresh_token_expires_at") < (int)DateTime.Now.Subtract(DateTime.UnixEpoch).TotalSeconds)
                return false;

            var req = new AuthRefreshRequest
            {
                refresh_token = PlayerPrefs.GetString("foundry_refresh_token")
            };
            var response = await Post<AuthLoginResponse>("/auth/refresh", req);
            if (response.status == 200)
            {
                sessionToken = response.data.session_token.token;
                sessionTokenExpiresAt = DateTime.UnixEpoch.AddSeconds(response.data.session_token.expires_at);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", sessionToken);
                
                var res = await UpdateSessionVariables();
                if (!res.IsSuccess)
                    return false;
                
                PlayerPrefs.SetString("foundry_refresh_token", response.data.refresh_token.token);
                PlayerPrefs.SetInt("foundry_refresh_token_expires_at", (int)response.data.refresh_token.expires_at);
                PlayerPrefs.Save();
                
                return true;
            }
            if (response.status == 401)
            {
                PlayerPrefs.DeleteKey("foundry_refresh_token");
                PlayerPrefs.DeleteKey("foundry_refresh_token_expires_at");
                PlayerPrefs.Save();
            }

            return false;
        }
        
        /// <summary>
        /// Fetches commonly used values from the server and caches them locally.
        /// </summary>
        /// <returns></returns>
        public async Task<ApiResult> UpdateSessionVariables()
        {
            if (!LoggedIn)
                return new ApiResult
                {
                    status = 401,
                    error_message = "User not logged in"
                };
            var userPropDefsTask = GetUserPropertyDefs();
            var userDataTask = GetUser();
            await Task.WhenAll(userPropDefsTask, userDataTask);
            var userPropDefs = userPropDefsTask.Result;
            var userData = userDataTask.Result;

            if (userPropDefs.status != 200)
            {
                return new ApiResult
                {
                    status = userPropDefs.status,
                    error_message = "Failed to user properties: " + userPropDefs.error_message
                }; 
            }
            
            if (userData.status != 200)
            {
                return new ApiResult
                {
                    status = userData.status,
                    error_message = "Failed to get local user data: " + userData.error_message
                }; 
            }
            
            userPropertyDefs = userPropDefs.data;
            localUser = userData.data;
            
            return new ApiResult
            {
                status = 200
            };    
        }

        /// <summary>
        /// Get the definitions of all user roles.
        /// </summary>
        /// <returns>Dictionary containing results, or an error message</returns>
        public async Task<ApiResult<Dictionary<string, UserRoleDef>>> GetUserRoleDefs()
        {
            var res = await Get<UserRolesDefsResponse>("/user/roles/defs");
            if (res.status != 200)
            {
                return new ApiResult<Dictionary<string, UserRoleDef>>
                {
                    error_message = res.error_message,
                    status = res.status
                };
            }
            
            return new ApiResult<Dictionary<string, UserRoleDef>>
            {
                data = res.data.defs,
                status = res.status
            };
        }
        
        /// <summary>
        /// Define roles that may be added to users.
        /// This will overwrite/update any existing roles if keys match.
        /// </summary>
        /// <param name="defs">Roles to create or update</param>
        /// <param name="delete">Roles to remove</param>
        public async Task<ApiResult> DefineUserRoles(Dictionary<string, UserRoleDef> defs, List<string> delete)
        {
            var res = await Post<UserRolesDefsResponse>("/user/roles/define", new UserRolesDefineRequest
            {
                define = defs,
                delete = delete
            });
            
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Give the current user a role, if they have the permissions to do so.
        /// </summary>
        /// <param name="role">Key of role to give</param>
        public async Task<ApiResult> AddUserRole(string role) => await AddUserRole(localUser._id, role);
        
        /// <summary>
        /// Give a specified user a role, if the current user has the permissions to do so.
        /// </summary>
        /// <param name="userId">User to be given a role</param>
        /// <param name="role">Role to be given</param>
        public async Task<ApiResult> AddUserRole(string userId, string role) => await AddUserRoles(userId, new List<string> {role});
        
        /// <summary>
        /// Give the current user multiple roles, if they have the permissions to do so.
        /// </summary>
        /// <param name="roles">List of roles to give</param>
        public async Task<ApiResult> AddUserRoles(List<string> roles) => await AddUserRoles(localUser._id, roles);
        
        /// <summary>
        /// Give a specified user multiple roles, if the current user has the permissions to do so.
        /// </summary>
        /// <param name="userId">User to be given roles</param>
        /// <param name="roles">Roles to be given</param>
        public async Task<ApiResult> AddUserRoles(string userId, List<string> roles)
        {
            var res = await Post<ApiResult>("/user/roles/add", new UserRolesAddRequest
            {
                user_id = userId,
                roles = roles
            });
            
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Remove a role from the current user if they have the permissions to do so.
        /// </summary>
        /// <param name="role">Role to remove</param>
        public async Task<ApiResult> RemoveUserRole(string role) => await RemoveUserRole(localUser._id, role);
        
        /// <summary>
        /// Remove a role from a specified user if the current user has the permissions to do so.
        /// </summary>
        /// <param name="userId">User to remove role from</param>
        /// <param name="role">Role to remove</param>
        public async Task<ApiResult> RemoveUserRole(string userId, string role) => await RemoveUserRoles(userId, new List<string> {role});
        
        /// <summary>
        /// Remove multiple roles from the current user if they have the permissions to do so.
        /// </summary>
        /// <param name="roles">Roles to remove</param>
        public async Task<ApiResult> RemoveUserRoles(List<string> roles) => await RemoveUserRoles(localUser._id, roles);
        
        public async Task<ApiResult> RemoveUserRoles(string userId, List<string> roles)
        {
            var res = await Post<ApiResult>("/user/roles/remove", new UserRolesRemoveRequest
            {
                user_id = userId,
                roles = roles
            });
            
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        }

        /// <summary>
        /// Search for users by username. This will fail if the current user does not have admin rights.
        /// </summary>
        /// <param name="username">Username or username substring to search for</param>
        /// <param name="roles">Roles to filter by, must match exactly</param>
        /// <param name="startIndex">Index to start at, any results before this number will be skipped</param>
        /// <param name="count">Number of results to return</param>
        public async Task<ApiResult<List<UserSearchResponseItem>>> SearchUsersByUsername(string username, List<string> roles = null, ulong startIndex = 0, ulong count = 20)
        {
            string queryParams = $"?start_index={startIndex}&count={count}";
            if (!string.IsNullOrWhiteSpace(username))
            {
                queryParams += "&username=" + username;
            }
            if (roles != null)
            {
                queryParams += "&roles=";
                foreach (var role in roles)
                {
                    queryParams += role + ",";
                }
                queryParams = queryParams.Remove(queryParams.Length - 1);
            }
            
            var res = await Get<UserSearchResponse>($"/user/search{queryParams}");
            return new ApiResult<List<UserSearchResponseItem>>
            {
                data = res.data.users,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Search for users by email. This will fail if the current user does not have admin rights.
        /// </summary>
        /// <param name="email">Email, must match exactly</param>
        /// <param name="roles">Roles to filter by, must match exactly</param>
        /// <param name="startIndex">Results to skip</param>
        /// <param name="count">Number of results to return</param>
        public async Task<ApiResult<List<UserSearchResponseItem>>> SearchUsersByEmail(string email, List<string> roles = null, ulong startIndex = 0, ulong count = 20)
        {
            string queryParams = $"?&start_index={startIndex}&count={count}";
            if (!string.IsNullOrWhiteSpace(email))
            {
                queryParams += "&email=" + email;
            }
            if (roles != null)
            {
                queryParams += "&roles=";
                foreach (var role in roles)
                {
                    queryParams += role + ",";
                }
                queryParams = queryParams.Remove(queryParams.Length - 1);
            }
            
            var res = await Get<UserSearchResponse>($"/user/search{queryParams}");
            return new ApiResult<List<UserSearchResponseItem>>
            {
                data = res.data.users,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Get the current user's data.
        /// </summary>
        public async Task<ApiResult<UserDoc>> GetUser()
        {
            var res = await Get<UserDoc>("/user");
            return new ApiResult<UserDoc>
            {
                data = res.data,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Get a user's data by their ID.
        /// </summary>
        public async Task<ApiResult<UserDoc>> GetUser(string userId)
        {
            var res = await Get<UserDoc>($"/user?user_id={userId}");
            return new ApiResult<UserDoc>
            {
                data = res.data,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Update the current user's data.
        /// </summary>
        public async Task<ApiResult> UpdateUser(string newUsername, string newEmail = null) => await UpdateUser(null, newUsername, newEmail);
        
        /// <summary>
        /// Update a user's data by their ID.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="newUsername"></param>
        /// <param name="newEmail"></param>
        public async Task<ApiResult> UpdateUser(string userId, string newUsername, string newEmail)
        {
            var res = await Post<UserUpdateResponse>("/user/update", new UserUpdateRequest
            {
                user_id = userId,
                username = newUsername,
                email = newEmail
            });
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Get the definitions of all user properties.
        /// <param name="filter">List of keys to filter by</param>
        /// </summary>
        public async Task<ApiResult<Dictionary<string, UserPropertyDef>>> GetUserPropertyDefs(List<string> filter = null)
        {
            string queryParams = "";
            if (filter != null)
            {
                queryParams = "?";
                foreach (var f in filter)
                {
                    queryParams += "filter=" + f + "&";
                }
                queryParams = queryParams.Remove(queryParams.Length - 1);
            }
            
            var res = await Get<UserPropsDefsResponse>("/user/props/defs" + queryParams);
            if(res.status != 200)
                return new ApiResult<Dictionary<string, UserPropertyDef>>
                {
                    error_message = res.error_message,
                    status = res.status
                };
            return new ApiResult<Dictionary<string, UserPropertyDef>>
            {
                data = res.data.defs,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Define user properties that may be added to users.
        /// </summary>
        /// <param name="defs">Properties to create or update</param>
        /// <param name="delete">Properties to remove</param>
        public async Task<ApiResult> DefineUserProperties(Dictionary<string, UserPropertyDef> defs, List<string> delete)
        {
            var res = await Post<UserPropsDefineResponse>("/user/props/define", new UserPropsDefineRequest
            {
                define = defs,
                delete = delete
            });
            
            return new ApiResult
            {
                error_message = res.error_message,
                status = res.status
            };
        }

        /// <summary>
        /// Get a property of the current user.
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T">Type to serialize result into</typeparam>
        public async Task<ApiResult<T>> GetUserProperty<T>(string key) => await GetUserProperty<T>(localUser._id, key);
        
        /// <summary>
        /// Get a property of a specified user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="key"></param>
        /// <typeparam name="T">Type to serialize result into</typeparam>
        public async Task<ApiResult<T>> GetUserProperty<T>(string userId, string key)
        {
            var res = await GetUserProperty(userId, key);
            if (res.status != 200)
            {
                return new ApiResult<T>
                {
                    error_message = res.error_message,
                    status = res.status
                };
            }

            try
            {
                return new ApiResult<T>
                {
                    data = (T)res.data,
                    status = res.status
                };
            }
            catch (JsonException)
            {
                return new ApiResult<T>
                {
                    error_message = "Failed to parse response",
                    status = 422
                };
            }
            
        }

        /// <summary>
        /// Get a property of the current user.
        /// </summary>
        public async Task<ApiResult<object>> GetUserProperty(string key) => await GetUserProperty(localUser._id, key);

        /// <summary>
        /// Get a property of a specified user.
        /// </summary>
        public async Task<ApiResult<object>> GetUserProperty(string userId, string key)
        {
            Debug.Assert(LoggedIn, "User must be logged in to get user properties");
            var res = await GetUserProperties(userId, new List<string> {key});
            
            if (res.status == 200 && res.data.TryGetValue(key, out object value))
            {
                return new ApiResult<object>
                {
                    data = value,
                    status = res.status
                };
            }
            
            return new ApiResult<object>
            {
                error_message = res.error_message,
                status = res.status
            };
        }

        /// <summary>
        /// Get all properties of the current user.
        /// </summary>
        public async Task<ApiResult<Dictionary<string, object>>> GetUserProperties() =>
            await GetUserProperties(localUser._id, null);
        
        /// <summary>
        /// Get all properties of a specified user.
        /// </summary>
        /// <param name="userId"></param>
        public async Task<ApiResult<Dictionary<string, object>>> GetUserProperties(string userId) =>
            await GetUserProperties(userId, null);
        
        /// <summary>
        /// Get a specific set of properties of the current user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="props">Keys of the props to fetch</param>
        public async Task<ApiResult<Dictionary<string, object>>> GetUserProperties(string userId, List<string> props)
        {
            Debug.Assert(LoggedIn, "User must be logged in to get user properties");
            var req = new UserPropsGetRequest
            {
                user_id = userId,
                props = props,
                permissions = false
            };
            
            var res = await Post<UserPropsGetResponse>("/user/props/get", req);
            return new ApiResult<Dictionary<string, object>>
            {
                data = res.data?.props,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Set a property of the current user.
        /// </summary>
        /// <param name="key">Property to set</param>
        /// <param name="value">Value to set</param>
        /// <returns></returns>
        public async Task<ApiResult<UserPropsSetResponse>> SetUserProperty(string key, object value) => await SetUserProperty(localUser._id, key, value);
        
        
        /// <summary>
        /// Set a property of a specified user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="key">Property to set</param>
        /// <param name="value">Value to set</param>
        public async Task<ApiResult<UserPropsSetResponse>> SetUserProperty(string userId, string key, object value)
        {
            var res = await SetUserProperties(userId, new Dictionary<string, object>
            {
                {key, value}
            });
            
            return new ApiResult<UserPropsSetResponse>
            {
                data = res.data,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        /// <summary>
        /// Set multiple properties of the current user.
        /// </summary>
        /// <param name="props">A list of key-value pairs representing the properties to set</param>
        public async Task<ApiResult<UserPropsSetResponse>> SetUserProperties(Dictionary<string, object> props) => await SetUserProperties(localUser._id, props);
        
        /// <summary>
        /// Set multiple properties of a specified user.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="props">A list of key-value pairs representing the properties to set</param>
        public async Task<ApiResult<UserPropsSetResponse>> SetUserProperties(string userId, Dictionary<string, object> props)
        {
            Debug.Assert(LoggedIn, "User must be logged in to get user properties"); 
            var res = await Post<UserPropsSetResponse>("/user/props/set", new UserPropsSetRequest
            {
                user_id = userId,
                props = props
            });
            
            return new ApiResult<UserPropsSetResponse>
            {
                data = res.data,
                error_message = res.error_message,
                status = res.status
            };
        }
        
        
        /// <summary>
        /// Attempt to get the info for an instance of a sector with the provided name
        /// </summary>
        /// <param name="sectorName"></param>
        /// <returns>Sector instance id, and runtime server IP</returns>
        public async Task<ApiResult<SectorResolveResponse>> ResolveSector(string sectorName)
        {
            return await Get<SectorResolveResponse>($"/sector/resolve?sector_name={sectorName}");
        }
        
        #region Http Methods

        public class ApiResult
        {
            public long status;
            public string error_message;
            
            public bool IsSuccess => status is >= 200 and < 300;
        }

        public class ApiResult<T>
        {
            public T data;
            public long status;
            public string error_message;
            
            public bool IsSuccess => status is >= 200 and < 300;
        }
        
        public async Task<ApiResult> ParseResult(HttpResponseMessage res)
        {
            if (res.IsSuccessStatusCode)
            {
                return new ApiResult
                {
                    status = (long)res.StatusCode
                };
            }
            return await GetResultError(res);
        }
        
        public async Task<ApiResult<T>> ParseResult<T>(HttpResponseMessage res)
        {
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync();
                return new ApiResult<T>
                {
                    data = JsonConvert.DeserializeObject<T>(json),
                    status = (long)res.StatusCode
                };
            }
            return await GetResultError<T>(res);
        }
        
        public async Task<ApiResult> GetResultError(HttpResponseMessage res)
        {
            if (TryParseError(await res.Content.ReadAsStringAsync(), out var error))
            {
                return new ApiResult
                {
                    status = error.code,
                    error_message = error.Error.detail
                };
            }
            return new ApiResult
            {
                status = (long)res.StatusCode,
                error_message = res.ReasonPhrase
            };
        }
        
        public async Task<ApiResult<T>> GetResultError<T>(HttpResponseMessage res)
        {
            if (TryParseError(await res.Content.ReadAsStringAsync(), out var error))
            {
                return new ApiResult<T>
                {
                    status = error.code,
                    error_message = error.Error.detail
                };
            }
            return new ApiResult<T>
            {
                status = (long)res.StatusCode,
                error_message = res.ReasonPhrase
            };
        }
        
        private bool TryParseError(string json, out JSONErrorResponse jsonError)
        {
            try
            {
                jsonError = JsonConvert.DeserializeObject<JSONErrorResponse>(json);
                return jsonError != null; 
            }
            catch (JsonException)
            {
                jsonError = null;
                return false;
            }
        }
        
        private async Task<ApiResult<T>> Get<T>(string url)
            where T : class
        {
            try
            {
                using var res = await httpClient.GetAsync(url);
                return await ParseResult<T>(res);
            }
            catch (HttpRequestException e)
            {
                return new ApiResult<T>
                {
                    status = 500,
                   error_message = e.InnerException?.Message ?? e.Message
                };
            }
        }
        
        private async Task<ApiResult> Post(string url, object data)
        {
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            
            try
            {
                using var res = await httpClient.PostAsync(url, content);
                return await ParseResult(res);
            }
            catch (HttpRequestException e)
            {
                return new ApiResult
                {
                    status = 500,
                   error_message = e.InnerException?.Message ?? e.Message
                };
            }
        }
        
        private async Task<ApiResult<T>> Post<T>(string url, object data)
            where T: class
        {
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            
            try
            {
                using var res = await httpClient.PostAsync(url, content);
                return await ParseResult<T>(res);
            }
            catch (HttpRequestException e)
            {
                return new ApiResult<T>
                {
                    status = 500,
                    error_message = e.InnerException?.Message ?? e.Message
                };
            }
        }
        
        private async Task<ApiResult> Put(string url, object data)
        {
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            
            try
            {
                using var res = await httpClient.PutAsync(url, content);
                return await ParseResult(res);
            }
            catch (HttpRequestException e)
            {
                return new ApiResult
                {
                    status = 500,
                   error_message = e.InnerException?.Message ?? e.Message
                };
            }
        }
        
        private async Task<ApiResult<T>> Put<T>(string url, object data)
            where T: class
        {
            var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            
            try
            {
                using var res = await httpClient.PutAsync(url, content);
                return await ParseResult<T>(res);
            }
            catch (HttpRequestException e)
            {
                return new ApiResult<T>
                {
                    status = 500,
                   error_message = e.InnerException?.Message ?? e.Message
                };
            }
        }
        #endregion

    }
}
