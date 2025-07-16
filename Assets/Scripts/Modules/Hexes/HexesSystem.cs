using System.Collections.Generic;
using Atoms.Hexes.DataLayer;
using Atoms.Hexes.DataTypes;
using Core.DataLayer;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

namespace Atoms.Hexes
{
    [UsedImplicitly]
    public class HexesSystem : IHexesAPI
    {
        private readonly HexViewDataLayer _hexDataLayer;
        private readonly HexMonoFactory _hexMonoFactory;
        private readonly TerrainLevelGenerator _levelGeneratorService;
        private readonly SpotGenerator _spotGenerator;
        private readonly TerrainGeneratorSettingsScriptable _terrainGeneratorSettings;
        private IDataContainer<HeightmapDataLayer> _dataContainer;
        private HeightmapDataLayer _heightmapDataLayer;

        [Inject]
        private HexesSystem(HexMonoFactory hexMonoFactory, HexViewDataLayer hexDataLayer,
            TerrainGeneratorSettingsScriptable terrainGeneratorSettings, SpotGenerator spotGenerator,
            TerrainLevelGenerator levelGeneratorService, HeightmapDataLayer heightmapDataLayer)
        {
            _hexMonoFactory = hexMonoFactory;
            _hexDataLayer = hexDataLayer;
            _terrainGeneratorSettings = terrainGeneratorSettings;
            _spotGenerator = spotGenerator;
            _levelGeneratorService = levelGeneratorService;
            _heightmapDataLayer = heightmapDataLayer;
        }

        public List<HexViewData> SelectHexDataByLevel(HexViewData specificHexData, SelectionType selectionType)
        {
            var selectedHexesData = new List<HexViewData>();
            var visited = new HashSet<HexViewData>();
            var queue = new Queue<HexViewData>();

            queue.Enqueue(specificHexData);
            visited.Add(specificHexData);

            while (queue.Count > 0)
            {
                var hexToCheck = queue.Dequeue();

                var needToAdd = selectionType switch
                {
                    SelectionType.SameLevel => hexToCheck.Level.Value == specificHexData.Level.Value,
                    SelectionType.SameAndHigher => hexToCheck.Level.Value <= specificHexData.Level.Value,
                    SelectionType.SameAndLowest => hexToCheck.Level.Value >= specificHexData.Level.Value
                };

                if (needToAdd)
                    selectedHexesData.Add(hexToCheck);

                foreach (var neighbor in hexToCheck.Neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }

            return selectedHexesData;
        }

        public async void SetHexLevel(HexId hexId, int level)
        {
            Debug.Log("[skh] call");
            var hexData = _hexDataLayer.GetHex(hexId);
            hexData.SetLevel(level);

            //var shape = SelectHexDataByLevel(hexData, SelectionType.SameAndHigher).Select(h=> h.Position).ToList();

            //await _levelGeneratorService.Generate(shape);

            //TODO: взяти гекс, залити його одним кольором

            //_heightmapDataLayer.SetTexture(new Texture2D(128, 128, TextureFormat.R8, false));

            //await smoothCommand.Execute();
            //toMeshCommand.Execute();

            //var handler = RebuildMesh(hexData).Schedule();

            /*foreach (var neighbor in hexData.Neighbors)
        {
            handler = RebuildMesh(neighbor).Schedule(handler);
        }

        await handler.ToUniTask(PlayerLoopTiming.Update);
        handler.Complete();*/


            //спробувати наступне:
            //будуємо маску від найвищих гексів до найнижчих
            //кожен гекс має скількись пікселів на мапі, або ж можемо по мапі на кожен взяти з конкретним розміром
            //можна маска будується наступним чином. Зона в маленькому кордоні - контанта і може бути задекорована плоским шумом для утворення нерівностей
            //зона між маленьким кордоном і до нового кордону який повинен простягатися до малих кордонів сусідніх гексів - спуск до 0. Таким чином будемо мати різний кіт нахилу
            //цю маску можна буде декорувати більш декоративним шумом для переходу
            //нижчі зони можна буде декорувати 

            //Спробувати наступне:
            //будемо проходитись шар за шаром від найнижчих гексів до найвижчих
            //по малому кордону висота буде константою і буде спускатись до 0 до великого кордону на константну висоту
            //будемо включати всі гекси одного рівня або вижче в одну область
            //потім будемо будувати для цієї області мапу, декорувати її шумами
            //потім будемо рухатись вгору далі і далі. 
            //максимальний перепад рівнів висоти 2 рівні. Якщо перепад більше, то сусідні гекси будуть автоматично підняті  до рівня x-2 і так далі
        }

        private void BuildLevelMesh(NativeList<int2> hexesIds)
        {
            var vertices = new List<Vector3>();
        }

        void IHexesAPI.CreateAllHexes()
        {

        }

        UniTask<bool> IHexesAPI.CreateHex(HexViewData hexData, int detailLevel)
        {
            return _hexMonoFactory.SpawnHex(hexData, detailLevel);
        }

        public enum SelectionType
        {
            SameLevel,
            SameAndHigher,
            SameAndLowest
        }
    }
}
