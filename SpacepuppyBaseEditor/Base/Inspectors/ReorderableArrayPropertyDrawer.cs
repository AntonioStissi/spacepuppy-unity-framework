﻿using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.Linq;

using com.spacepuppy;
using com.spacepuppy.Utils;

using com.spacepuppyeditor.Internal;

namespace com.spacepuppyeditor.Base
{

    [CustomPropertyDrawer(typeof(ReorderableArrayAttribute))]
    public class ReorderableArrayPropertyDrawer : PropertyDrawer, IArrayHandlingPropertyDrawer
    {

        #region Fields

        public string Label;
        private ReorderableList _lst;
        private GUIContent _label;
        private bool _disallowFoldout;
        private bool _removeBackgroundWhenCollapsed;
        private bool _draggable = true;

        private PropertyDrawer _internalDrawer;

        #endregion

        #region CONSTRUCTOR

        private CachedReorderableList GetList(SerializedProperty property, GUIContent label)
        {
            var lst = CachedReorderableList.GetListDrawer(property, _maskList_DrawHeader, _maskList_DrawElement);
            lst.draggable = _draggable;

            if(property.arraySize > 0)
            {
                var pchild = property.GetArrayElementAtIndex(0);
                if (_internalDrawer != null)
                {
                    lst.elementHeight = _internalDrawer.GetPropertyHeight(pchild, label);
                }
                else
                {
                    lst.elementHeight = SPEditorGUI.GetDefaultPropertyHeight(pchild, label) + 1;
                }
            }
            else
            {
                lst.elementHeight = EditorGUIUtility.singleLineHeight;
            }

            return lst;
        }

        private void StartOnGUI(SerializedProperty property, GUIContent label)
        {
            _lst = this.GetList(property, label);
            if (_lst.index >= _lst.count) _lst.index = -1;

            var attrib = this.attribute as ReorderableArrayAttribute;
            if (attrib != null)
            {
                _disallowFoldout = attrib.DisallowFoldout;
                _removeBackgroundWhenCollapsed = attrib.RemoveBackgroundWhenCollapsed;
                _draggable = attrib.Draggable;
            }
            else
            {
                _disallowFoldout = false;
                _removeBackgroundWhenCollapsed = false;
                _draggable = true;
            }

            _label = label;
        }
        
        private void EndOnGUI(SerializedProperty property, GUIContent label)
        {
            _lst.serializedProperty = null;
            _label = null;
        }

        #endregion

        #region Properties

        

        #endregion

        #region OnGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float h;
            if (EditorHelper.AssertMultiObjectEditingNotSupportedHeight(property, label, out h)) return h;

            if (property.isArray)
            {
                if (_disallowFoldout || property.isExpanded)
                {
                    return this.GetList(property, label).GetHeight();
                }
                else
                {
                    return EditorGUIUtility.singleLineHeight;
                }
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (EditorHelper.AssertMultiObjectEditingNotSupported(position, property, label)) return;

            if (property.isArray)
            {
                if(_disallowFoldout)
                {
                    this.StartOnGUI(property, label);
                    _lst.DoList(EditorGUI.IndentedRect(position));
                    this.EndOnGUI(property, label);
                }
                else
                {
                    const float WIDTH_FOLDOUT = 5f;
                    if (property.isExpanded)
                    {
                        this.StartOnGUI(property, label);
                        property.isExpanded = EditorGUI.Foldout(new Rect(position.xMin, position.yMin, WIDTH_FOLDOUT, EditorGUIUtility.singleLineHeight), property.isExpanded, GUIContent.none);
                        _lst.DoList(EditorGUI.IndentedRect(position));
                        this.EndOnGUI(property, label);
                    }
                    else
                    {
                        if(_removeBackgroundWhenCollapsed)
                        {
                            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
                        }
                        else
                        {
                            property.isExpanded = EditorGUI.Foldout(new Rect(position.xMin, position.yMin, WIDTH_FOLDOUT, EditorGUIUtility.singleLineHeight), property.isExpanded, GUIContent.none);
                            ReorderableListHelper.DrawRetractedHeader(EditorGUI.IndentedRect(position), label);
                        }
                    }
                }

            }
            else
            {
                EditorGUI.PropertyField(position, property, label, false);
            }
        }

        #endregion

        #region Masks ReorderableList Handlers

        private void _maskList_DrawHeader(Rect area)
        {
            if(this.Label != null)
            {
                EditorGUI.LabelField(area, this.Label ?? _lst.serializedProperty.displayName);
            }
            else
            {
                EditorGUI.LabelField(area, _label ?? EditorHelper.TempContent(_lst.serializedProperty.displayName));
            }
        }

        private void _maskList_DrawElement(Rect area, int index, bool isActive, bool isFocused)
        {
            var element = _lst.serializedProperty.GetArrayElementAtIndex(index);
            if (element == null) return;

            //EditorGUI.PropertyField(area, element, GUIContent.none, false);
            var attrib = this.attribute as ReorderableArrayAttribute;
            GUIContent label = GUIContent.none;
            if(attrib != null)
            {
                if (attrib.ElementLabelFormatString != null)
                {
                    label = EditorHelper.TempContent(string.Format(attrib.ElementLabelFormatString, index));
                }
                if(attrib.ElementPadding > 0f)
                {
                    area = new Rect(area.xMin + attrib.ElementPadding, area.yMin, Mathf.Max(0f, area.width - attrib.ElementPadding), area.height);
                }
            }

            if(_internalDrawer != null)
            {
                _internalDrawer.OnGUI(area, element, label);
            }
            else
            {
                SPEditorGUI.DefaultPropertyField(area, element, label);
            }

            if (GUI.enabled) ReorderableListHelper.DrawDraggableElementDeleteContextMenu(_lst, area, index, isActive, isFocused);
        }

        #endregion




        #region IArrayHandlingPropertyDrawer Interface

        PropertyDrawer IArrayHandlingPropertyDrawer.InternalDrawer
        {
            get
            {
                return _internalDrawer;
            }
            set
            {
                _internalDrawer = value;
            }
        }

        #endregion
        
    }
}
