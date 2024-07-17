using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Codice.Client.Common;
using CyberHub.Foundry.Database;
using CyberHub.Foundry.Database.API;
using CyberHub.Foundry.Editor.UIUtils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Image = UnityEngine.UIElements.Image;
using Object = UnityEngine.Object;

public class DatabaseManagementWindow : EditorWindow
{
    
    public Dictionary<string, bool> PropDefRolesShown = new();
    
    public List<UserRoleDefData> userRoleDefs;
    public List<UserRoleDefData> originalUserRoleDefs;
    
    public List<UserPropDefData> originalUserPropDefs;
    public List<UserPropDefData> userPropDefs;
    
    public DatabaseSession session;
    
    [MenuItem("Foundry/Database/Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<DatabaseManagementWindow>("Database Manager");
        window.position = new Rect(100, 100, 800, 600);
    }
    
    private VisualElement userRoleDefsView;
    private VisualElement userPropDefsView;
    private VisualElement updateResetTemplateView;

    [SerializeField]
    private string selectedTab = "Login";
    private VisualElement tabContentRoot;

    private bool reset = false;
    private bool sent = false;
    
    readonly float headerFontSize = 16f;
    readonly float fontSize = 14f;

    readonly Color black = new Color(0.1f, 0.1f, 0.1f, 1f);
    readonly Color grey = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    readonly Color solidGrey = new Color(0.3f, 0.3f, 0.3f, 1f);
    
    private async void CreateGUI()
    {
        session = await DatabaseSession.GetActive();

        VisualElement root = rootVisualElement;

        var scrollView = new ScrollView();
        scrollView.style.paddingLeft = 10;
        scrollView.style.paddingRight = 10;
        root.Add(scrollView);
            
        //I already wrote everything and am too lazy to refactor
        root = scrollView;
            
            
        //Header
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;
        root.Add(header);
        
        var logoData = Resources.Load<Texture2D>("brane_icon");
        var logo = new Image();

        logo.image = logoData;
        logo.style.width = 50;
        logo.style.height = 50;
        logo.style.marginTop = 20;
        logo.style.marginBottom = 20;
        logo.scaleMode = ScaleMode.ScaleToFit;
        header.Add(logo);

        var configTitle = new Label("Brane Database Manager");
        configTitle.style.fontSize = headerFontSize;
        configTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        configTitle.style.marginBottom = 10;
        header.Add(configTitle);

        List<ToolbarToggle> navBarElements = session.LoggedIn ? new()
        {
            new ToolbarToggle
            {
                text = "Definitions",
                name = "Definitions"
            },
            new ToolbarToggle
            {
                text = "Users",
                name = "Users"
            },
            new ToolbarToggle
            {
                text = "Account",
                name = "Account"
            },
            new ToolbarToggle
            {
                text = "General",
                name = "General"
            }
        } : new()
        {
            new ToolbarToggle
            {
                text = "Login",
                name = "Login"
            },
            new ToolbarToggle
            {
                text = "Create Account",
                name = "CreateAccount"
            },
        };

        if (reset){
            navBarElements.Add(
                new ToolbarToggle {
                text = "Reset Password",
                name = "ResetPassword"
                }
            );
        }
        
        if (navBarElements.All(t => t.name != selectedTab))
            selectedTab = navBarElements.First().name;
        
        var navBar = new Toolbar()
        {
            style =
            {
                backgroundColor = Color.black,
                borderBottomWidth = 0,
                marginBottom = 0,
                height = 30,
            }
        };
        navBar.contentContainer.style.paddingTop = 5;
        navBar.contentContainer.style.paddingLeft = 5;
        navBar.contentContainer.style.paddingRight = 5;
        navBar.contentContainer.style.borderTopLeftRadius = 5;
        navBar.contentContainer.style.borderTopRightRadius = 5;
        root.Add(navBar);
        var tabContentContainer = new VisualElement()
        {
            style =
            {
                backgroundColor = Color.black,
                borderBottomLeftRadius = 5,
                borderBottomRightRadius = 5,
                borderBottomColor = Color.black,
                borderRightColor = Color.black,
                borderLeftColor = Color.black,
                borderBottomWidth = 2,
                borderRightWidth = 2,
                borderLeftWidth = 2,
                marginTop = 0,
            }
        };
        EditorUIUtils.SetPadding(tabContentContainer, 0);
        root.Add(tabContentContainer);

        tabContentRoot ??= new VisualElement
        {
            style =
            {
                marginTop = 15,
                backgroundColor = solidGrey,
            }
        };
        EditorUIUtils.SetBorderRadius(tabContentRoot, 5);
        EditorUIUtils.SetPadding(tabContentRoot, 5);
        EditorUIUtils.SetMargin(tabContentRoot, 2);
        tabContentContainer.Add(tabContentRoot);
        
        foreach(var tab in navBarElements)
        {
            tab.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    selectedTab = tab.name;
                    foreach(var navBarElement in navBarElements)
                        navBarElement.value = navBarElement.name == selectedTab;
                    DrawTabContent();
                }
            });
            tab.value = tab.name == selectedTab;
            
