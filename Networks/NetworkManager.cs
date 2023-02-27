using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <summary>
        /// Реализация алгоритма Нарайаны для поиска всех перестановок
        /// </summary>
        public static class Narayana
        {
            /// <summary>
            /// Функция, задающая отношение порядка для значений типа T: &lt; либо &gt;
            /// </summary>
            public delegate bool Predicate2<T>(T value0, T value1);

            /// <summary>
            /// Поиск очередной перестановки
            /// </summary>
            public static bool NextPermutation<T>(T[] sequence, Predicate2<T> compare)
            {
                // Этап № 1
                var i = sequence.Length;
                do
                {
                    if (i < 2)
                        return false; // Перебор закончен
                    --i;
                } while (!compare(sequence[i - 1], sequence[i]));

                // Этап № 2
                var j = sequence.Length;
                while (i < j && !compare(sequence[i - 1], sequence[--j]))
                {
                }

                SwapItems(sequence, i - 1, j);
                // Этап № 3
                j = sequence.Length;
                while (i < --j)
                    SwapItems(sequence, i++, j);
                return true;
            }

            /// <summary>
            /// Обмен значениями двух элементов последовательности
            /// </summary>
            private static void SwapItems<T>(T[] sequence, int index0, int index1)
            {
                (sequence[index1], sequence[index0]) = (sequence[index0], sequence[index1]);
            }
        }

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
            //фунд  бк  нбр  вл1  вл35 вл>
            { 5.0, 2.0, 1.0, 1.0, 2.0, 3.0 }, // Водопровод и напорная канализация 
            { 2.0, 1.5, 1.0, 1.0, 2.0, 3.0 }, // Теплосети от наружной стенки
            { 0.6, 1.5, 1.0, 0.5, 5.0, 10 }, // Кабели силовые и связи
        };

        private static readonly double[,] DistancesToGasPipe =
        {
            { 1.0, 1.0, 1.5, 2.0 }, // Водопровод
            { 1.0, 1.5, 2.0, 5.0 }, // Канализация
            { 0.2, 2.0, 2.0, 4.0 }, // Теплосети
            { 0.4, 0.4, 0.4, 0.4 }, // Газопровод
            { 1.0, 1.0, 1.0, 1.0 }, // Силовые кабели
            { 1.0, 1.0, 1.0, 1.0 }, // Связь
            { 1.0, 1.0, 1.0, 1.0 }, // Фундамент
            { 1.0, 1.0, 1.0, 1.0 }, // Бортовой камень
            { 1.0, 1.0, 1.0, 1.0 }, // Наружная бровка
            { 1.0, 1.0, 1.0, 1.0 }, // Опора влэп 1
            { 1.0, 1.0, 1.0, 1.0 }, // Опора влэп 35
            { 1.0, 1.0, 1.0, 1.0 }, // Опора влэп >
        };

        /// <summary>
        /// Словарь названий слоев сетей
        /// </summary>
        private static readonly Dictionary<Networks, string> LayerNames = new Dictionary<Networks, string>();

        /// <summary>
        /// Словарь сетей по названию слоя
        /// </summary>
        private static readonly Dictionary<string, Networks> NetworksMap = new Dictionary<string, Networks>();

        /// <summary>
        /// Получение допустимого расстояния между двумя типами коммуникаций
        /// </summary>
        public static double GetDistance(Networks firstNetwork, Networks secondNetwork)
        {
            int nw1 = 0, nw2 = 0;
            switch (firstNetwork)
            {
                case Networks.WaterPipe:
                    nw1 = 0;
                    break;
                case Networks.Sewer:
                    nw1 = 1;
                    break;
                case Networks.PowerCable:
                    nw1 = 2;
                    break;
                case Networks.CommunicationCable:
                    nw1 = 3;
                    break;
                case Networks.HeatingNetworks:
                    nw1 = 4;
                    break;
            }

            switch (secondNetwork)
            {
                case Networks.WaterPipe:
                    nw2 = 0;
                    break;
                case Networks.Sewer:
                    nw2 = 1;
                    break;
                case Networks.PowerCable:
                    nw2 = 2;
                    break;
                case Networks.CommunicationCable:
                    nw2 = 3;
                    break;
                case Networks.HeatingNetworks:
                    nw2 = 4;
                    break;
            }

            return Distances[nw1, nw2];
        }

        /// <summary>
        /// Получение допустимого расстояния между коммуникацией и зданием
        /// </summary>
        public static double GetDistanceToBuilding(Networks network, Buildings building)
        {
            int nw = 0, bld = 0;
            switch (network)
            {
                case Networks.WaterPipe:
                case Networks.Sewer:
                    nw = 0;
                    break;
                case Networks.PowerCable:
                case Networks.CommunicationCable:
                    nw = 2;
                    break;
                case Networks.HeatingNetworks:
                    nw = 1;
                    break;
            }

            switch (building)
            {
                case Buildings.BuildingsFoundation:
                    bld = 0;
                    break;
                case Buildings.StreetSideStone:
                    bld = 1;
                    break;
                case Buildings.ExternalEdge:
                    bld = 2;
                    break;
                case Buildings.HvlSupportsFoundation1:
                    bld = 3;
                    break;
                case Buildings.HvlSupportsFoundation35:
                    bld = 4;
                    break;
                case Buildings.HvlSupportsFoundationOver:
                    bld = 5;
                    break;
            }

            return DistancesToBuildings[nw, bld];
        }

        public static double GetDistanceToGasPipe(double pressure, Networks network)
        {
            int gasPipeType;
            if (pressure < 0.005)
                gasPipeType = 0;
            else if (pressure < 0.3)
                gasPipeType = 1;
            else if (pressure < 0.6)
                gasPipeType = 2;
            else
                gasPipeType = 3;
            int nw;
            switch (network)
            {
                case Networks.WaterPipe:
                    nw = 0;
                    break;
                case Networks.Sewer:
                    nw = 1;
                    break;
                case Networks.PowerCable:
                    nw = 4;
                    break;
                case Networks.CommunicationCable:
                    nw = 5;
                    break;
                case Networks.HeatingNetworks:
                    nw = 2;
                    break;
                case Networks.GasPipe:
                    nw = 3;
                    break;
                default:
                    throw new Exception("Неизвесная сеть");
            }

            return DistancesToGasPipe[nw, gasPipeType];
        }

        public static double GetDistanceToGasPipe(double pressure, Buildings building)
        {
            int gasPipeType;
            if (pressure < 0.005)
                gasPipeType = 0;
            else if (pressure < 0.3)
                gasPipeType = 1;
            else if (pressure < 0.6)
                gasPipeType = 2;
            else
                gasPipeType = 3;
            int bd;
            switch (building)
            {
                case Buildings.BuildingsFoundation:
                case Buildings.StreetSideStone:
                case Buildings.ExternalEdge:
                case Buildings.HvlSupportsFoundation1:
                case Buildings.HvlSupportsFoundation35:
                case Buildings.HvlSupportsFoundationOver:
                case Buildings.RedLine:
                default:
                    throw new Exception("Неизвесный объект");
            }

            return DistancesToGasPipe[bd, gasPipeType];
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

        /// <summary>
        /// Получение оптимальной перестановки сетей
        /// </summary>
        /// <param name="networks">Масиив сетей перестановку которых необходимо найти</param>
        /// <returns>Возвращает оптимальную перестановку сетей</returns>
        public static Networks[] GetBestPermutation(Networks[] networks)
        {
            var sizes = new[] { 0.0, 0.0, 0.0 };
            Networks[] result = new Networks[networks.Length];
            double best = double.PositiveInfinity;

            do
            {
                double distance = GetDistances(networks, sizes).Sum();
                if (distance < best)
                {
                    best = distance;
                    networks.CopyTo(result, 0);
                }
            } while (Narayana.NextPermutation(networks, (n1, n2) => (int)n1 < (int)n2));

            return result;
        }

        /// <summary>
        /// Получение расстояний между коммуникациями
        /// </summary>
        /// <param name="networks">Массив сетей расстояние между которыми необходимо вычислить</param>
        /// <param name="sizes">Размеры коммуникаций { Водопровод, Канализация, Теплосеть }</param>
        /// <returns>Массив расстояний между коммуникациями</returns>
        public static double[] GetDistances(Networks[] networks, double[] sizes)
        {
            double[] result = new double[networks.Length];
            result[0] = 1;

            // Расстояние между соседними сетями
            for (int i = 0; i < networks.Length - 1; i++)
            {
                result[i + 1] = Distances[(int)networks[i], (int)networks[i + 1]];
            }

            // Расстояние между не соседними сетями
            for (int i = 0; i < networks.Length - 2; i++)
            {
                for (int j = i + 2; j < networks.Length; j++)
                {
                    double s = result.FromToSum(i + 1, j + 1);
                    if (s < Distances[(int)networks[i], (int)networks[j]])
                    {
                        double delta = Distances[(int)networks[i], (int)networks[j]] - s;
                        result[j] += delta;
                    }
                }
            }

            // Размеры коммуникаций
            for (int i = 0; i < networks.Length - 1; i++)
            {
                if (networks[i] == Networks.WaterPipe)
                {
                    result[i] += sizes[0] / 2;
                    result[i + 1] += sizes[0] / 2;
                }

                if (networks[i] == Networks.Sewer)
                {
                    result[i] += sizes[1] / 2;
                    result[i + 1] += sizes[1] / 2;
                }

                if (networks[i] == Networks.HeatingNetworks)
                {
                    result[i] += sizes[2] / 2;
                    result[i + 1] += sizes[2] / 2;
                }
            }

            if (networks[networks.Length - 1] == Networks.WaterPipe)
                result[networks.Length - 1] += sizes[0] / 2;
            if (networks[networks.Length - 1] == Networks.Sewer)
                result[networks.Length - 1] += sizes[1] / 2;
            if (networks[networks.Length - 1] == Networks.HeatingNetworks)
                result[networks.Length - 1] += sizes[2] / 2;

            return result;
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
            throw new Exception();
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
        }
    }
}