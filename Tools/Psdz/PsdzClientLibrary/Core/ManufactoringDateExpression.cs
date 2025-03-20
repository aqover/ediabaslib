﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PsdzClient.Core
{
	[Serializable]
	public class ManufactoringDateExpression : RuleExpression
	{
        private readonly CompareExpression.ECompareOperator compareOperator;

        private readonly DateTime dateArgument;

        private readonly long datevalue;

        public ManufactoringDateExpression(CompareExpression.ECompareOperator compareOperator, long datevalue)
        {
            this.compareOperator = compareOperator;
            DateTime dateTime = new DateTime(datevalue);
            dateArgument = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
            this.datevalue = dateArgument.Ticks;
        }

        public new static ManufactoringDateExpression Deserialize(Stream ms, ILogger logger, Vehicle vec)
        {
            byte b = (byte)ms.ReadByte();
            CompareExpression.ECompareOperator eCompareOperator = (CompareExpression.ECompareOperator)b;
            byte[] array = new byte[8];
            ms.Read(array, 0, 8);
            long ticks = BitConverter.ToInt64(array, 0);
            logger.Debug("ManufactoringDateExpression.Deserialize()", "found date: {0}", new DateTime(ticks));
            return new ManufactoringDateExpression(eCompareOperator, ticks);
        }

        public override bool Evaluate(Vehicle vec, IFFMDynamicResolver ffmResolver, IRuleEvaluationServices ruleEvaluationServices, ValidationRuleInternalResults internalResult)
        {
            ILogger logger = ruleEvaluationServices.Logger;
            if (vec == null)
            {
                return false;
            }
            bool flag = false;
            string empty = string.Empty;
            long ticks;
            try
            {
                DateTime minValue = DateTime.MinValue;
                if (!minValue.Ticks.Equals(vec.ProductionDate.Ticks))
                {
                    ticks = vec.ProductionDate.Ticks;
                }
                else
                {
                    if (string.IsNullOrEmpty(vec.Modelljahr) || string.IsNullOrEmpty(vec.Modellmonat))
                    {
                        return false;
                    }
                    ticks = new DateTime(Convert.ToInt32(vec.Modelljahr, CultureInfo.InvariantCulture), Convert.ToInt32(vec.Modellmonat, CultureInfo.InvariantCulture), 1).Ticks;
                }
            }
            catch (Exception exception)
            {
                logger.WarningException("ManufactoringDateExpression.Evaluate()", exception);
                return false;
            }
            switch (compareOperator)
            {
                case CompareExpression.ECompareOperator.EQUAL:
                    flag = ticks == datevalue;
                    empty = "==";
                    break;
                case CompareExpression.ECompareOperator.NOT_EQUAL:
                    flag = ticks != datevalue;
                    empty = "!=";
                    break;
                case CompareExpression.ECompareOperator.GREATER:
                    flag = ticks > datevalue;
                    empty = ">";
                    break;
                case CompareExpression.ECompareOperator.GREATER_EQUAL:
                    flag = ticks >= datevalue;
                    empty = ">=";
                    break;
                case CompareExpression.ECompareOperator.LESS:
                    flag = ticks < datevalue;
                    empty = "<";
                    break;
                case CompareExpression.ECompareOperator.LESS_EQUAL:
                    flag = ticks <= datevalue;
                    empty = "<=";
                    break;
                default:
                    logger.Warning("ManufactoringDateExpression.Evaluate", "unknown logical operator: {0}", compareOperator);
                    flag = false;
                    break;
            }
            logger.Debug("ManufactoringDateExpression.Evaluate()", "rule: ProdDate {0} {1} result: {2}", compareOperator, datevalue, flag);
            return flag;
        }

        public override EEvaluationResult EvaluateVariantRule(ClientDefinition client, CharacteristicSet baseConfiguration, EcuConfiguration ecus)
        {
            if (baseConfiguration.ProdDates.Count == 0)
            {
                return EEvaluationResult.MISSING_CHARACTERISTIC;
            }
            int num = baseConfiguration.ProdDates.BinarySearch(datevalue);
            switch (compareOperator)
            {
                case CompareExpression.ECompareOperator.EQUAL:
                    return (num < 0) ? EEvaluationResult.INVALID : EEvaluationResult.VALID;
                case CompareExpression.ECompareOperator.NOT_EQUAL:
                    return (num >= 0) ? EEvaluationResult.INVALID : EEvaluationResult.VALID;
                case CompareExpression.ECompareOperator.GREATER:
                    if (num >= 0 && num < baseConfiguration.ProdDates.Count - 1)
                    {
                        return EEvaluationResult.VALID;
                    }
                    if (baseConfiguration.ProdDates.Count > 0 && baseConfiguration.ProdDates[0] > datevalue)
                    {
                        return EEvaluationResult.VALID;
                    }
                    return EEvaluationResult.INVALID;
                case CompareExpression.ECompareOperator.GREATER_EQUAL:
                    if (num >= 0)
                    {
                        return EEvaluationResult.VALID;
                    }
                    if (baseConfiguration.ProdDates.Count > 0 && baseConfiguration.ProdDates[0] > datevalue)
                    {
                        return EEvaluationResult.VALID;
                    }
                    return EEvaluationResult.INVALID;
                case CompareExpression.ECompareOperator.LESS:
                    if (num > 0)
                    {
                        return EEvaluationResult.VALID;
                    }
                    if (baseConfiguration.ProdDates.Count > 0 && baseConfiguration.ProdDates[baseConfiguration.ProdDates.Count - 1] < datevalue)
                    {
                        return EEvaluationResult.VALID;
                    }
                    return EEvaluationResult.INVALID;
                case CompareExpression.ECompareOperator.LESS_EQUAL:
                    if (num >= 0)
                    {
                        return EEvaluationResult.VALID;
                    }
                    if (baseConfiguration.ProdDates.Count > 0 && baseConfiguration.ProdDates[baseConfiguration.ProdDates.Count - 1] < datevalue)
                    {
                        return EEvaluationResult.VALID;
                    }
                    return EEvaluationResult.INVALID;
                default:
                    throw new Exception("Unknown logical operator");
            }
        }

        public override long GetExpressionCount()
        {
            return 1L;
        }

        public override long GetMemorySize()
        {
            return 20L;
        }

        public override IList<long> GetUnknownCharacteristics(CharacteristicSet baseConfiguration)
        {
            List<long> list = new List<long>();
            if (baseConfiguration.ProdDates == null || baseConfiguration.ProdDates.Count == 0)
            {
                list.Add(-1L);
            }
            return list;
        }

        public override void Serialize(MemoryStream ms)
        {
            ms.WriteByte(4);
            ms.WriteByte((byte)compareOperator);
            ms.Write(BitConverter.GetBytes(datevalue), 0, 8);
        }

        public override string ToFormula(FormulaConfig formulaConfig)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(FormulaSeparator(formulaConfig));
            stringBuilder.Append("(");
            stringBuilder.Append(formulaConfig.GetLongFunc);
            stringBuilder.Append("(\"Produktionsdatum\")");

            stringBuilder.Append(" ");
            stringBuilder.Append(this.GetFormulaOperator());
            stringBuilder.Append(" ");

            DateTime date = new DateTime(datevalue);
            stringBuilder.Append(date.ToString("yyyyMM", CultureInfo.InvariantCulture));
            stringBuilder.Append(")");
            stringBuilder.Append(FormulaSeparator(formulaConfig));

            return stringBuilder.ToString();
        }


        public override string ToString()
        {
            string @operator = GetOperator();
            long num = datevalue;
            return "Produktionsdatum " + @operator + " " + num.ToString(CultureInfo.InvariantCulture);
        }

        private string GetFormulaOperator()
        {
            switch (this.compareOperator)
            {
                case CompareExpression.ECompareOperator.EQUAL:
                    return "==";
                case CompareExpression.ECompareOperator.NOT_EQUAL:
                    return "!=";
                case CompareExpression.ECompareOperator.GREATER:
                    return ">";
                case CompareExpression.ECompareOperator.GREATER_EQUAL:
                    return ">=";
                case CompareExpression.ECompareOperator.LESS:
                    return "<";
                case CompareExpression.ECompareOperator.LESS_EQUAL:
                    return "<=";
                default:
                    throw new Exception("Unknown operator");
            }
        }

        private string GetOperator()
        {
            switch (compareOperator)
            {
                case CompareExpression.ECompareOperator.EQUAL:
                    return "=";
                case CompareExpression.ECompareOperator.GREATER:
                    return ">";
                case CompareExpression.ECompareOperator.GREATER_EQUAL:
                    return ">=";
                case CompareExpression.ECompareOperator.LESS:
                    return "<";
                case CompareExpression.ECompareOperator.LESS_EQUAL:
                    return "<=";
                case CompareExpression.ECompareOperator.NOT_EQUAL:
                    return "!=";
                default:
                    throw new Exception("Unknown operator");
            }
        }
    }
}
