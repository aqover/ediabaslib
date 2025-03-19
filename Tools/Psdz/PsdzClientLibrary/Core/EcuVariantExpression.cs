﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BMW.Rheingold.CoreFramework.Contracts.Vehicle;

namespace PsdzClient.Core
{
	public class EcuVariantExpression : SingleAssignmentExpression
	{
        public string VariantName { get; private set; }
        
        public EcuVariantExpression()
		{
		}

		public EcuVariantExpression(long ecuVariantId)
		{
			this.value = ecuVariantId;
		}

		public override bool Evaluate(Vehicle vec, IFFMDynamicResolver ffmResolver, IRuleEvaluationServices ruleEvaluationServices, ValidationRuleInternalResults internalResult)
        {
            if (vec == null)
            {
                ruleEvaluationServices.Logger.Warning("EcuVariantExpression.Evaluate()", "vec was null");
                return false;
            }
            PsdzDatabase database = ClientContext.GetDatabase(vec);
            if (database == null)
            {
                return false;
            }
            PsdzDatabase.EcuVar ecuVariantById = database.GetEcuVariantById(this.value.ToString(CultureInfo.InvariantCulture));
            if (ecuVariantById == null)
            {
                ruleEvaluationServices.Logger.Warning("EcuVariantExpression.Evaluate()", "no valid variant information found for id: {0}", value);
                return false;
            }
            VariantName = ecuVariantById.Name;
            if (vec.VCI != null && (vec.VehicleIdentLevel == IdentificationLevel.BasicFeatures || vec.VehicleIdentLevel == IdentificationLevel.VINBasedFeatures || vec.VehicleIdentLevel == IdentificationLevel.VINOnly))
            {
                RuleEvaluationUtill ruleEvaluationUtill = new RuleEvaluationUtill(ruleEvaluationServices, database);
                if (ruleEvaluationUtill.EvaluateSingleRuleExpression(vec, ecuVariantById.Id, ffmResolver))
                {
                    PsdzDatabase.EcuGroup ecuGroupById = database.GetEcuGroupById(ecuVariantById.EcuGroupId);
                    if (ecuGroupById != null)
                    {
                        if (ruleEvaluationUtill.EvaluateSingleRuleExpression(vec, ecuGroupById.Id, ffmResolver))
                        {
                            ruleEvaluationServices.Logger.Info("EcuVariantExpression.Evaluate()", "Infosession and manual VIN input => no ECU variant evaluation for {0} due to VehicleIdentLevel: {1} but found valid variant and valid group => true", VariantName, vec.VehicleIdentLevel);
                            return true;
                        }
                        ruleEvaluationServices.Logger.Info("EcuVariantExpression.Evaluate()", "Infosession and manual VIN input => no ECU variant evaluation for {0} due to VehicleIdentLevel: {1} and found valid variant found without a valid group => false", VariantName, vec.VehicleIdentLevel);
                        return false;
                    }
                    ruleEvaluationServices.Logger.Info("EcuVariantExpression.Evaluate()", "Infosession and manual VIN input => no ECU variant evaluation for {0} due to VehicleIdentLevel: {1} and found valid variant found without a group", VariantName, vec.VehicleIdentLevel);
                    return true;
                }
                ruleEvaluationServices.Logger.Info("EcuVariantExpression.Evaluate()", "Infosession and manual VIN input => no ECU variant evaluation for {0} due to VehicleIdentLevel: {1} and found NO valid variant and NO valid group", VariantName, vec.VehicleIdentLevel);
                return false;
            }
            if (vec.VehicleIdentLevel == IdentificationLevel.VINBasedOnlineUpdated && vec.ECU != null && !vec.ECU.Any())
            {
                ruleEvaluationServices.Logger.Info("EcuVariantExpression.Evaluate()", "Infosession and manual VIN input => no ECU variant evaluation for {0} due to VehicleIdentLevel: {1}", VariantName, vec.VehicleIdentLevel);
                return true;
            }
            bool flag = vec.getECUbyECU_SGBD(VariantName) != null;
            ruleEvaluationServices.Logger.Debug("EcuVariantExpression.Evaluate()", "EcuVariantId: {0} (original rule: {1})  result: {2}", value, VariantName, flag);
            return flag;
        }

        public override EEvaluationResult EvaluateVariantRule(ClientDefinition client, CharacteristicSet baseConfiguration, EcuConfiguration ecus)
		{
			if (ecus.EcuVariants.ToList<long>().BinarySearch(this.value) >= 0)
			{
				return EEvaluationResult.VALID;
			}
			if (ecus.UnknownEcuVariants.ToList<long>().BinarySearch(this.value) >= 0)
			{
				return EEvaluationResult.MISSING_VARIANT;
			}
			return EEvaluationResult.INVALID;
		}

		public override IList<long> GetUnknownVariantIds(EcuConfiguration ecus)
		{
			List<long> list = new List<long>();
			if (ecus.UnknownEcuVariants.ToList<long>().BinarySearch(this.value) >= 0)
			{
				list.Add(this.value);
			}
			return list;
		}

		public override void Serialize(MemoryStream ms)
		{
			ms.WriteByte(11);
			base.Serialize(ms);
		}

        public override string ToFormula(FormulaConfig formulaConfig)
        {
            PsdzDatabase.EcuVar ecuVariantById = ClientContext.GetDatabase(this.vecInfo)?.GetEcuVariantById(this.value.ToString(CultureInfo.InvariantCulture));
            
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(FormulaSeparator(formulaConfig));
            stringBuilder.Append(formulaConfig.CheckStringFunc);
            stringBuilder.Append("(\"EcuVariant\", ");
            stringBuilder.Append("\"");
            if (ecuVariantById != null)
            {
                stringBuilder.Append(ecuVariantById.Name);
            }
            stringBuilder.Append("\")");
            stringBuilder.Append(FormulaSeparator(formulaConfig));

            return stringBuilder.ToString();
        }

		public override string ToString()
		{
			return string.Concat(new string[]
			{
				"EcuVariant=",
				this.value.ToString(CultureInfo.InvariantCulture),
				" (",
				this.VariantName,
				")"
			});
		}

		private string variantName;
    }
}
