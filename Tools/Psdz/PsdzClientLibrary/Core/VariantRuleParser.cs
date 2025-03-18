﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PsdzClient.Core
{
	public class VariantRuleParser
	{
        private VariantRuleParser()
		{
		}

		public static RuleExpression Parse(string rule)
		{
            Stack<Symbol> stack = new Stack<Symbol>();
            int i = 0;
            while (i < rule.Length)
            {
                char c = rule[i++];
                Symbol symbol;
                if (char.IsDigit(c))
                {
                    int num = i - 1;
                    for (; i < rule.Length && char.IsDigit(rule[i]); i++)
                    {
                    }
                    symbol = new Symbol();
                    symbol.Type = RuleExpression.ESymbolType.Value;
                    symbol.Value = Convert.ToInt64(rule.Substring(num, i - num), CultureInfo.InvariantCulture);
                }
                else if (char.IsLetter(c))
                {
                    string text = c.ToString(CultureInfo.InvariantCulture);
                    while (i < rule.Length && char.IsLetter(rule[i]))
                    {
                        text += rule[i++];
                    }
                    switch (text)
                    {
                        case "AND":
                            symbol = new Symbol(RuleExpression.ESymbolType.TerminalAnd);
                            break;
                        case "OR":
                            symbol = new Symbol(RuleExpression.ESymbolType.TerminalOr);
                            break;
                        case "NOT":
                            symbol = new Symbol(RuleExpression.ESymbolType.TerminalNot);
                            break;
                        case "Produktionsdatum":
                            symbol = new Symbol(RuleExpression.ESymbolType.TerminalProduktionsdatum);
                            break;
                        default:
                            throw new Exception("Unknown terminal symbol '" + text + "' at position " + i);
                    }
                }
                else
                {
                    switch (c)
                    {
                        case '(':
                            symbol = new Symbol();
                            symbol.Type = RuleExpression.ESymbolType.TerminalLPar;
                            break;
                        case ')':
                            symbol = new Symbol();
                            symbol.Type = RuleExpression.ESymbolType.TerminalRPar;
                            break;
                        case '=':
                            symbol = new Symbol();
                            symbol.Type = RuleExpression.ESymbolType.Operator;
                            symbol.Value = CompareExpression.ECompareOperator.EQUAL;
                            break;
                        case '<':
                            symbol = new Symbol();
                            symbol.Type = RuleExpression.ESymbolType.Operator;
                            if (i < rule.Length && rule[i] == '=')
                            {
                                i++;
                                symbol.Value = CompareExpression.ECompareOperator.LESS_EQUAL;
                            }
                            else
                            {
                                symbol.Value = CompareExpression.ECompareOperator.LESS;
                            }
                            break;
                        case '>':
                            symbol = new Symbol();
                            symbol.Type = RuleExpression.ESymbolType.Operator;
                            if (i < rule.Length && rule[i] == '=')
                            {
                                i++;
                                symbol.Value = CompareExpression.ECompareOperator.GREATER_EQUAL;
                            }
                            else
                            {
                                symbol.Value = CompareExpression.ECompareOperator.GREATER;
                            }
                            break;
                        default:
                            if (char.IsWhiteSpace(c))
                            {
                                symbol = null;
                                break;
                            }
                            throw new Exception("Unknown character at position " + i);
                    }
                }
                if (symbol == null)
                {
                    continue;
                }
                stack.Push(symbol);
                bool flag = true;
                while (flag)
                {
                    Symbol symbol2 = stack.Pop();
                    Symbol symbol3 = ((stack.Count <= 0) ? new Symbol(RuleExpression.ESymbolType.Unknown) : stack.Pop());
                    Symbol symbol4 = ((stack.Count <= 0) ? new Symbol(RuleExpression.ESymbolType.Unknown) : stack.Pop());
                    bool flag2 = false;
                    if (symbol4.Type == RuleExpression.ESymbolType.Value && symbol3.Type == RuleExpression.ESymbolType.Operator && symbol2.Type == RuleExpression.ESymbolType.Value)
                    {
                        Symbol symbol5 = new Symbol(RuleExpression.ESymbolType.CompareExpression);
                        symbol5.Value = new CompareExpression((long)symbol4.Value, (CompareExpression.ECompareOperator)symbol3.Value, (long)symbol2.Value);
                        stack.Push(symbol5);
                        flag2 = true;
                    }
                    else if (IsExpression(symbol4) && symbol3.Type == RuleExpression.ESymbolType.TerminalAnd && IsExpression(symbol2))
                    {
                        Symbol symbol6 = new Symbol(RuleExpression.ESymbolType.AndExpression);
                        symbol6.Value = new AndExpression((RuleExpression)symbol4.Value, (RuleExpression)symbol2.Value);
                        stack.Push(symbol6);
                        flag2 = true;
                    }
                    else if (IsExpression(symbol4) && symbol3.Type == RuleExpression.ESymbolType.TerminalOr && IsExpression(symbol2))
                    {
                        Symbol symbol7 = new Symbol(RuleExpression.ESymbolType.OrExpression);
                        symbol7.Value = new OrExpression((RuleExpression)symbol4.Value, (RuleExpression)symbol2.Value);
                        stack.Push(symbol7);
                        flag2 = true;
                    }
                    else if (symbol4.Type == RuleExpression.ESymbolType.TerminalProduktionsdatum && symbol3.Type == RuleExpression.ESymbolType.Operator && symbol2.Type == RuleExpression.ESymbolType.Value)
                    {
                        Symbol symbol8 = new Symbol(RuleExpression.ESymbolType.DateExpression);
                        symbol8.Value = new DateExpression((CompareExpression.ECompareOperator)symbol3.Value, (long)symbol2.Value);
                        stack.Push(symbol8);
                        flag2 = true;
                    }
                    else if (symbol3.Type == RuleExpression.ESymbolType.TerminalNot && IsExpression(symbol2))
                    {
                        Symbol symbol9 = new Symbol(RuleExpression.ESymbolType.NotExpression);
                        symbol9.Value = new NotExpression((RuleExpression)symbol2.Value);
                        if (symbol4.Type != 0)
                        {
                            stack.Push(symbol4);
                        }
                        stack.Push(symbol9);
                        flag2 = true;
                    }
                    else if (symbol4.Type == RuleExpression.ESymbolType.TerminalLPar && IsExpression(symbol3) && symbol2.Type == RuleExpression.ESymbolType.TerminalRPar)
                    {
                        stack.Push(symbol3);
                        flag2 = true;
                    }
                    if (!flag2)
                    {
                        if (symbol4.Type != 0)
                        {
                            stack.Push(symbol4);
                        }
                        if (symbol3.Type != 0)
                        {
                            stack.Push(symbol3);
                        }
                        stack.Push(symbol2);
                        flag = false;
                    }
                    else
                    {
                        flag = true;
                    }
                }
            }
            switch (stack.Count)
            {
                case 0:
                    return null;
                case 1:
                    {
                        Symbol symbol10 = stack.Pop();
                        if (IsExpression(symbol10))
                        {
                            return (RuleExpression)symbol10.Value;
                        }
                        throw new Exception("Illegal last token");
                    }
                default:
                    throw new Exception("Could not completely reduce tokens");
            }
        }

        private static bool IsExpression(Symbol op)
        {
            return op.Type == RuleExpression.ESymbolType.CompareExpression || op.Type == RuleExpression.ESymbolType.AndExpression || op.Type == RuleExpression.ESymbolType.OrExpression || op.Type == RuleExpression.ESymbolType.NotExpression || op.Type == RuleExpression.ESymbolType.DateExpression;
        }
    }
}
