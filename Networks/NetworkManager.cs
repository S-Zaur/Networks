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
        HvlSupportsFoundation,
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
        private static readonly double[][] Distances = new double[][]
        {
            new[] { 1,   1.5, 1.5, 0.5, 0.5, 1.5, 1.5, 1.5, 1   }, // Водопровод
            new[] { 1.5, 0.4, 0.4, 0.5, 0.5, 1,   1,   1,   1   }, // Канализация бытовая
            new[] { 1.5, 0.4, 0.4, 0.5, 0.5, 1,   1,   1,   1   }, // Канализация дождевая
            new[] { 0.5, 0.5, 0.5, 0.1, 0.5, 2,   2,   2,   1.5 }, // Кабели силовые
            new[] { 0.5, 0.5, 0.5, 0.5, 0,   1,   1,   1,   1   }, // Кабели связи
            new[] { 1.5, 1,   1,   2,   1,   0,   0,   2,   1   }, // Теплосети от наружной стенки
            new[] { 1.5, 1,   1,   2,   1,   0,   0,   2,   1   }, // Теплосети от оболочки
            new[] { 1.5, 1,   1,   2,   1,   2,   2,   0,   1   }, // Каналы тоннели
            new[] { 1,   1,   1,   1.5, 1,   1,   1,   1,   0   }  // Пневмомусоропроводы
        };

        private static readonly double[][] Distances2 = new double[][]
        {//         фунд фунд2жд15 жд7  бк  нбр вл1 вл35 вл>
            new[] { 5,   3,   4,   2.8, 2,   1, 1,   2, 3  }, // Водопровод и напорная канализация 
            new[] { 3,   1.5, 4,   2.8, 1.5, 1, 1,   2, 3  }, // Самотечная канализация
            new[] { 3,   1,   4,   2.8, 1.5, 1, 1,   2, 3  }, // Дренаж
            new[] { 0.4, 0.4, 0.4, 0,   0.4, 0, 0,   0, 0  }, // Сопутствующий дренаж
            new[] { 2,   1.5, 4,   2.8, 1.5, 1, 1,   2, 3  }, // Теплосети от наружной стенки
            new[] { 5,   1.5, 4,   2.8, 1.5, 1, 1,   2, 3  }, // теплосети от оболочки
            new[] { 0.6, 0.5, 3.2, 2.8, 1.5, 1, 0.5, 5, 10 }, // Кабели силовые и связи
            new[] { 2,   1.5, 4,   2.8, 1.5, 1, 1,   2, 3  }, // Каналы тоннели
            new[] { 2,   1,   3.8, 2.8, 1.5, 1, 1,   3, 5  }  // Пневмомусоропроводы
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
            return Distances[(int)firstNetwork][(int)secondNetwork];
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

            Distances[0][1] = distance;
            Distances[1][0] = distance;
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
                result[i + 1] = Distances[(int)networks[i]][(int)networks[i + 1]];
            }

            // Расстояние между не соседними сетями
            for (int i = 0; i < networks.Length - 2; i++)
            {
                for (int j = i + 2; j < networks.Length; j++)
                {
                    double s = result.FromToSum(i + 1, j + 1);
                    if (s < Distances[(int)networks[i]][(int)networks[j]])
                    {
                        double delta = Distances[(int)networks[i]][(int)networks[j]] - s;
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