using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CyberHub.Foundry.Database.API;
using CyberHub.Foundry.Editor.UIUtils;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class UserRoleDefData
{
    public string key = "new_role";
    public UserRoleDef value = new()
    {
        permissions = new()
    };
    public bool dirty = true;
        
}

public class UserRoleSelectionElement : VisualElement
{
    List<string> originalRoles;
    
    /// <summary>
    /// Element for displaying and editing a list of roles
    /// </summary>
    /// <param name="label"></param>
    /// <param name="selectedRoles">Currently selected roles, this list will be edited</param>
    /// <param name="roleOptions">List of roles the user can select from</param>
    /// <param name="showAddRemove">Show the add/remove buttons</param>
    /// <param name="showReset">Show a reset button</param>
    /// <param name="onChange">Called whenever a role is added or removed</param>
    /// <param name="onSave">If not null, a save button will be displayed. The callback is passed both the original list of roles, and the new list, and should return a bool defining if the save task completed successfully or not</param>
    public UserRoleSelectionElement(string label, 
        List<string> selectedRoles,
        List<UserRoleDefData> roleOptions, 
        bool showAddRemove = false, 
        bool showReset = false, 
        Action onChange = null, 
        Func<List<string>, List<string>, Task<bool>> onSave = null)
    {
        
        originalRoles = new List<string>(selectedRoles);
        Add(new Label(label)
        {
            style =
            {
                fontSize = 16f,
                unityFontStyleAndWeight = FontStyle.Bold
            }
        });
        var listView = new ListView(selectedRoles, -1f, () =>
        {
            var box = new Box();
            box.style.alignItems = Align.Center;
            EditorUIUtils.SetBorderColor(box, Color.black);
            EditorUIUtils.SetBorderRadius(box, 5);
            EditorUIUtils.SetBorderWidth(box, 1);
            EditorUIUtils.SetPadding(box, 2);

            var roleLabel = new Label();
            roleLabel.style.fontSize = 14f;
            roleLabel.name = "role";
            box.Add(roleLabel);


            return box;
        }, (e, i) => { e.Q<Label>("role").text = selectedRoles[i]; });
        listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        Add(listView);
        
        var actions = new Box
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.FlexEnd,
                backgroundColor = Color.clear
            }
        };
        Add(actions);

        if (showAddRemove)
        {
            List<string> choices = roleOptions.Select(d => d.key).Union(selectedRoles).Union(new[]
            {
                "self",
                "admin",
                "all"
            }).ToList();
            choices.Insert(0, "Add Role");
    
            var addRole = new PopupField<string>(choices, 0);
            addRole.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "Add Role")
                    return;
                if (!selectedRoles.Contains(evt.newValue))
                {
                    selectedRoles.Add(evt.newValue);
                    onChange?.Invoke();
                    listView.Rebuild();
                }
    
                addRole.SetValueWithoutNotify("Add Role");
            });
            addRole.style.maxWidth = 150;
            actions.Add(addRole);
    
            var removeSelected = new Button(() =>
            {
                if (listView.selectedIndex < 0)
                    return;
                selectedRoles.RemoveAt(listView.selectedIndex);
                onChange?.Invoke();
                listView.Rebuild();
            });
            removeSelected.Add(new Label("Remove Selected"));
            removeSelected.style.maxWidth = 150;
            actions.Add(removeSelected);
        }
        
        if (showReset)
        {
            var reset = new Button(() =>
            {
                selectedRoles.Clear();
                selectedRoles.AddRange(originalRoles);
                onChange?.Invoke();
                listView.Rebuild();
            });
            reset.Add(new Label("Reset"));
            reset.style.maxWidth = 150;
            actions.Add(reset);
        }
        
        if (onSave != null)
        {
            var save = new Button(async () =>
            {
                var saveSuccessful = await onSave(originalRoles, selectedRoles);
                if (saveSuccessful) {
                    originalRoles.Clear();
                    originalRoles.AddRange(selectedRoles);
                }
                else
                {
                    selectedRoles.Clear();
                    selectedRoles.AddRange(originalRoles);
                    listView.Rebuild();
                }
                
            });
            save.Add(new Label("Save"));
            save.style.maxWidth = 150;
            actions.Add(save);
        }
       
    }
}
