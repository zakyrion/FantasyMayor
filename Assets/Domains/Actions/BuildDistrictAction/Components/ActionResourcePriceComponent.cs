using System;
using Domains.Economy.Resource.Components;

namespace Domains.Actions.BuildDistrictAction.Components
{
    // Zero-allocation snapshot of a district's resource price, copied from BuildDistrictCostConfig.DistrictPrices
    // (a List<ResourceComponent>) onto the committed build-action entity at commit (Step 4). Fixed inline slots —
    // no managed list/array as component data (zero-allocation systems rule). Capacity = 7 = the number of
    // spendable ResourceType members (Grain, Clay, Wood, RawMeat, RawFish, SmokedMeat, SmokedFish; ActionPoint is
    // the separate AP cost carried by ActionAPCostComponent), so a full price always fits. Only the first `Count`
    // slots are meaningful.
    public struct ActionResourcePriceComponent
    {
        public const int Capacity = 7;

        public ResourceComponent Price1;
        public ResourceComponent Price2;
        public ResourceComponent Price3;
        public ResourceComponent Price4;
        public ResourceComponent Price5;
        public ResourceComponent Price6;
        public ResourceComponent Price7;

        public int Count;

        public ResourceComponent this[int index]
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
