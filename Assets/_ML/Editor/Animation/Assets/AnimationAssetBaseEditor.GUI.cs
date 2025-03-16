using Animancer;
using Animancer.Editor;
using ML.Engine.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using static ML.Engine.Animation.IAssetHasEvents;

namespace ML.Editor.Animation
{
    public partial class AnimationAssetBaseEditor : UnityEditor.Editor
    {

        public static float AnimFloatField(string label, float normalized, float length, float frameRate, ref bool showAnimFloatField, float rangemin = 0, float rangemax = 1, System.Action ElseDrawGUI = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showAnimFloatField = EditorGUILayout.Foldout(showAnimFloatField, label);
            if (showAnimFloatField)
            {
                EditorGUI.indentLevel++;
                // 规范值 [0,1]，带x后缀
                normalized = EditorGUILayout.FloatField(new GUIContent("规范化时间", "[0, 1]范围内的时间"), normalized);
                //GUILayout.Label("x");

                EditorGUI.BeginChangeCheck();
                // 实际长度值，带s后缀
                float actualLength = normalized * length;
                actualLength = EditorGUILayout.FloatField(new GUIContent("秒数时间", "以s为单位的时间"), actualLength);
                //GUILayout.Label("s");
                if (EditorGUI.EndChangeCheck())
                {
                    normalized = actualLength / length;
                }

                EditorGUI.BeginChangeCheck();
                // 帧数值，带f后缀
                float frameCount = actualLength * frameRate;
                frameCount = EditorGUILayout.FloatField(new GUIContent("帧时间", "以帧为单位的时间"), frameCount);
                //GUILayout.Label("f");
                if (EditorGUI.EndChangeCheck())
                {
                    normalized = frameCount / length / frameRate;
                }


                ElseDrawGUI?.Invoke();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            normalized = Mathf.Clamp(normalized, rangemin, rangemax);

            return normalized;
        }

        public static float AnimFloatField(string label, float normalized, float length, float frameRate, ref bool bIsEnable, string enableToolTip, float defaultValue, ref bool showAnimFloatField, float rangemin = 0, float rangemax = 1, System.Action ElseDrawGUI = null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool bIsNan = float.IsNaN(normalized);
            if (bIsNan)
            {
                normalized = defaultValue;
            }
            showAnimFloatField = EditorGUILayout.Foldout(showAnimFloatField, label);
            if (showAnimFloatField)
            {
                EditorGUI.indentLevel++;
                bIsEnable = EditorGUILayout.Toggle(new GUIContent("是否启用", enableToolTip), bIsEnable);

                // 规范值 [0,1]，带x后缀
                normalized = EditorGUILayout.FloatField(new GUIContent("规范化时间", "[0, 1]范围内的时间"), normalized);
                //GUILayout.Label("x");

                EditorGUI.BeginChangeCheck();
                // 实际长度值，带s后缀
                float actualLength = normalized * length;
                actualLength = EditorGUILayout.FloatField(new GUIContent("秒数时间", "以s为单位的时间"), actualLength);
                //GUILayout.Label("s");
                if (EditorGUI.EndChangeCheck())
                {
                    normalized = actualLength / length;
                }

                EditorGUI.BeginChangeCheck();
                // 帧数值，带f后缀
                float frameCount = actualLength * frameRate;
                frameCount = EditorGUILayout.FloatField(new GUIContent("帧时间", "以帧为单位的时间"), frameCount);
                //GUILayout.Label("f");
                if (EditorGUI.EndChangeCheck())
                {
                    normalized = frameCount / length / frameRate;
                }

                ElseDrawGUI?.Invoke();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            normalized = Mathf.Clamp(normalized, rangemin, rangemax);
            if (bIsNan)
            {
                if (normalized != defaultValue)
                {
                    bIsEnable = true;
                }
                else
                {
                    normalized = float.NaN;
                }
            }
            return normalized;
        }

        public static void DoSpeedGUI(ITransitionDetailed transition)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            bool enable = EditorGUILayout.Toggle(!float.IsNaN(transition.Speed));
            if (EditorGUI.EndChangeCheck())
            {
                if (!enable)
                {
                    transition.Speed = float.NaN;
                }
                else
                {
                    transition.Speed = 1;
                }
            }

            EditorGUI.BeginChangeCheck();
            var tmp = EditorGUILayout.FloatField("播放速度", float.IsNaN(transition.Speed) ? 1 : transition.Speed);
            if (EditorGUI.EndChangeCheck())
            {
                transition.Speed = tmp;
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 返回以秒为单位的过渡时间
        /// </summary>
        /// <param name="transition"></param>
        /// <param name="length"></param>
        /// <param name="frameRate"></param>
        /// <param name="bShowFoldOut"></param>
        /// <returns></returns>
        public static float DoFadeDurationGUI(ITransition transition, float length, float frameRate, ref bool bShowFoldOut)
        {
            float fadenormalize = transition.FadeDuration / length;
            return AnimFloatField($"过渡时间({transition.FadeDuration}s)", fadenormalize, length, frameRate, ref bShowFoldOut, 0, float.PositiveInfinity) * length;
        }

        /// <summary>
        /// 规范化的开始时间
        /// </summary>
        /// <param name="transition"></param>
        /// <returns></returns>
        public static void DoStartTimeGUI(ITransitionDetailed transition, float length, float frameRate, float defaultStartValue, ref bool bShowFoldOut)
        {
            float NStartTime = transition.NormalizedStartTime;
            bool enableStart = transition.FadeMode == Animancer.FadeMode.FromStart;
            var tmp = AnimFloatField($"开始时间({(float.IsNaN(NStartTime) ? defaultStartValue : NStartTime) * length}s)", NStartTime, length, frameRate, ref enableStart, "启用时使用FromStart，每次从设置的时间开始\n禁用时，使用FixedSpeed，从当前时间继续运行", defaultStartValue, ref bShowFoldOut, float.NegativeInfinity, float.PositiveInfinity);
            if (enableStart != (transition.FadeMode == Animancer.FadeMode.FromStart))
            {
                if (!enableStart)
                {
                    tmp = float.NaN;
                }
                else
                {
                    tmp = defaultStartValue;
                }
            }
            if (tmp != NStartTime)
            {
                transition.NormalizedStartTime = tmp;
            }
        }

        /// <summary>
        /// Property 必须是IAssetHasEvents.AssetEvent类型
        /// </summary>
        /// <param name="property"></param>
        /// <param name="length"></param>
        /// <param name="frameRate"></param>
        /// <param name="bShowFoldOut"></param>
        /// <returns></returns>
        public static void DoEndTimeGUI(SerializedProperty property, float length, float frameRate, float defaultEndValue, ref bool bShowFoldOut)
        {
            var endevent = property.GetValue<IAssetHasEvents.AssetEvent>();
            float NEndTime = endevent.NormalizedTime;
            bool enableEnd = !float.IsNaN(NEndTime);
            EditorGUI.BeginChangeCheck();
            var tmp = AnimFloatField($"结束时间({(float.IsNaN(NEndTime) ? defaultEndValue : NEndTime) * length}s)", NEndTime, length, frameRate, ref enableEnd, "禁用时\n速度>=0，则End在1\nSpeed<0，则End在0", defaultEndValue, ref bShowFoldOut, float.NegativeInfinity, float.PositiveInfinity, () =>
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative("UnityEvents"), new GUIContent("结束事件"));
            });
            if (EditorGUI.EndChangeCheck())
            {
                // 改变了数值
                if (!float.IsNaN(NEndTime) && tmp != NEndTime)
                {
                    enableEnd = true;
                }
                // 改变了启用状态
                else// if(enableEnd == float.IsNaN(NEndTime))
                {
                    if (!enableEnd)
                    {
                        tmp = float.NaN;
                    }
                    else
                    {
                        tmp = defaultEndValue;
                    }
                }
                endevent.NormalizedTime = tmp;
            }
        }

        // 帧率选项
        private static float[] frameRates = { 24, 25, 30, 50, 60 };
        public static void DoClipFrameGUI(AnimationClip clip)
        {
            int i = Array.IndexOf(frameRates, clip.frameRate);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("帧率", GUILayout.Width(100));
            var selectedFrameRateIndex = EditorGUILayout.Popup(i, Array.ConvertAll(frameRates, rate => rate.ToString()));
            EditorGUILayout.EndHorizontal();
            if (i != selectedFrameRateIndex)
            {
                clip.frameRate = frameRates[selectedFrameRateIndex];
            }
        }

        public static void DoAnimTimelineGUI(ITransitionDetailed transition, float endNormalizedTime, float length, float frameRate)
        {
            // Speed >= 0 正向绘制
            // Speed < 0 反向绘制
            float speed = float.IsNaN(transition.Speed) ? 1 : transition.Speed;
            bool isReverse = speed < 0 ? true : false;

            // 总轴 -> [TimeStart, TimeEnd]
            // TimeStart = Start
            float start = (float.IsNaN(transition.NormalizedStartTime) ? (isReverse ? 1 : 0) : transition.NormalizedStartTime) * length;
            float timeStart = start;
            // End >= ClipEnd -> TimeEnd = End+Fade*Speed
            // End < ClipEnd -> TimeEnd = ClipEnd
            float end = (float.IsNaN(endNormalizedTime) ? (isReverse ? 0 : 1) : endNormalizedTime) * length;
            float timeEnd = (end <= 0 || end >= length) ? end + transition.FadeDuration * speed : (isReverse ? 0 : length);
            var area = EditorGUILayout.GetControlRect();
            //var color = new Color(70 / 256f, 89 / 256f, 153 / 256f, 1);
            var color = new Color(0.1f, 1f, 1f, 0.4f);

            float scale = area.width / Mathf.Abs(timeEnd - timeStart);
            //float scale = area.width / (timeEnd - timeStart);
            // 过渡时间 -> [TimeStart, TimeStart+Fade*Speed]
            float fadeStart = timeStart;
            float fadeEnd = timeStart + transition.FadeDuration * speed;
            var fadeSP = new Vector2(fadeStart * scale + area.x, area.y);
            var fadeEPT = new Vector2(fadeEnd * scale + area.x, area.y);
            var fadeEPB = new Vector2(fadeEnd * scale + area.x, area.y + area.height);
            // 开始时间 -> [TimeStart+Fade*Speed, End]

            // 结束时间 -> [End, TimeEnd]
            var endSPT = new Vector2(end * scale + area.x, area.y);
            var endSPB = new Vector2(end * scale + area.x, area.y + area.height);
            var endEP = new Vector2(timeEnd * scale + area.x, area.y + area.height);

            // Clip 时间 -> [0, ClipEnd]
            var clipSP = new Vector2(0 * scale + area.x, area.y + area.height * 0.75f);
            var clipEP = new Vector2(length * scale + area.x, area.y + area.height * 0.75f);

            Vector3[] vertices = { fadeSP, fadeEPB, endEP, endSPT };
            //Debug.Log($"{fadeSP} {fadeEPB} {endEP} {endSPT}");

            //Debug.Log($"Pre {vertices[0]} {vertices[1]} {vertices[2]} {vertices[3]}");
            float dis = 0;
            for (int i = 0; i < vertices.Length; ++i)
            {
                for (int j = 0; j < vertices.Length; ++j)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    float tmp = Vector3.Distance(vertices[i], vertices[j]);
                    if (tmp > dis)
                    {
                        dis = tmp;
                    }
                }
            }
            // 长度规约
            // (x - area.x) / scale * (area.width / vertives.distance) + area.x
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i].x *= (area.width / dis);
            }
            // 左对齐
            float minX = float.PositiveInfinity;
            foreach (var v in vertices)
            {
                if (minX > v.x)
                {
                    minX = v.x;
                }
            }
            minX -= area.x;
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i].x = vertices[i].x - minX;
            }

            // 计算所有点的中心点
            Vector3 center = new Vector3(
                vertices.Average(v => v.x),
                vertices.Average(v => v.y),
                vertices.Average(v => v.z)
            );

            // 根据相对于中心点的角度进行排序
            vertices = vertices.OrderBy(v => Mathf.Atan2(v.y - center.y, v.x - center.x)).ToArray();
            //Debug.Log($"Post {vertices[0]} {vertices[1]} {vertices[2]} {vertices[3]}");

            // 绘制时间轴
            DrawAAConvexGUI(vertices, color);

            // 绘制Clip轴
            clipSP.x = clipSP.x * (area.width / dis) - minX;
            clipEP.x = clipEP.x * (area.width / dis) - minX;
            EditorGUI.DrawRect(new Rect(clipSP.x, clipSP.y, clipEP.x - clipSP.x, area.height * 0.25f), new Color(0.5f, 0.5f, 0.5f, 1));



            // 绘制刻度线
            // 0-Clip
            clipSP.y = area.y + area.height;
            DrawRectLine(clipSP, area.height, "0", Color.white, area);
            // Length-Clip
            clipEP.y = area.y + area.height;
            DrawRectLine(clipEP, area.height, length.ToString("F2"), Color.white, area);
            // Start
            fadeSP.x = fadeSP.x * (area.width / dis) - minX;
            fadeSP.y = area.y + area.height;
            DrawRectLine(fadeSP, area.height, fadeStart.ToString("F2"), Color.white, area);
            // Start-Fade
            fadeEPB.x = fadeEPB.x * (area.width / dis) - minX;
            fadeEPB.y = area.y + area.height;
            DrawRectLine(fadeEPB, area.height, fadeEnd.ToString("F2"), Color.white, area);
            // End-Fade
            endSPB.x = endSPB.x * (area.width / dis) - minX;
            endSPB.y = area.y + area.height;
            DrawRectLine(endSPB, area.height, end.ToString("F2"), Color.white, area);
        }

        /// <summary>
        /// 绘制三角形
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="color"></param>
        public static void DrawAAConvexGUI(Vector3[] vertices, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;

            // 绘制三角形
            Handles.DrawAAConvexPolygon(vertices);

            Handles.EndGUI();
        }

        public static void DrawRectLine(Vector2 point, float length, string text, Color color, Rect area)
        {
            point.y -= length;
            Rect rect = new Rect(point, new Vector2(1, length));
            Rect tRect = new Rect(point.x + 1, point.y - length * 0.5f, 40, 10);

            // 检查并调整rect
            if (rect.xMax > area.xMax)
            {
                rect.x = area.xMax - rect.width;
            }
            if (rect.yMax > area.yMax)
            {
                rect.y = area.yMax - rect.height;
            }
            if (rect.xMin < area.xMin)
            {
                rect.x = area.xMin;
            }
            if (rect.yMin < area.yMin)
            {
                rect.y = area.yMin;
            }
            EditorGUI.DrawRect(rect, color);

            // 检查并调整tRect
            if (tRect.xMax > area.xMax)
            {
                tRect.x = area.xMax - tRect.width;
            }
            if (tRect.yMax > area.yMax)
            {
                tRect.y = area.yMax - tRect.height;
            }
            if (tRect.xMin < area.xMin)
            {
                tRect.x = area.xMin;
            }
            if (tRect.yMin < area.yMin)
            {
                tRect.y = area.yMin;
            }
            EditorGUI.LabelField(tRect, text);
        }

    }
}