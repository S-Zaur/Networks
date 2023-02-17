using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autocad = Autodesk.AutoCAD.ApplicationServices.Application;
using System.Linq;
using System;

// ReSharper disable MemberCanBePrivate.Global

namespace Networks
{
    internal static class AutocadExtensions
    {
        private const double Epsilon = 1e-7;

        /// <summary>
        /// Минимальное расстояние от кривой до кривой
        /// </summary>
        public static double GetMinDistanceToCurve(this Curve firstCurve, Curve secondCurve)
        {
            PointOnCurve3d[] pointOnCurve3d = firstCurve.GetGeCurve().GetClosestPointTo(secondCurve.GetGeCurve());
            Point3d pointOnFirstCurveClosestToSecondCurve = pointOnCurve3d.First().Point;
            Point3d pointOnSecondCurveClosestToFirstCurve =
                secondCurve.GetClosestPointTo(pointOnFirstCurveClosestToSecondCurve, false);
            return pointOnFirstCurveClosestToSecondCurve.DistanceTo(pointOnSecondCurveClosestToFirstCurve);
        }

        /// <summary>
        /// Получение коэффициента смещения кривой для смещения ее в сторону точки
        /// </summary>
        public static int GetOffsetCoefficient(this Curve curve, Point3d point)
        {
            int offsetCoefficient = 1;
            DBObjectCollection acDbObjColl = curve.GetOffsetCurves(offsetCoefficient * 0.001);
            foreach (Curve acEnt in acDbObjColl)
            {
                if (point.DistanceTo(curve.GetClosestPointTo(point, false)) <
                    point.DistanceTo(acEnt.GetClosestPointTo(point, false)))
                    offsetCoefficient = -1;
                break;
            }

            return offsetCoefficient;
        }

        /// <summary>
        /// Смещение кривой на заданное расстояние с заданними размерами
        /// </summary>
        /// <param name="curve">Кривая которая будет смещена</param>
        /// <param name="offset">Минимальное расстояние смещения</param>
        /// <param name="offsetCoefficient">Коэффициент смещения</param>
        /// <param name="size">Необходимый размер кривой</param>
        /// <returns>Коллекция объектов соответствующая коммуникации с заданными размерами</returns>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        /// <exception cref="ArgumentException"></exception>
        public static DBObjectCollection OffsetWithSize(
            this Curve curve,
            double offset,
            double offsetCoefficient,
            double size)
        {
            DBObjectCollection acDbObjColl = curve.GetOffsetCurves(offset * offsetCoefficient);
            foreach (DBObject cur in curve.GetOffsetCurves((offset + size) * offsetCoefficient))
            {
                acDbObjColl.Add(cur);
            }

            if (acDbObjColl.Count <= 1)
                throw new InvalidOperationException();
            var curve1 = acDbObjColl[0] as Curve;
            var curve2 = acDbObjColl[1] as Curve;
            if (curve1 is null || curve2 is null)
                throw new ArgumentException();
            acDbObjColl.Add(new Line(curve1.StartPoint, curve2.StartPoint));
            acDbObjColl.Add(new Line(curve1.EndPoint, curve2.EndPoint));
            return acDbObjColl;
        }

