using System.Collections.Generic;
using System.Linq;
using CyberHub.Foundry.Database;
using CyberHub.Foundry.Database.API;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

public class UserEditorElement : VisualElement
{
    public UserEditorElement(UserDoc user, List<UserRoleDefData> userRoleOptions, List<UserPropDefData> userPropDefs, DatabaseSession session)
    {
        style.marginTop = 10;
        style.marginBottom = 10;
        
        var detailsHeader = new Label("Details")
        {
            style =
            {
                fontSize = 16f,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 10,
            }
        };
        Add(detailsHeader);
        
        var usernameField = new TextField("Username")
        {
            value = user.username,
            style =
            {
                alignSelf = Align.FlexStart,
                minWidth = 400,
            }
        };
        Add(usernameField);
        
        var emailField = new TextField("Email")
        {
            value = user.email,
            style =
            {
                alignSelf = Align.FlexStart,
                minWidth = 400,
            }
        };
        Add(emailField);
        
        var updateButton = new Button(async () =>
        {
            var updateRes = await session.UpdateUser(user._id, usernameField.value, emailField.value);
            if (updateRes.status != 200)
            {
                Debug.LogError($"Failed to update user: {updateRes.error_message}");
                return;
            }
            Debug.Log("User updated successfully");
        });
        updateButton.Add(new Label("Update"));
        Add(updateButton);

        if (user == session.LocalUser)
        {
            
            var changePasswordHeader = new Label("Change Password")
            {
                style =
                {
                    fontSize = 16f,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 10,
                }
            };
            Add(changePasswordHeader);
            
            var oldPasswordField = new TextField("Old Password")
            {
                isPasswordField = true,
                style =
                {
                    alignSelf = Align.FlexStart,
                    minWidth = 400,
                }
            };
            Add(oldPasswordField);
            
            var newPasswordField = new TextField("New Password")
            {
                isPasswordField = true,
                style =
                {
                    alignSelf = Align.FlexStart,
                    minWidth = 400,
                }
            };
            Add(newPasswordField);
            
            var confirmPasswordField = new TextField("Confirm Password")
            {
                isPasswordField = true,
                style =
                {
                    alignSelf = Align.FlexStart,
                    minWidth = 400,
                }
            };
            Add(confirmPasswordField);

            var errorText = new Label();
            Add(errorText);
            
            var changePasswordButton = new Button(async () =>
            {
                errorText.style.color = Color.red;
                if (newPasswordField.value != confirmPasswordField.value)
                {
                    errorText.text = "New passwords do not match!";
                    return;
                }
                
                var changePasswordRes = await session.ChangePassFromOld(oldPasswordField.value, newPasswordField.value);
                if (changePasswordRes.status != 200)
                {
                    errorText.text = changePasswordRes.error_message;
                    return;
                }
                errorText.text = "Password changed successfully";
                errorText.style.color = Color.green;
                oldPasswordField.value = "";
                newPasswordField.value = "";
                confirmPasswordField.value = "";
                Debug.Log("Password changed successfully");
            });
            changePasswordButton.Add(new Label("Change Password"));
            Add(changePasswordButton);
        }
        
        
        var roleSelection = new UserRoleSelectionElement("Roles", user.roles, userRoleOptions, true, true, () => { }, async (oldRoles, newRoles) =>
        {
            var addedRoles = newRoles.FindAll(r => !oldRoles.Contains(r));
            var removedRoles = oldRoles.FindAll(r => !newRoles.Contains(r));
            
            if (addedRoles.Count > 0)
            {
                var addRes = await session.AddUserRoles(user._id, addedRoles);
                if (addRes.status != 200)
                {
                    Debug.LogError($"Failed to add roles: {addRes.error_message}");
                    return false;
                }
                Debug.Log("Roles added successfully");
            }
            
            if (removedRoles.Count > 0)
            {
                var removeRes = await session.RemoveUserRoles(user._id, removedRoles);
                if (removeRes.status != 200)
                {
                    Debug.LogError($"Failed to remove roles: {removeRes.error_message}");
                    return false;
                }
                Debug.Log("Roles removed successfully");
            }

            return true;
        })
        {
            style =
            {
                marginTop = 10
            }
        };
        Add(roleSelection);
        
        var propsEditor = new UserPropertyEditorElement("Properties", user, userPropDefs, session, true, true, () => { }, async (oldProps, newProps) =>
        {
            var setProps = newProps.FindAll(p =>
            {
                var oldKvp = oldProps.Find(op => op.Key == p.Key);
                if(oldKvp == null)
                    return true;
                return oldKvp.Value != p.Value;
                
            }).ToDictionary(p => p.Key, p =>
            {
                var value = JsonConvert.DeserializeObject(p.Value);
                return value;
            });
            foreach(var prop in oldProps.FindAll(p => newProps.Find(v=> v.Key == p.Key) == null))
            {
                setProps.Add(prop.Key, null);
            }
            
            if (setProps.Count > 0)
            {
                var addRes = await session.SetUserProperties(user._id, setProps);
                if (addRes.status != 200)
                {
                    Debug.LogError($"Failed to set properties: {addRes.error_message}");
                    return false;
                }
                Debug.Log("Properties set successfully");
            }

            return true;
        })
        {
            style =
            {
                marginTop = 10
            }
        };
        Add(propsEditor);
        
        /*var deleteButton = new Button(async () =>
        {
            var deleteRes = await session.DeleteUser(user._id);
            if (deleteRes.status != 200)
            {
                Debug.LogError($"Failed to delete user: {deleteRes.error_message}");
                return;
            }
            Debug.Log("User deleted successfully");
        });
        deleteButton.Add(new Label("Delete"));
        userEditor.Add(deleteButton);*/
    }
    
}
