/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2022 KinematicSoup Technologies Incorporated 
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
using UnityEngine;

namespace KS.Reactor.Client.Unity
{
    /// <summary>
    /// Wraps a <see cref="ksClientInputPredictor"/>, allowing it to be attached to a game object for an entity or room
    /// type.
    /// </summary>
    public class ksClientInputPredictorComponent : ksPredictorWrapper<ksClientInputPredictor>, ksICloneableScript
    {
        /// <summary>Does the predictor require a player controller?</summary>
        public override bool RequiresController
        {
            get { return true; }
        }

        /// <summary>Creates the predictor.</summary>
        private void Awake()
        {
            m_predictor = ksClientInputPredictor.Create();
        }

        /// <summary>Copies the values of this script onto <paramref name="script"/>.</summary>
        /// <param name="script">Script to copy to.</param>
        public void CopyTo(Component script)
        {

        }
    }
}
