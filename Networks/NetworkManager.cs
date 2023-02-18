using System.Collections.Generic;
using System.Linq;

namespace Networks
{
    enum Networks
    {
        WaterPipe,
        HouseholdSewer,
        RainSewer,
        PowerCable,
        CommunicationCable,
        HeatingNetworks,
        ChannelsAndTunnels,
        PneumoWasteChutes
    }

    enum Buildings
    {
        BuildingsFoundation,
        EnterprisesFoundation,
        Railways1520,
        Railways750,
        StreetSideStone,
        ExternalEdge,
        HvlSupportsFoundation1,
        HvlSupportsFoundation35,
        HvlSupportsFoundationOver,
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
        private static readonly double[,] Distances = new double[,]
        {
            { 1, 1.5, 1.5, 0.5, 0.5, 1.5, 1.5, 1.5, 1 }, // Водопровод
            { 1.5, 0.4, 0.4, 0.5, 0.5, 1, 1, 1, 1 }, // Канализация бытовая
            { 1.5, 0.4, 0.4, 0.5, 0.5, 1, 1, 1, 1 }, // Канализация дождевая
            { 0.5, 0.5, 0.5, 0.1, 0.5, 2, 2, 2, 1.5 }, // Кабели силовые
            { 0.5, 0.5, 0.5, 0.5, 0, 1, 1, 1, 1 }, // Кабели связи
            { 1.5, 1, 1, 2, 1, 0, 0, 2, 1 }, // Теплосети от наружной стенки
            { 1.5, 1, 1, 2, 1, 0, 0, 2, 1 }, // Теплосети от оболочки
            { 1.5, 1, 1, 2, 1, 2, 2, 0, 1 }, // Каналы тоннели
            { 1, 1, 1, 1.5, 1, 1, 1, 1, 0 } // Пневмомусоропроводы
        };

        /// <summary>
        /// Матрица расстояний от коммуникаций до зданий
        /// </summary>
        private static readonly double[,] DistancesToBuildings = new double[,]
        { 
            //фунд фунд2жд15 жд7  бк  нбр вл1 вл35 вл>
            { 5, 3, 4, 2.8, 2, 1, 1, 2, 3 }, // Водопровод и напорная канализация 
            { 3, 1.5, 4, 2.8, 1.5, 1, 1, 2, 3 }, // Самотечная канализация
            { 3, 1, 4, 2.8, 1.5, 1, 1, 2, 3 }, // Дренаж
            { 0.4, 0.4, 0.4, 0, 0.4, 0, 0, 0, 0 }, // Сопутствующий дренаж
            { 2, 1.5, 4, 2.8, 1.5, 1, 1, 2, 3 }, // Теплосети от наружной стенки
            { 5, 1.5, 4, 2.8, 1.5, 1, 1, 2, 3 }, // теплосети от оболочки
            { 0.6, 0.5, 3.2, 2.8, 1.5, 1, 0.5, 5, 10 }, // Кабели силовые и связи
            { 2, 1.5, 4, 2.8, 1.5, 1, 1, 2, 3 }, // Каналы тоннели
            { 2, 1, 3.8, 2.8, 1.5, 1, 1, 3, 5 } // Пневмомусоропроводы
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
                case Networks.HouseholdSewer:
                    nw1 = 1;
                    break;
                case Networks.RainSewer:
                    nw1 = 2;
                    break;
                case Networks.PowerCable:
                    nw1 = 3;
                    break;
                case Networks.CommunicationCable:
                    nw1 = 4;
                    break;
                case Networks.HeatingNetworks:
                    nw1 = 5;
                    break;
                case Networks.ChannelsAndTunnels:
                    nw1 = 7;
                    break;
                case Networks.PneumoWasteChutes:
                    nw1 = 8;
                    break;
            }
            switch (secondNetwork)
            {
                case Networks.WaterPipe:
                    nw2 = 0;
                    break;
                case Networks.HouseholdSewer:
                    nw2 = 1;
                    break;
                case Networks.RainSewer:
                    nw2 = 2;
                    break;
                case Networks.PowerCable:
                    nw2 = 3;
                    break;
                case Networks.CommunicationCable:
                    nw2 = 4;
                    break;
                case Networks.HeatingNetworks:
                    nw2 = 5;
                    break;
                case Networks.ChannelsAndTunnels:
                    nw2 = 7;
                    break;
                case Networks.PneumoWasteChutes:
                    nw2 = 8;
                    break;
            }
            return Distances[nw1,nw2];
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
                case Networks.HouseholdSewer:
                    nw = 0;
                    break;
                case Networks.RainSewer:
                    nw = 1;
                    break;
                case Networks.PowerCable:
                case Networks.CommunicationCable:
                    nw = 6;
                    break;
                case Networks.HeatingNetworks:
                    nw = 4;
                    break;
                case Networks.ChannelsAndTunnels:
                    nw = 7;
                    break;
                case Networks.PneumoWasteChutes:
                    nw = 8;
                    break;
            }