        /// <summary>
        /// Добавление объекта на чертеж
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="entity">Объект который небходимо добавить на чертеж</param>
        public static void Draw(this Transaction transaction, Entity entity)
        {
            BlockTable acBlkTbl =
                transaction.GetObject(Autocad.DocumentManager.MdiActiveDocument.Database.BlockTableId,
                    OpenMode.ForRead) as BlockTable;
            if (acBlkTbl is null) return;
            BlockTableRecord acBlkTblRec =
                transaction.GetObject(acBlkTbl[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
            if (acBlkTblRec is null) return;

            acBlkTblRec.AppendEntity(entity);
            transaction.AddNewlyCreatedDBObject(entity, true);
        }

        public static void DrawNetworks(
            this Transaction transaction,
            Curve curve,
            double[] sizes,
            double offsetCoefficient,
            Networks[] networks,
            Curve[] ignores)
        {
            var distance = 1.0;
            Networks network;
            for (int i = 0; i < networks.Length; i++)
            {
                network = networks[i];
                Curve nextCurve;
                var deltas = ignores.Select(x =>
                    NetworkManager.GetDistance(network, NetworkManager.GetType(x.Layer))).ToArray();
                if (network == Networks.WaterPipe && sizes[0] != 0)
                    nextCurve = curve.Offset(sizes[0], distance * offsetCoefficient, ignores, deltas);
                else if (network == Networks.HouseholdSewer && sizes[1] != 0)
                    nextCurve = curve.Offset(sizes[1], distance * offsetCoefficient, ignores, deltas);
                else if (network == Networks.HeatingNetworks && sizes[2] != 0)
                    nextCurve = curve.Offset(sizes[2], distance * offsetCoefficient, ignores, deltas);
                else
                    nextCurve = curve.Offset(distance * offsetCoefficient, ignores, deltas);
                nextCurve.Layer = NetworkManager.GetNetworkName(network);
                transaction.Draw(nextCurve);

                if (i >= networks.Length - 1)
                    continue;

                distance = curve.GetMinDistanceToCurve(nextCurve);
                distance += NetworkManager.GetDistance(networks[i], networks[i + 1]);
                if (network == Networks.WaterPipe && sizes[0] != 0)
                    distance += sizes[0] / 2;
                else if (network == Networks.HouseholdSewer && sizes[1] != 0)
                    distance += sizes[1] / 2;
                else if (network == Networks.HeatingNetworks && sizes[2] != 0)
                    distance += sizes[2] / 2;
                ignores = ignores.Append(nextCurve).ToArray();
            }
        }

        /// <summary>
        /// Смещение кривой на заданное минимальное расстояние с учетом расстояний до других кривых и необходимых размеров самой кривой
        /// </summary>
        /// <param name="firstCurve">Кривая которая будет смещена</param>
        /// <param name="size">Необходимый размер кривой</param>
        /// <param name="offset">Минимальное расстояние смещения</param>
        /// <param name="otherCurves">Игнорируемые кривые</param>
        /// <param name="otherCurvesDeltas">Минимальные расстояния до игнорируемых кривых</param>
        /// <returns>Итоговая смещенная кривая соответствующая минимальным расстояниям</returns>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        public static Curve Offset(this Curve firstCurve,
            double size,
            double offset,
            Curve[] otherCurves,
            double[] otherCurvesDeltas)
        {
            var offsetCoefficient = offset / Math.Abs(offset);
            offset = Math.Abs(offset);
            var deltaOffset = 0.1;
            var coefficient = 1;

            DBObjectCollection acDbObjColl = firstCurve.OffsetWithSize(offset, offsetCoefficient, size);
            var allRightFlag = true;
            foreach (Curve newCurve in acDbObjColl)
            {
                var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                for (int i = 0; i < distances.Length; i++)
                {
                    if (distances[i] >= otherCurvesDeltas[i])
                        continue;
                    allRightFlag = false;
                    break;
                }

                if (!allRightFlag)
                    break;
            }

            if (allRightFlag)
            {
                var curves = firstCurve.GetOffsetCurves((offset + size / 2) * offsetCoefficient);
                foreach (Curve variableCurve in curves)
                {
                    return variableCurve;
                }
            }

            allRightFlag = true;
            while (deltaOffset > Epsilon)
            {
                while (true)
                {
                    acDbObjColl = firstCurve.OffsetWithSize(offset, offsetCoefficient, size);
                    if (acDbObjColl.Count == 0)
                        throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
                    foreach (Curve newCurve in acDbObjColl)
                    {
                        var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                        double[] d = new double[distances.Length];
                        for (int i = 0; i < distances.Length; i++)
                            d[i] = distances[i] - otherCurvesDeltas[i];

                        if (d.Any(x => x < 0 && coefficient < 0))
                            break;

                        if (!d.All(x => x > 0) && coefficient > 0 ||
                            !d.Any(x => x < 0) && coefficient < 0)
                        {
                            allRightFlag = false;
                            break;
                        }
                    }

                    if (allRightFlag)
                    {
                        coefficient *= -1;
                        break;
                    }

                    offset += deltaOffset * coefficient;
                    allRightFlag = true;
                }

                deltaOffset /= 10;
            }

            var resultCurves = firstCurve.GetOffsetCurves((offset + size / 2) * offsetCoefficient);
            foreach (Curve variableCurve in resultCurves)
            {
                return variableCurve;
            }

            throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
        }

        /// <summary>
        /// Смещение кривой на заданное минимальное расстояние с учетом расстояний до других кривых
        /// </summary>
        /// <param name="firstCurve">Кривая которая будет смещена</param>
        /// <param name="offset">Минимальное расстояние смещения</param>
        /// <param name="otherCurves">Игнорируемые кривые</param>
        /// <param name="otherCurvesDeltas">Минимальные расстояния до игнорируемых кривых</param>
        /// <returns>Итоговая смещенная кривая соответствующая минимальным расстояниям</returns>
        /// <exception cref="InvalidOperationException">Ошибка возникающая при невозможности сметить кривую на нужное расстояние</exception>
        public static Curve Offset(this Curve firstCurve,
            double offset,
            Curve[] otherCurves,
            double[] otherCurvesDeltas)
        {
            var offsetCoefficient = offset / Math.Abs(offset);
            offset = Math.Abs(offset);
            var deltaOffset = 0.1;
            var allRightFlag = true;
            var coefficient = 1;

            DBObjectCollection acDbObjColl = firstCurve.GetOffsetCurves(offset * offsetCoefficient);
            foreach (Curve newCurve in acDbObjColl)
            {
                var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                for (int i = 0; i < distances.Length; i++)
                {
                    if (distances[i] >= otherCurvesDeltas[i])
                        continue;
                    allRightFlag = false;
                    break;
                }

                if (!allRightFlag)
                    break;
                return newCurve;
            }

            allRightFlag = true;
            while (deltaOffset > Epsilon)
            {
                while (true)
                {
                    acDbObjColl = firstCurve.GetOffsetCurves(offset * offsetCoefficient);
                    if (acDbObjColl.Count == 0)
                        throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
                    foreach (Curve newCurve in acDbObjColl)
                    {
                        var distances = otherCurves.Select(x => newCurve.GetMinDistanceToCurve(x)).ToArray();
                        double[] d = new double[distances.Length];
                        for (int i = 0; i < distances.Length; i++)
                            d[i] = distances[i] - otherCurvesDeltas[i];

                        if (d.All(x => x > 0) && coefficient > 0 ||
                            d.Any(x => x < 0) && coefficient < 0)
                        {
                            allRightFlag = false;
                            coefficient *= -1;
                            break;
                        }
                    }

                    if (!allRightFlag)
                        break;
                    offset += deltaOffset * coefficient;
                }

                deltaOffset /= 10;
                allRightFlag = true;
            }

            var resultCurves = firstCurve.GetOffsetCurves(offset * offsetCoefficient);
            foreach (Curve variableCurve in resultCurves)
            {
                return variableCurve;
            }

            throw new InvalidOperationException($"Невозможно сместить кривую на расстояние {offset}");
        }

        /// <summary>
        /// Получение суммы массива от индекса <paramref name="fromIndex"/> до индекса <paramref name="toIndex"/>
        /// </summary>
        public static double FromToSum(this double[] array, int fromIndex, int toIndex)
        {
            double sum = 0;
            for (int i = fromIndex; i < toIndex; i++)
                sum += array[i];
            return sum;
        }
    }
}