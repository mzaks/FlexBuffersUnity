using System.Collections.Generic;
using System.IO;
using System.Linq;
using FlexBuffers;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using FlexBuffers;

public class FlexBufferTreeWindow : EditorWindow
{
    private TreeViewState _treeViewState;

    private FlexBufferTreeView _treeView;
    private string _path = "";
    private int _fileSize;
    private string _jsonPath = "";
    private int _jsonFileSize;
    private string _query = "";
    
    void OnEnable ()
    {
        // Check whether there is already a serialized view state (state 
        // that survived assembly reloading)
        if (_treeViewState == null)
            _treeViewState = new TreeViewState ();
        _path = "";
        _jsonPath = "";
        _query = "";
        _fileSize = 0;
    }
    
    void OnGUI ()
    {
        if (GUILayout.Button("Open FlexBuffer file..."))
        {
            var jsonPath = EditorUtility.OpenFilePanel("Select FlexBuffer file", "", "bytes");
            if (jsonPath.Length == 0)
            {
                return;
            }

            _path = jsonPath;

            _query = "";
            
            var bytes = File.ReadAllBytes(_path);

            _fileSize = bytes.Length;

            var root = FlxValue.FromBytes(bytes);
            _treeView = new FlexBufferTreeView(root, _treeViewState);
            _treeViewState = new TreeViewState ();
        }

        if (_path.Length == 0)
        {
            return;
        }

        var jsonFileString = _jsonPath.Length > 0 ? $"| {_jsonPath} [{_jsonFileSize}]" : "";
        GUILayout.Label($"{_path} [{_fileSize}] {jsonFileString}");

        if (_jsonPath.Length > 0)
        {
            GUILayout.BeginHorizontal();
        }

        if (GUILayout.Button("Export as JSON..."))
        {
            var jsonPath = EditorUtility.SaveFilePanel(
                "Save as JSON",
                "",
                $"{Path.GetFileNameWithoutExtension(_path)}.json",
                "json");

            if (jsonPath.Length != 0)
            {
                var prettyJson = FlexBuffersPreferences.PrettyPrintedJson ? _treeView._rootValue.ToPrettyJson() : _treeView._rootValue.ToJson;
                _jsonFileSize = prettyJson.Length;
                File.WriteAllText(jsonPath, prettyJson);
            }

            _jsonPath = jsonPath;
        }

        if (_jsonPath.Length > 0)
        {
            if (GUILayout.Button("Open JSON"))
            {
                Application.OpenURL($"file://{_jsonPath}");
            }
            
            if (GUILayout.Button("Import from JSON"))
            {
                var bytes = JsonToFlexBufferConverter.ConvertFile(_jsonPath);

                if (bytes != null)
                {
                    File.WriteAllBytes(_path, bytes);
                }

                var root = FlxValue.FromBytes(bytes);
                _treeView = new FlexBufferTreeView(root, _treeViewState);
                _treeViewState = new TreeViewState ();
            }
            GUILayout.EndHorizontal();
        }
        
        var newQuery = GUILayout.TextField(_query);

        if (newQuery != _query)
        {
            var query = FlxQueryParser.Convert(newQuery);
            _query = newQuery;
            _treeView?.SetQuery(query);
        }

        _treeView?.OnGUI(new Rect(0, 80, position.width, position.height - 80));


        if (Event.current.type == EventType.KeyUp && (Event.current.modifiers == EventModifiers.Control ||
                                                      Event.current.modifiers == EventModifiers.Command))
        {
            if (Event.current.keyCode == KeyCode.C)
            {
                Event.current.Use();
                var selection = _treeView?.GetSelection();
                if (selection != null && selection.Count > 0)
                {
                    if (_treeView.GetRows().First(item => item.id == selection[0]) is FlxValueTreeViewItem row)
                    {
                        GUIUtility.systemCopyBuffer = row.FlxValue.ToJson;
                    }
                }
            }
        }
    }
    
    [MenuItem("Tools/FlexBuffers/FlexBuffer Browser")]
    private static void ShowWidow()
    {
        var window = GetWindow<FlexBufferTreeWindow> ();
        window.titleContent = new GUIContent ("FlexBuffer Browser");
        window.Show ();
    }
}

public class FlexBufferTreeView : TreeView
{
    internal readonly FlxValue _rootValue;
    private FlxQuery _query;

    internal void SetQuery(FlxQuery query)
    {
        _query = query;
        Reload();
    }

