using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Threading.Tasks;
using CyberHub.Foundry.Database;
using CyberHub.Foundry.Database.API;
using CyberHub.Foundry.Editor.UIUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[Serializable]
public class UserPropKVP
{
    public string Key;
    public string Value;
    
    public UserPropKVP Clone()
    {
        return new UserPropKVP
        {
            Key = Key,
            Value = Value
        };
    }
}

[Serializable]
public class UserPropDefData
{
    public string key = "new_prop";
    public UserPropertyDef value = new()
    {
        get_roles = new(),
        set_roles = new()
    };
    public bool dirty = true;
}

public class UserPropertyEditorElement : VisualElement
{
    List<UserPropKVP> originalProps = new();
    List<UserPropKVP> _propValues = new();
    
    VisualElement listElement;
    bool _showAddRemove;
    Func<List<UserPropKVP>, List<UserPropKVP>, Task<bool>> _onSave;
    Action _onChange;

    private UserDoc _user;

    private DatabaseSession _session;
    
    /// <summary>
    /// Element for displaying and editing a list of user properties
    /// </summary>
    /// <param name="label"></param>
    /// <param name="session">Database to request information from</param>
    /// <param name="propDefs"></param>
    /// <param name="showAddRemove"></param>
    /// <param name="showReset"></param>
    /// <param name="onChange"></param>
    /// <param name="onSave"></param>
    public UserPropertyEditorElement(string label, 
        UserDoc user,
        List<UserPropDefData> propDefs,
        DatabaseSession session,
        bool showAddRemove = false, 
        bool showReset = false, 
        Action onChange = null, 
        Func<List<UserPropKVP>, List<UserPropKVP>, Task<bool>> onSave = null)
    {
        _showAddRemove = showAddRemove;
        _onSave = onSave;
        _onChange = onChange;
        _user = user;

        _session = session;
        Add(new Label(label)
        {
            style =
            {
                fontSize = 16f,
                unityFontStyleAndWeight = FontStyle.Bold
            }
        });
        listElement = new VisualElement();
        listElement.style.flexDirection = FlexDirection.Column;
        Add(listElement);

        RefreshProps();
        DrawList();
        
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
            var choices = new []{"Add Property"}.Union(propDefs.Select(d => d.key)).Union(_propValues.Select(p => p.Key)).ToList();
            var addRole = new PopupField<string>(choices, 0);
            addRole.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == "Add Property")
                    return;
                if (_propValues.All(kvp => kvp.Key != evt.newValue))
                {
                    AddValue(new UserPropKVP {Key = evt.newValue, Value = "null"}, onChange);
                    onChange?.Invoke();
                }
    
                addRole.SetValueWithoutNotify("Add Property");
            });
            addRole.style.maxWidth = 150;
            actions.Add(addRole);
        }
        
        if (showReset)
        {
            var reset = new Button(() =>
            {
                _propValues.Clear();
                _propValues.AddRange(originalProps);
                onChange?.Invoke();
                
                
            });
            reset.Add(new Label("Reset"));
            reset.style.maxWidth = 150;
            actions.Add(reset);
        }
        
        if (onSave != null)
        {
            var save = new Button(async () =>
            {
                var saveSuccessful = await onSave(originalProps, _propValues);
                if (saveSuccessful) {
                    RefreshProps();
                }
                else
                {
                    _propValues.Clear();
                    _propValues.AddRange(originalProps);
                    
                    DrawList();
                }
                
            });
            save.Add(new Label("Save"));
            save.style.maxWidth = 150;
            actions.Add(save);
        }
       
    }

    public void DrawList()
    {
        listElement.Clear();
        foreach (var valuePair in _propValues)
        {
            DrawValue(valuePair, _onChange);
        }
    }

    public void AddValue(UserPropKVP valuePair, Action onChange = null)
    {
        _propValues.Add(valuePair);
        DrawValue(valuePair, onChange);
    }

    private void DrawValue(UserPropKVP valuePair, Action onChange = null)
    {
        var box = new Box
        {
            name = valuePair.Key,
        };
        EditorUIUtils.SetBorderColor(box, Color.black);
        EditorUIUtils.SetBorderRadius(box, 5);
        EditorUIUtils.SetBorderWidth(box, 1);
        EditorUIUtils.SetPadding(box, 2);
        
        var valuesElement = new Box
        {
            name = valuePair.Key,
            style =
            {
                backgroundColor = Color.clear,
                alignItems = Align.Center,
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.SpaceBetween,
            }
        };
        box.Add(valuesElement);
        
        var buttonsElement = new Box
        {
            style =
            {
                flexDirection = FlexDirection.Row,
                justifyContent = Justify.FlexEnd,
                backgroundColor = Color.clear
            }
        };
        box.Add(buttonsElement);
        
        var keyField = new TextField
        {
            value = valuePair.Key,
            style =
            {
                minWidth = 150,
            }
        };
        keyField.RegisterValueChangedCallback(evt =>
        {
            valuePair.Key = evt.newValue;
            box.name = evt.newValue;
            onChange?.Invoke();
        });
        valuesElement.Add(keyField);

        var errorText = new Label
        {
            style =
            {
                color = Color.red
            }
        };
        buttonsElement.Add(errorText);
            
        var valueField = new TextField
        {
            value = valuePair.Value,
            multiline = true,
            style =
            {
                minWidth = 150,
            }
        };
        EditorUIUtils.SetBorderColor(valueField, Color.clear);
        EditorUIUtils.SetBorderRadius(valueField, 5);
        EditorUIUtils.SetBorderWidth(valueField, 1);
        valueField.RegisterValueChangedCallback(evt =>
        {
            try
            {
                JsonConvert.DeserializeObject(evt.newValue);
            } catch (Exception e)
            {
                EditorUIUtils.SetBorderColor(valueField, Color.red);
                errorText.text = e.Message;
                return;
            }
            errorText.text = "";
            EditorUIUtils.SetBorderColor(valueField, Color.clear);
            valuePair.Value = evt.newValue;
            onChange?.Invoke();
        });
        valuesElement.Add(valueField);
        
        if (_showAddRemove)
        {
            var removeButton = new Button(() =>
            {
                listElement.Remove(box);
                _propValues.Remove(valuePair);
                onChange?.Invoke();
            });
            removeButton.Add(new Label("Remove"));
            removeButton.style.maxWidth = 150;
            buttonsElement.Add(removeButton);
        }
        
        listElement.Add(box);
    }

    private async void RefreshProps()
    {
        var res = await _session.GetUserProperties(_user._id);
        if (res.status != 200)
        {
            Debug.LogError($"Failed to get user properties: {res.error_message}");
            return;
        }
        
        var resData = res.data.Select(p =>
        {
            string value = p.Value is string ? "\"" + p.Value + "\"" : p.Value.ToString();
            return new UserPropKVP { Key = p.Key, Value = value };
        }).ToList();
        
        _propValues.Clear();
        _propValues.AddRange(resData);
        originalProps.Clear();
        originalProps.AddRange(resData.Select(p => p.Clone()).ToList());
        DrawList();
    }
}
