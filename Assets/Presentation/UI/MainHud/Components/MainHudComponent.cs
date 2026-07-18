using Core;
using Friflo.Engine.ECS;
using UnityEngine;

namespace Presentation.UI.MainHud.Components
{
    public struct MainHudComponent : IComponent
    {
        public Box<GameObject> RootBox;
    }
}
