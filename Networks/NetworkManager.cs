using System;
using System.Collections.Generic;

namespace Networks
{
    internal enum Networks
    {
        WaterPipe,
        Sewer,
        PowerCable,
        CommunicationCable,
        HeatingNetworks,
        GasPipe
    }

    internal enum Buildings
    {
        BuildingsFoundation,
        StreetSideStone,
        ExternalEdge,
        HvlSupportsFoundation1,
        HvlSupportsFoundation35,
        HvlSupportsFoundationOver,
        RedLine
    }

    internal static class NetworkManager
    {
        private static int _gasPipeType = 0;

        /// <summary>
        /// Матрица расстояний между коммункациями
        /// Индексы соответствуют перечислению Networks
        /// </summary>
        private static readonly double[,] Distances =
        {
            { 1.0, 1.5, 0.5, 0.5, 1.5 }, // Водопровод
            { 1.5, 0.4, 0.5, 0.5, 1.0 }, // Канализация
            { 0.5, 0.5, 0.1, 0.5, 2.0 }, // Кабели силовые
            { 0.5, 0.5, 0.5, 0.0, 1.0 }, // Кабели связи
            { 1.5, 1.0, 2.0, 1.0, 0.0 }, // Теплосети от наружной стенки
        };

        /// <summary>
        /// Матрица расстояний от коммуникаций до зданий
        /// </summary>
        private static readonly double[,] DistancesToBuildings =
        {
            //фунд  бк  нбр  вл1  вл35 вл>  кр.лин
            { 5.0, 2.0, 1.0, 1.0, 2.0, 3.0, 0.1 }, // Водопровод и напорная канализация 
            { 2.0, 1.5, 1.0, 1.0, 2.0, 3.0, 0.1 }, // Теплосети от наружной стенки
            { 0.6, 1.5, 1.0, 0.5, 5.0, 10, 0.1 }, // Кабели силовые и связи
        };

        private static readonly double[,] DistancesToGasPipe =
        {
            { 1.0, 1.0, 1.5, 2.0 }, // Водопровод
            { 1.0, 1.5, 2.0, 5.0 }, // Канализация
            { 1.0, 1.0, 1.0, 2.0 }, // Силовые кабели
            { 1.0, 1.0, 1.0, 1.0 }, // Связь
            { 0.2, 2.0, 2.0, 4.0 }, // Теплосети
            { 0.4, 0.4, 0.4, 0.4 }, // Газопровод
            { 2.0, 4.0, 7.0, 10.0 }, // Фундамент
            { 1.5, 1.5, 2.5, 2.5 }, // Бортовой камень
            { 1.0, 1.0, 1.0, 1.0 }, // Наружная бровка
            { 1.0, 1.0, 1.0, 1.0 }, // Опора влэп 1
            { 1.0, 1.0, 1.0, 1.0 }, // Опора влэп 35
            { 1.0, 1.0, 1.0, 1.0 }, // Опора влэп >
            { 0.1, 0.1, 0.1, 0.1 }, // Красная линия
        };

        /// <summary>
        /// Словарь названий слоев сетей
        /// </summary>
        private static readonly Dictionary<Networks, string> LayerNames = new Dictionary<Networks, string>();

        /// <summary>
        /// Словарь сетей по названию слоя
        /// </summary>
        private static readonly Dictionary<string, Networks> NetworksMap = new Dictionary<string, Networks>();

        private static readonly Dictionary<string, Buildings> BuildingsMap = new Dictionary<string, Buildings>();

        /// <summary>
        /// Получение допустимого расстояния между двумя типами коммуникаций
        /// </summary>
        public static double GetDistance(Networks firstNetwork, Networks secondNetwork)
        {
            if (firstNetwork == Networks.GasPipe)
                return DistancesToGasPipe[(int)secondNetwork, _gasPipeType];
            if (secondNetwork == Networks.GasPipe)
                return DistancesToGasPipe[(int)firstNetwork, _gasPipeType];
            return Distances[(int)firstNetwork, (int)secondNetwork];
        }

        /// <summary>
        /// Получение допустимого расстояния между коммуникацией и зданием
        /// </summary>
        public static double GetDistanceToBuilding(Networks network, Buildings building)
        {
            if (network == Networks.GasPipe)
            {
                return DistancesToGasPipe[(int)building + 6, _gasPipeType];
            }

            int nw;
            switch (network)
            {
                case Networks.WaterPipe:
                case Networks.Sewer:
                    nw = 0;
                    break;
                case Networks.HeatingNetworks:
                    nw = 1;
                    break;
                case Networks.PowerCable:
                case Networks.CommunicationCable:
                    nw = 2;
                    break;
                case Networks.GasPipe:
                default:
                    throw new Exception("Неизвесная сеть");
            }

            return DistancesToBuildings[nw, (int)building];
        }