            switch (building)
            {
                case Buildings.BuildingsFoundation:
                    bld = 0;
                    break;
                case Buildings.EnterprisesFoundation:
                    bld = 1;
                    break;
                case Buildings.Railways1520:
                    bld = 2;
                    break;
                case Buildings.Railways750:
                    bld = 3;
                    break;
                case Buildings.StreetSideStone:
                    bld = 4;
                    break;
                case Buildings.ExternalEdge:
                    bld = 5;
                    break;
                case Buildings.HvlSupportsFoundation1:
                    bld = 6;
                    break;
                case Buildings.HvlSupportsFoundation35:
                    bld = 7;
                    break;
                case Buildings.HvlSupportsFoundationOver:
                    bld = 8;
                    break;
            }

            return DistancesToBuildings[nw, bld];
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

            Distances[0,1] = distance;
            Distances[1,0] = distance;
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
                result[i + 1] = Distances[(int)networks[i],(int)networks[i + 1]];
            }

            // Расстояние между не соседними сетями
            for (int i = 0; i < networks.Length - 2; i++)
            {
                for (int j = i + 2; j < networks.Length; j++)
                {
                    double s = result.FromToSum(i + 1, j + 1);
                    if (s < Distances[(int)networks[i],(int)networks[j]])
                    {
                        double delta = Distances[(int)networks[i],(int)networks[j]] - s;
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

                if (networks[i] == Networks.HouseholdSewer)
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
            if (networks[networks.Length - 1] == Networks.HouseholdSewer)
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
            return Networks.ChannelsAndTunnels;
        }

        /// <summary>
        /// Установка названий слоев
        /// </summary>
        public static void SetLayers()
        {
            LayerNames.Clear();
            LayerNames[Networks.WaterPipe] = Properties.Settings.Default.WaterPipeLayerName;
            LayerNames[Networks.HouseholdSewer] = Properties.Settings.Default.SewerLayerName;
            LayerNames[Networks.HeatingNetworks] = Properties.Settings.Default.HeatingNetworkLayerName;
            LayerNames[Networks.CommunicationCable] = Properties.Settings.Default.CommunicationCableLayerName;
            LayerNames[Networks.PowerCable] = Properties.Settings.Default.PowerCableLayerName;

            NetworksMap.Clear();
            NetworksMap[Properties.Settings.Default.WaterPipeLayerName] = Networks.WaterPipe;
            NetworksMap[Properties.Settings.Default.SewerLayerName] = Networks.HouseholdSewer;
            NetworksMap[Properties.Settings.Default.HeatingNetworkLayerName] = Networks.HeatingNetworks;
            NetworksMap[Properties.Settings.Default.CommunicationCableLayerName] = Networks.CommunicationCable;
            NetworksMap[Properties.Settings.Default.PowerCableLayerName] = Networks.PowerCable;
        }
    }
}