            tab.style.borderTopLeftRadius = 5;
            tab.style.borderTopRightRadius = 5;
            tab.style.fontSize = headerFontSize;
            tab.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            navBar.Add(tab);
        }

        DrawTabContent();
    }
    
    private void DrawTabContent()
    {
        tabContentRoot.Clear();
        switch (selectedTab)
        {
            case "Login":
                DrawLoginTab(tabContentRoot);
                break;
            case "CreateAccount":
                CreateAccountTab(tabContentRoot);
                break;
            case "ResetPassword":
                ResetPasswordTab(tabContentRoot);
                break;
            case "Definitions":
                DrawDefsTab(tabContentRoot);
                break;
            case "Users":
                DrawUsersTab(tabContentRoot);
                break;
            case "Account":
                DrawAccountTab(tabContentRoot);
                break;
            case "General":
                DrawGeneralTab(tabContentRoot);
                break;
        }
    }

    private void DrawLoginTab(VisualElement parent)
    {
        var loginBox = CreateBox(solidGrey, black);

        var username = new TextField("Username");
        var password = new TextField("Password");
        password.isPasswordField = true;

        Label failText = null;
        Action loginCallback = async () =>
        {
            bool shouldRefresh = false;
            try
            {
                var res = await session.Login(username.value, password.value);
                if (!res.IsSuccess)
                {
                    if (failText == null)
                    {
                        failText = new Label($"Login failed: {res.error_message}")
                        {
                            style =
                            {
                                color = Color.red
                            }
                        };
                        loginBox.Add(failText);
                    }
                    else
                        failText.text = $"Login failed: {res.error_message}";
                }
                else
                {
                    if (failText != null)
                    {
                        loginBox.Remove(failText);
                        failText = null;
                    }

                    shouldRefresh = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (shouldRefresh)
            {
                rootVisualElement.Clear();
                CreateGUI();
            }
        };

        Action resetCallback = () =>
        {
            reset = true;
            selectedTab = "ResetPassword";
            rootVisualElement.Clear();
            CreateGUI();
        };

    

        password.RegisterCallback((KeyDownEvent evt) =>
        {
            if (evt.keyCode == KeyCode.Return)
                loginCallback();
        });


        var loginButton = new Button(loginCallback);
        var resetButton = new Button(resetCallback);


        loginButton.Add(new Label("Login"));
        resetButton.Add(new Label("Forgot Password?"));
        loginBox.Add(username);
        loginBox.Add(password);
        loginBox.Add(loginButton);
        loginBox.Add(resetButton);
        parent.Add(loginBox);
    }

    private void ResetPasswordTab(VisualElement parent) {
        var resetBox = CreateBox(solidGrey, black);

        var email = new TextField("Email");
        var reset_code = new TextField("Reset Code");
        var new_password = new TextField("New Password");
        new_password.isPasswordField = true;

        Label failText = null;
        Action resetPasswordCallback = async () => {
            bool shouldRefresh = false;
            try {
                var res = await session.RequestResetPassword(email.value);
                if (!res.IsSuccess){
                    if (failText == null) {
                        failText = new Label($"Password reset failed: {res.error_message}") {
                            style =
                            {
                                color = Color.red
                            }
                        };
                        resetBox.Add(failText);
                    }
                    else
                        failText.text = $"Password reset failed: {res.error_message}";
                } else {
                    if (failText != null)
                    {
                        resetBox.Remove(failText);
                        failText = null;
                    }
                    
                    Debug.Log("Reset code sent.");
                    sent = true;
                    shouldRefresh = true;
                }
            } catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (shouldRefresh)
            {
                rootVisualElement.Clear();
                CreateGUI();
            }
        };

        Action resetCodeCallback = async () => {
            bool shouldRefresh = false;
            try {
                var res = await session.TryResetPassword(reset_code.value, new_password.value);
                if (!res.IsSuccess){
                    if (failText == null) {
                        failText = new Label($"Password reset failed: {res.error_message}") {
                            style =
                            {
                                color = Color.red
                            }
                        };
                        resetBox.Add(failText);
                    }
                    else
                        failText.text = $"Password reset failed: {res.error_message}";
                } else {
                    if (failText != null)
                    {
                        resetBox.Remove(failText);
                        failText = null;
                    }
                    
                    Debug.Log("Password successfully reset.");
                    sent = false;
                    reset = false;
                    selectedTab = "Login";
                    shouldRefresh = true;
                }
            } catch (Exception e)
            {
                Debug.LogError(e);
            }
            if (shouldRefresh)
            {
                rootVisualElement.Clear();
                CreateGUI();
            }
        };

        
        email.RegisterCallback((KeyDownEvent evt) =>
        {
            if (evt.keyCode == KeyCode.Return){
                if (sent == true) {
                    resetCodeCallback();
                } else{
                    resetPasswordCallback();
                }
            }
        });

        var resetButton = new Button(resetPasswordCallback);
        var submitButton = new Button(resetCodeCallback);

        if (sent == true) {
            submitButton.Add(new Label("Submit"));
            resetBox.Add(reset_code);
            resetBox.Add(new_password);
            resetBox.Add(submitButton);
        } else {
            resetButton.Add(new Label("Send Reset Code"));
            resetBox.Add(email);
            resetBox.Add(resetButton);
        }

        parent.Add(resetBox);
    }

    private void CreateAccountTab(VisualElement parent)
    {
        var createAccountBox = CreateBox(solidGrey, black);

        var username = new TextField("Username");
        var password = new TextField("Password");
        password.isPasswordField = true;
        var email = new TextField("Email");

        Label failText = null;
        Action createAccountCallback = async () =>
        {
            bool shouldRefresh = false;
            try
            {
                var res = await session.CreateAccount(username.value, password.value, email.value);
                if (!res.IsSuccess)
                {
                    if (failText == null)
                    {
                        failText = new Label($"Account creation failed: {res.error_message}")
                        {
                            style =
                            {
                                color = Color.red
                            }
                        };
                        createAccountBox.Add(failText);
                    }
                    else
                        failText.text = $"Account creation failed: {res.error_message}";
                }
                else
                {
                    if (failText != null)
                    {
                        createAccountBox.Remove(failText);
                        failText = null;
                    }
                    
                    Debug.Log("Account created successfully, logging in...");
                    shouldRefresh = true;
                    
                    res = await session.Login(username.value, password.value);
                    if (!res.IsSuccess)
                        Debug.LogError($"Failed to login after account creation: {res.error_message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (shouldRefresh)
            {
                rootVisualElement.Clear();
                CreateGUI();
            }
        };

        password.RegisterCallback((KeyDownEvent evt) =>
        {
            if (evt.keyCode == KeyCode.Return)
                createAccountCallback();
        });

        var createAccountButton = new Button(createAccountCallback);

        createAccountButton.Add(new Label("Create Account"));
        createAccountBox.Add(username);
        createAccountBox.Add(password);
        createAccountBox.Add(email);
        createAccountBox.Add(createAccountButton);
        parent.Add(createAccountBox);
    }

    private void DrawDefsTab(VisualElement parent)
    {
        if (!session.LocalUser.roles.Contains("admin"))
        {
            parent.Add(new Label("User is not marked as an admin. Cannot edit definitions.")
            {
                style =
                {
                    color = Color.red
                }
            });
            return;
        }
        RefreshUserRoleDefsView(parent);
        RefreshPropertyDefsView(parent);
    }
    
    private void DrawUsersTab(VisualElement parent)
    {
        var findUsersHeader = new Label("Find Users")
        {
            style =
            {
                fontSize = headerFontSize,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 10,
            }
        };
        parent.Add(findUsersHeader);

        var usersSearchBar = new TextField("Search")
        {
            style =
            {
                alignSelf = Align.FlexStart,
                minWidth = 400,
            }
        };
        parent.Add(usersSearchBar);
        
        var searchResultCount = new TextField("Max Results")
        {
            style =
            {
                alignSelf = Align.FlexStart,
                minWidth = 200,
            }
        };
        searchResultCount.value = "20";
        searchResultCount.RegisterValueChangedCallback(evt =>
        {
            if (!int.TryParse(evt.newValue, out var res))
            {
                EditorUIUtils.SetBorderColor(searchResultCount, Color.red);
                EditorUIUtils.SetBorderWidth(searchResultCount, 2);
            }
            else
            {
                EditorUIUtils.SetBorderWidth(searchResultCount, 0);
            }
        });
        parent.Add(searchResultCount);
        
        var searchButton = new Button();
        searchButton.Add(new Label("Search"));
        parent.Add(searchButton);
        
        var resultsHeader = new Label("Search Results")
        {
            style =
            {
                fontSize = fontSize,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginTop = 10,
            }
        };
        parent.Add(resultsHeader);

        var searchResults = CreateBox(grey, Color.black);
        parent.Add(searchResults);
    
        var selectedUserHeader = new Label("Selected User")
        {
            style =
            {
                fontSize = headerFontSize,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginTop = 10,
                
            }
        };
        parent.Add(selectedUserHeader);
        
        var selectedUserBox = CreateBox(grey, black);
        parent.Add(selectedUserBox);

        Func<string, ulong, ulong, Task> searchUsers = async (username, startIndex, count) =>
        {
            var res = await session.SearchUsersByUsername(username, null, startIndex, count);
            if (res.status != 200)
            {
                Debug.LogError($"Failed to search users: {res.error_message}");
                return;
            }
            searchResults.Clear();
            var scrollView = new ScrollView();
            searchResults.Add(scrollView);
            foreach (var user in res.data)
            {
                var userItem = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        justifyContent = Justify.SpaceBetween,
                    }
                };
                
                var selectButton = new Button(async () =>
                {
                    var userRes = await session.GetUser(user._id);
                    if (userRes.status != 200)
                    {
                        Debug.LogError($"Failed to get user: {userRes.error_message}");
                        return;
                    }
                    
                    selectedUserBox.Clear();
                    DrawUserEditor(selectedUserBox, userRes.data);
                });
                selectButton.Add(new Label("Select"));
                userItem.Add(selectButton);
                
                
                var usernameLabel = new Label(user.username);
                userItem.Add(usernameLabel);
                
                var objectIdLabel = new Label($"oid: {user._id}");
                userItem.Add(objectIdLabel);
                
                scrollView.Add(userItem);
            }
        };
        searchUsers("", 0, 20);
        
        usersSearchBar.RegisterCallback((KeyDownEvent evt) =>
        {
            if (evt.keyCode == KeyCode.Return && ulong.TryParse(searchResultCount.value, out ulong count))
                searchUsers(usersSearchBar.value, 0, count);
        });

        searchButton.clicked += () =>
        {
            if (ulong.TryParse(searchResultCount.value, out var count))
                searchUsers(usersSearchBar.value, 0, count);
        };
    }

    private async void DrawAccountTab(VisualElement parent)
    {
        parent.Add(new Label($"Currently logged in as {session.LocalUser.username}")
        {
            style =
            {
                fontSize = headerFontSize,
                unityFontStyleAndWeight = FontStyle.Bold,
                marginBottom = 10,
            }
        });

        if (userRoleDefs == null || originalUserRoleDefs == null)
        {
            userRoleDefs = new List<UserRoleDefData>();
            originalUserRoleDefs = new List<UserRoleDefData>();
            UpdateUserRollDefs();
        }
        
        if (userPropDefs == null || originalUserPropDefs == null)
        {
            userPropDefs = new List<UserPropDefData>();
            originalUserPropDefs = new List<UserPropDefData>();
            UpdatePropertyDefs();
        }
        
        DrawUserEditor(parent, session.LocalUser);
        
        var logoutButton = new Button(async () =>
        {
            await session.Logout(true);
            
            userRoleDefs = null;
            originalUserRoleDefs = null;
            userPropDefs = null;
            originalUserPropDefs = null;
            
            rootVisualElement.Clear();
            CreateGUI();
        });
        logoutButton.Add(new Label("Logout"));
        parent.Add(logoutButton);
    }

    private void DrawUserEditor(VisualElement parent, UserDoc user)
    {
        parent.Add(new UserEditorElement(user, userRoleDefs, userPropDefs, session));
    }

    private async void DrawGeneralTab(VisualElement parent){
        if (session.LocalUser.roles.Contains("admin"))
        {
            parent.Add(new Label("Reset Email Template Editor") {
                style = {
                    fontSize = headerFontSize,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10,
                }
            });

            var reset_res = await session.GetResetEmail();

            var resetEmailEditorBox = CreateBox(solidGrey, black);
            var code = new TextField("HTML", -1, true, false, '*') {
                value = reset_res.data
            };

            Label failText = null;
            Action updateResetEmailCallback = async () =>
            {
                bool shouldRefresh = false;
                try {
                    var res = await session.UpdateResetEmail(code.value);
                    if (!res.IsSuccess)
                    {
                        if (failText == null)
                        {
                            failText = new Label($"Reset Email Update failed: {res.error_message}")
                            {
                                style =
                                {
                                    color = Color.red
                                }
                            };
                            resetEmailEditorBox.Add(failText);
                        }
                        else
                            failText.text = $"Reset Email Update failed: {res.error_message}";
                    }
                    else
                    {
                        if (failText != null)
                        {
                            resetEmailEditorBox.Remove(failText);
                            failText = null;
                        }
                        
                        Debug.Log("Reset Email Updated successfully!");
                        shouldRefresh = true;
                    }
                }
                catch (Exception e) {
                    Debug.LogError(e);
                }

                if (shouldRefresh) {
                    rootVisualElement.Clear();
                    CreateGUI();
                }
            };

            var updateResetEmailButton = new Button(updateResetEmailCallback);
            updateResetEmailButton.Add(new Label("Update Reset Email"));

            resetEmailEditorBox.Add(code);
            resetEmailEditorBox.Add(updateResetEmailButton);
            parent.Add(resetEmailEditorBox);
        }
    }

    private Box CreateBox(Color background, Color border)
    {
        var newBox = new Box();
        EditorUIUtils.SetBorderRadius(newBox, 7);
        EditorUIUtils.SetPadding(newBox, 10);
        EditorUIUtils.SetBorderWidth(newBox, 2);
        EditorUIUtils.SetBorderColor(newBox, border);
        newBox.style.backgroundColor = background;

        return newBox;
    }

    private void RefreshUserRoleDefsView(VisualElement root = null)
    {
        if (userRoleDefsView == null && root == null)
            return;
        if (root == null)
        {
            userRoleDefsView.Clear();
        }
        else
        {
            userRoleDefsView = new VisualElement();
            userRoleDefsView.name = "userRoleDefsView";
            root.Add(userRoleDefsView);
        }
        
        var roleDefsBox = CreateBox(grey, black);
        roleDefsBox.style.marginBottom = 20;
        userRoleDefsView.Add(roleDefsBox);
            
        var roleDefsTitle = new Label("Role Definitions");
        roleDefsTitle.style.fontSize = fontSize + 1;
        roleDefsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        roleDefsTitle.style.marginBottom = 5;
        roleDefsBox.Add(roleDefsTitle);
            
        if (userRoleDefs == null || originalUserRoleDefs == null)
        {
            userRoleDefs = new List<UserRoleDefData>();
            originalUserRoleDefs = new List<UserRoleDefData>();
            UpdateUserRollDefs();
        }
        
        var userRoleDefsListView = new ListView(userRoleDefs, -1f, () =>
            {
                var box = new Box();
                EditorUIUtils.SetPadding(box, 5);
                
                var keyHeader = new Label("Key");
                keyHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                keyHeader.style.fontSize = headerFontSize;
                keyHeader.name = "key_header";
                box.Add(keyHeader);

                var keyInput = new TextField("Name");
                keyInput.name = "key";
                
                var permissions = new TextField("Permissions");
                permissions.name = "permissions";
                
                box.Add(keyInput);
                box.Add(permissions);
                
                return box;
            }, (e, i)=> 
            {
                if (userRoleDefs[i] == null)
                    userRoleDefs[i] = new UserRoleDefData();
                var data = userRoleDefs[i];
                var key = e.Q<TextField>("key");
                key.UnregisterCallback<ChangeEvent<string>>(null);
                key.RegisterValueChangedCallback(evt =>
                {
                    e.Q<Label>("key_header").text = string.IsNullOrWhiteSpace(evt.newValue) ? "[Empty]" : evt.newValue;
                });
                key.value = data.key;
                key.RegisterValueChangedCallback(evt =>
                {
                    data.key = evt.newValue;
                    data.dirty = true;
                });
                
                var permissions = e.Q<TextField>("permissions");
                if (data.value.permissions.Count > 0)
                    permissions.value = data.value.permissions.Aggregate((a, b) => a + ", " + b);
                permissions.UnregisterCallback<ChangeEvent<string>>(null);
                permissions.RegisterValueChangedCallback(evt =>
                {
                    data.value.permissions = evt.newValue.Split(',')
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s.Trim()).ToList();
                    data.dirty = true;
                });
            });
            userRoleDefsListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            userRoleDefsListView.showAddRemoveFooter = true;
            userRoleDefsListView.showBoundCollectionSize = true;
            roleDefsBox.Add(userRoleDefsListView);
            
            var rolesRefreshButton = new Button(() =>
            {
                UpdateUserRollDefs();
            });
            rolesRefreshButton.Add(new Label("Refresh"));
            roleDefsBox.Add(rolesRefreshButton);
            
            var rolesResetButton = new Button(() =>
            {
                userRoleDefs.Clear();
                foreach (var roleDef in originalUserRoleDefs)
                {
                    userRoleDefs.Add(new UserRoleDefData
                    {
                        key = roleDef.key,
                        value = roleDef.value.Clone()
                    });
                }
                RefreshUserRoleDefsView();
            });
            rolesResetButton.Add(new Label("Reset"));
            roleDefsBox.Add(rolesResetButton);
            
            var rolesSaveButton = new Button(async () =>
            {
                var toUpdate = userRoleDefs.Where(d => d.dirty).ToDictionary((d)=>d.key, d=>d.value);
                var toDelete = originalUserRoleDefs.Where(d => userRoleDefs.All(ud => ud.key != d.key)).Select(d=>d.key).ToList();
                if (toUpdate.Count == 0 && toDelete.Count == 0)
                    return;

                var res = await session.DefineUserRoles(toUpdate, toDelete);
                if (!res.IsSuccess)
                {
                    Debug.LogError($"Failed to update user role definitions: {res.error_message}");
                    return;
                }
                Debug.Log("User role definitions updated with result " + res.status);
                UpdateUserRollDefs();
            });
            rolesSaveButton.Add(new Label("Save"));
            roleDefsBox.Add(rolesSaveButton);

    }

    private void RefreshPropertyDefsView(VisualElement root = null)
    {
        if(userPropDefsView == null && root == null)
            return;
        if (root == null)
        {
            userPropDefsView.Clear();
        }
        else
        {
            userPropDefsView = new VisualElement();
            userPropDefsView.name = "userRoleDefsView";

            root.Add(userPropDefsView);
        }
        
        PropDefRolesShown = PropDefRolesShown.Where(kv => userPropDefs.Any(d => d.key == kv.Key)).ToDictionary((d) => d.Key, d => d.Value);
        
        var userPropDefsBox = CreateBox(grey, black);
        userPropDefsBox.style.marginBottom = 20;
        userPropDefsView.Add(userPropDefsBox);
    
        var userPropDefsTitle = new Label("User Property Definitions");
        userPropDefsTitle.style.fontSize = fontSize + 1;
        userPropDefsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        userPropDefsTitle.style.marginBottom = 5;
        userPropDefsBox.Add(userPropDefsTitle);
        
        if (userPropDefs == null || originalUserPropDefs == null)
        {
            userPropDefs = new List<UserPropDefData>();
            originalUserPropDefs = new List<UserPropDefData>();
            UpdatePropertyDefs();
        }

        foreach (var data in userPropDefs)
        {
            var box = new Box();
            EditorUIUtils.SetPadding(box, 5);
            EditorUIUtils.SetMargin(box, 4);
            EditorUIUtils.SetBorderColor(box, Color.black);
            EditorUIUtils.SetBorderRadius(box, 5);
            
            var keyHeader = new Label(data.key);
            keyHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            keyHeader.style.fontSize = headerFontSize;
            keyHeader.name = "key_header";
            box.Add(keyHeader);

            var key = new TextField("Key");
            key.name = "key";
            box.Add(key);
            
            key.RegisterValueChangedCallback(evt =>
            {
                keyHeader.text = string.IsNullOrWhiteSpace(evt.newValue) ? "[Empty]" : evt.newValue;
            });
            key.value = data.key;
            key.RegisterValueChangedCallback(evt =>
            {
                data.key = evt.newValue;
                data.dirty = true;
            });

            var roleSelections = new Foldout();
            roleSelections.value = PropDefRolesShown.GetValueOrDefault(data.key, false);
            roleSelections.RegisterValueChangedCallback(evt =>
            {
                PropDefRolesShown[data.key] = evt.newValue;
            });
            
            roleSelections.text = "Role Permissions";
            roleSelections.contentContainer.style.flexDirection = FlexDirection.Row;
            roleSelections.contentContainer.style.justifyContent = Justify.FlexStart;
            
            box.Add(roleSelections);
            
            var getRoles = new UserRoleSelectionElement("Get Roles", data.value.get_roles, userRoleDefs, true, false, () =>
            {
                data.dirty = true;
            })
            {
                style =
                {
                    maxHeight = 600,
                    width = 240,
                }
            };
            EditorUIUtils.SetMargin(getRoles, 5);
            EditorUIUtils.SetPadding(getRoles, 5);
            EditorUIUtils.SetBorderWidth(getRoles, 2);
            EditorUIUtils.SetBorderColor(getRoles, Color.black);
            EditorUIUtils.SetBorderRadius(getRoles, 5);
            roleSelections.contentContainer.Add(getRoles);
            
            var setRoles = new UserRoleSelectionElement("Set Roles", data.value.set_roles, userRoleDefs, true, false, () =>
            {
                data.dirty = true;
            })
            {
                style =
                {
                    maxHeight = 600,
                    width = 240,
                }
            };
            EditorUIUtils.SetMargin(setRoles, 5);
            EditorUIUtils.SetPadding(setRoles, 5);
            EditorUIUtils.SetBorderWidth(setRoles, 2);
            EditorUIUtils.SetBorderColor(setRoles, Color.black);
            EditorUIUtils.SetBorderRadius(setRoles, 5);
            roleSelections.contentContainer.Add(setRoles);
            userPropDefsBox.Add(box);

            var deleteButton = new Button(() =>
            {
                userPropDefs.Remove(data);
                PropDefRolesShown.Remove(data.key);
                userPropDefsBox.Remove(box);
            })
            {
                style =
                {
                    alignSelf = Align.FlexEnd,
                    marginTop = 15,
                    marginBottom = 2
                }
            };
            deleteButton.Add(new Label("Delete"));
            box.Add(deleteButton);
        }
        
        var addButton = new Button(() =>
        {
            userPropDefs.Add(new UserPropDefData
            {
                key = "new_prop",
                value = new UserPropertyDef
                {
                    get_roles = new List<string>(),
                    set_roles = new List<string>()
                },
                dirty = true
            });
            RefreshPropertyDefsView();
        });
        addButton.style.marginTop = 10;
        addButton.style.marginBottom = 20;
        addButton.style.alignSelf = Align.FlexEnd;
        addButton.Add(new Label("Add"));
        userPropDefsBox.Add(addButton);
        
        
        var updateButton = new Button(() =>
        {
            UpdatePropertyDefs();
        });
        updateButton.Add(new Label("Refresh"));
        userPropDefsBox.Add(updateButton);
        
        var resetButton = new Button(() =>
        {
            userPropDefs.Clear();
            foreach (var propDef in originalUserPropDefs)
            {
                userPropDefs.Add(new UserPropDefData
                {
                    key = propDef.key,
                    value = propDef.value.Clone()
                });
            }
            RefreshPropertyDefsView();
        });
        resetButton.Add(new Label("Reset"));
        userPropDefsBox.Add(resetButton);
        
        var saveButton = new Button(async () =>
        {
            var toUpdate = userPropDefs.Where(d => d.dirty).ToDictionary((d)=>d.key, d=>d.value);
            var toDelete = originalUserPropDefs.Where(d => !userPropDefs.Any(ud => ud.key == d.key)).Select(d=>d.key).ToList();
            if (toUpdate.Count == 0 && toDelete.Count == 0)
                return;

            var res = await session.DefineUserProperties(toUpdate, toDelete);
            if (res.status != 200)
            {
                Debug.LogError($"Failed to update user property definitions: {res.error_message}");
                return;
            }
            Debug.Log("User property definitions updated with result " + res.status);
            UpdatePropertyDefs();
        });
        saveButton.Add(new Label("Save"));
        userPropDefsBox.Add(saveButton);
    }
    
    private async void UpdatePropertyDefs()
    {
        var newUserPropDefs = await session.GetUserPropertyDefs();
        if (newUserPropDefs.status != 200)
        {
            Debug.LogError($"Failed to get user property definitions: {newUserPropDefs.error_message}");
            return;
        }
        
        originalUserPropDefs.Clear();
        userPropDefs.Clear();
        foreach(var propDef in newUserPropDefs.data)
        {
            originalUserPropDefs.Add(new UserPropDefData
            {
                key = propDef.Key,
                value = propDef.Value.Clone(),
                dirty = false
            });
            userPropDefs.Add(new UserPropDefData
            {
                key = propDef.Key,
                value = propDef.Value.Clone(),
                dirty = false
            });
        }

        if (userPropDefsView != null)
            RefreshPropertyDefsView();
        else
        {
            rootVisualElement.Clear();
            CreateGUI();
        }
    }
    
    
    private async void UpdateUserRollDefs()
    {
        var newUserRoleDefs = await session.GetUserRoleDefs();
        if (newUserRoleDefs.status != 200)
        {
            Debug.LogError($"Failed to get user role definitions: {newUserRoleDefs.error_message}");
            return;
        }
        
        originalUserRoleDefs.Clear();
        userRoleDefs.Clear();
        foreach(var roleDef in newUserRoleDefs.data)
        {
            originalUserRoleDefs.Add(new UserRoleDefData
            {
                key = roleDef.Key,
                value = roleDef.Value.Clone(),
                dirty = false
            });
            userRoleDefs.Add(new UserRoleDefData
            {
                key = roleDef.Key,
                value = roleDef.Value.Clone(),
                dirty = false
            });
        }
        if (userRoleDefsView != null)
            RefreshUserRoleDefsView();
        else
        {
            rootVisualElement.Clear();
            CreateGUI();
        }
    }
}
