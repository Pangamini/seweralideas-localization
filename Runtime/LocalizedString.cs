using System;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    [Serializable]
    public struct LocalizedString
    {
        [SerializeField] private string m_key;
        public string Key => m_key;
    }
}