    protected override TreeViewItem BuildRoot()
    {
        var root = new TreeViewItem      { id = -1, depth = -1, displayName = "Root" };
        var flxRoot = new FlxValueTreeViewItem(_rootValue, query:_query);
        root.AddChild(flxRoot);
        return root;
    }
    
    public FlexBufferTreeView(FlxValue rootValue, TreeViewState state) : base(state)
    {
        _rootValue = rootValue;
        Reload();
    }
    
    
}

public class FlxValueTreeViewItem : TreeViewItem
{
    public readonly FlxValue FlxValue;
    private int _depth;
    private FlxValueTreeViewItem _parent;
    private string _key;
    private List<TreeViewItem> _children;
    private FlxQuery _query;

    public FlxValueTreeViewItem(FlxValue value, int depth = 0, FlxValueTreeViewItem parent = null, string key = "", FlxQuery query = null)
    {
        FlxValue = value;
        _depth = depth;
        _parent = parent;
        _key = key;
        _query = query;
    }
    
    public override int id => FlxValue.BufferOffset;
    public override string displayName {
        get
        {
            var type = FlxValue.ValueType;
            if (TypesUtil.IsAVector(type))
            {
                return $"{_key}{type}[{FlxValue.AsVector.Length}]";
            }

            if (type == Type.Map)
            {
                return $"{_key}{type}[{FlxValue.AsMap.Length}]";
            }

            if (FlxValue.IsNull)
            {
                return $"{_key}null";
            }

            if (type == Type.Bool)
            {
                return $"{_key}{FlxValue.AsBool}";
            }

            if (type == Type.Blob)
            {
                return $"{_key}{FlxValue.ToJson}";
            }

            if (type == Type.Float || type == Type.IndirectFloat)
            {
                return $"{_key}{FlxValue.AsDouble}";
            }

            if (type == Type.Int || type == Type.IndirectInt)
            {
                return $"{_key}{FlxValue.AsLong}";
            }

            if (type == Type.Uint || type == Type.IndirectUInt)
            {
                return $"{_key}{FlxValue.AsULong}";
            }

            if (type == Type.String)
            {
                return $"{_key}'{FlxValue.AsString}'";
            }

            return "UNKNOWN";
        }
    }
    public override int depth => _depth;
    public override bool hasChildren => TypesUtil.IsAVector(FlxValue.ValueType) || FlxValue.ValueType == Type.Map;

    public override List<TreeViewItem> children
    {
        get
        {
            if (TypesUtil.IsAVector(FlxValue.ValueType))
            {
                var vec = FlxValue.AsVector;
                if (_children == null)
                {
                    _children = new List<TreeViewItem>(vec.Length);
                    var index = 0;
                    foreach (var item in vec)
                    {
                        if (_query != null)
                        {
                            var confirms = _query.Constraint.Confirms(vec, index);
                            if (confirms)
                            {
                                _children.Add(new FlxValueTreeViewItem(item, _depth+1, this, $"{index} : ", _query.Propagating ? _query : _query.Next));
                            } else if (_query.Optional)
                            {
                                _children.Add(new FlxValueTreeViewItem(item, _depth+1, this, $"{index} : ", _query));
                            }
                        }
                        else
                        {
                            _children.Add(new FlxValueTreeViewItem(item, _depth+1, this, $"{index} : "));    
                        }
                        
                        index++;
                    }
                }

                return _children;
            }

            if (FlxValue.ValueType == Type.Map)
            {
                var map = FlxValue.AsMap;
                if (_children == null)
                {
                    _children = new List<TreeViewItem>(map.Length);
                    foreach (var item in map)
                    {
                        if (_query != null)
                        {
                            var confirms = _query.Constraint.Confirms(map, item.Key);
                            if (confirms)
                            {
                                _children.Add(new FlxValueTreeViewItem(item.Value, _depth+1, this, $"{item.Key} : ", _query.Next));
                            } else if (_query.Optional)
                            {
                                _children.Add(new FlxValueTreeViewItem(item.Value, _depth+1, this, $"{item.Key} : ", _query));
                            }
                        }
                        else
                        {
                            _children.Add(new FlxValueTreeViewItem(item.Value, _depth+1, this, $"{item.Key} : "));
                        }
                    }
                }

                return _children;
            }
            
            return new List<TreeViewItem>();
        }
    }

    public override TreeViewItem parent => _parent;
}