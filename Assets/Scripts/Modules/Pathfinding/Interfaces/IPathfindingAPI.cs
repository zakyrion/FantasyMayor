using System.Collections.Generic;
using Unity.Mathematics;

public interface IPathfindingAPI : IAPI
{
    List<int2> GetPath(int2 start, int2 end);
}