        /// <summary>
        /// Задание типа труб водопровода
        /// </summary>
        /// <remarks>
        /// <para> 
        /// Чтобы учесть Прим. 2 Таблицы 12.6 СП 42.13330.2016
        /// </para>
        /// </remarks>
        /// <param name="type">Название типа трубы</param>
        /// <param name="size">Размер трубы</param>
        public static void SetPipeType(string type, double size)
        {
            double distance;
            switch (type)
            {
                case "Железобетонные и асбестоцементные трубы":
                    distance = 5;
                    break;
                case "Чугунные трубы":
                    if (size > 200)
                        distance = 3;
                    else
                        distance = 1.5;
                    break;
                case "Пластмассовые трубы":
                    distance = 1.5;
                    break;
                default:
                    distance = 1.5;
                    break;
            }

            Distances[0, 1] = distance;
            Distances[1, 0] = distance;
        }

        public static void SetGasPipePressure(double pressure)
        {
            if (pressure < 0.005)
                _gasPipeType = 0;
            else if (pressure < 0.3)
                _gasPipeType = 1;
            else if (pressure < 0.6)
                _gasPipeType = 2;
            else
                _gasPipeType = 3;
        }

        /// <summary>
        /// Получение названия коммуникации по ее типу
        /// </summary>
        public static string GetNetworkName(Networks network)
        {
            if (LayerNames.ContainsKey(network))
                return LayerNames[network];
            return "0";
        }

        /// <summary>
        /// Получение типа коммуникации по ее названию
        /// </summary>
        public static Networks GetType(string typeName)
        {
            if (NetworksMap.ContainsKey(typeName))
                return NetworksMap[typeName];
            throw new Exception("Ошибка слоя");
        }

        public static Buildings GetBuildingType(string typeName)
        {
            if (BuildingsMap.ContainsKey(typeName))
                return BuildingsMap[typeName];
            throw new Exception("Ошибка слоя");
        }

        /// <summary>
        /// Установка названий слоев
        /// </summary>
        public static void SetLayers()
        {
            LayerNames.Clear();
            LayerNames[Networks.WaterPipe] = Properties.Settings.Default.WaterPipeLayerName;
            LayerNames[Networks.Sewer] = Properties.Settings.Default.SewerLayerName;
            LayerNames[Networks.HeatingNetworks] = Properties.Settings.Default.HeatingNetworkLayerName;
            LayerNames[Networks.CommunicationCable] = Properties.Settings.Default.CommunicationCableLayerName;
            LayerNames[Networks.PowerCable] = Properties.Settings.Default.PowerCableLayerName;
            LayerNames[Networks.GasPipe] = Properties.Settings.Default.GasPipeLayerName;

            NetworksMap.Clear();
            NetworksMap[Properties.Settings.Default.WaterPipeLayerName] = Networks.WaterPipe;
            NetworksMap[Properties.Settings.Default.SewerLayerName] = Networks.Sewer;
            NetworksMap[Properties.Settings.Default.HeatingNetworkLayerName] = Networks.HeatingNetworks;
            NetworksMap[Properties.Settings.Default.CommunicationCableLayerName] = Networks.CommunicationCable;
            NetworksMap[Properties.Settings.Default.PowerCableLayerName] = Networks.PowerCable;
            NetworksMap[Properties.Settings.Default.GasPipeLayerName] = Networks.GasPipe;

            BuildingsMap.Clear();
            BuildingsMap[Properties.Settings.Default.BuildingsFoundationLayerName] = Buildings.BuildingsFoundation;
            BuildingsMap[Properties.Settings.Default.StreetSideStoneLayerName] = Buildings.StreetSideStone;
            BuildingsMap[Properties.Settings.Default.ExternalEdgeLayerName] = Buildings.ExternalEdge;
            BuildingsMap[Properties.Settings.Default.HvlSupportsFoundation1LayerName] =
                Buildings.HvlSupportsFoundation1;
            BuildingsMap[Properties.Settings.Default.HvlSupportsFoundation35LayerName] =
                Buildings.HvlSupportsFoundation35;
            BuildingsMap[Properties.Settings.Default.HvlSupportsFoundationOverLayerName] =
                Buildings.HvlSupportsFoundationOver;
            BuildingsMap[Properties.Settings.Default.RedLineLayerName] = Buildings.RedLine;
        }
    }
}