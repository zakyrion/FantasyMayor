using Domains.Economy.DistrictBuildCost.Data;
using Friflo.Engine.ECS;
using System;

namespace Domains.Economy.DistrictBuildCost.Components
{
    public struct DistrictBuildCostResourcePriceComponent : IComponent
    {
        public const int Capacity = 7;

        public ResourceCost Price1;
        public ResourceCost Price2;
        public ResourceCost Price3;
        public ResourceCost Price4;
        public ResourceCost Price5;
        public ResourceCost Price6;
        public ResourceCost Price7;

        public int Count;

        public ResourceCost this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Price1;
                    case 1: return Price2;
                    case 2: return Price3;
                    case 3: return Price4;
                    case 4: return Price5;
                    case 5: return Price6;
                    case 6: return Price7;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
            set
            {
                switch (index)
                {
                    case 0: Price1 = value; break;
                    case 1: Price2 = value; break;
                    case 2: Price3 = value; break;
                    case 3: Price4 = value; break;
                    case 4: Price5 = value; break;
                    case 5: Price6 = value; break;
                    case 6: Price7 = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }
    }
}
