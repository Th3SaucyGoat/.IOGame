/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2021 KinematicSoup Technologies Incorporated 
 All Rights Reserved.

NOTICE:  All information contained herein is, and remains the property of 
KinematicSoup Technologies Incorporated and its suppliers, if any. The 
intellectual and technical concepts contained herein are proprietary to 
KinematicSoup Technologies Incorporated and its suppliers and may be covered by
U.S. and Foreign Patents, patents in process, and are protected by trade secret
or copyright law. Dissemination of this information or reproduction of this
material is strictly forbidden unless prior written permission is obtained from
KinematicSoup Technologies Incorporated.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

namespace KS.Reactor.Client.Unity.Editor
{
    /// <summary>Custom inspector for <see cref="ksConvergingInputPredictorComponent"/>.</summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ksConvergingInputPredictorComponent))]
    public class ksConvergingInputPredictorEditor : ksPredictorWrapperEditor
    {
        /// <summary>
        /// Creates the property field. If the field is for
        /// <see cref="ksConvergingInputPredictor.ConfigData.VelocityTolerance"/>, draws a toggle for setting the
        /// tolerance to <see cref="ksConvergingInputPredictor.ConfigData.AUTO"/>.
        /// </summary>
        /// <param name="property">Property to draw.</param>
        protected override void DrawProperty(SerializedProperty property)
        {
            if (property.name == "VelocityTolerance")
            {
                if (EditorGUILayout.Toggle("Auto Velocity Tolerance",
                    property.floatValue == ksConvergingInputPredictor.ConfigData.AUTO))
                {
                    property.floatValue = ksConvergingInputPredictor.ConfigData.AUTO;
                    return;
                }
                if (property.floatValue < 0f)
                {
                    property.floatValue = 0f;
                }
            }
            base.DrawProperty(property);
        }
    }
}
