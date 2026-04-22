using System;

namespace Pixel.iris5.shared.br
{
    internal sealed class _RuleParser
    {
        private readonly _RuleScanner _sc;

        public _RuleParser(_RuleScanner sc)
        {
            _sc = sc;
        }

        public bool ParseOr()
        {
            var left = ParseAnd();
            while (_sc.Peek().Kind == _RuleTokKind.OR)
            {
                _sc.Consume();
                var right = ParseAnd();
                left = left || right;
            }

            return left;
        }

        private bool ParseAnd()
        {
            var left = ParseCmp();
            while (_sc.Peek().Kind == _RuleTokKind.AND)
            {
                _sc.Consume();
                var right = ParseCmp();
                left = left && right;
            }

            return left;
        }

        private bool ParseCmp()
        {
            var left = ParseAtom();
            var kind = _sc.Peek().Kind;

            if (kind != _RuleTokKind.EQ && kind != _RuleTokKind.NEQ &&
                kind != _RuleTokKind.GT && kind != _RuleTokKind.GTE &&
                kind != _RuleTokKind.LT && kind != _RuleTokKind.LTE)
            {
                return IsTruthy(left);
            }

            _sc.Consume();
            var right = ParseAtom();
            return Compare(left, kind, right);
        }

        private object? ParseAtom()
        {
            var tok = _sc.Peek();
            if (tok.Kind == _RuleTokKind.LP)
            {
                _sc.Consume();
                var result = ParseOr();
                if (_sc.Peek().Kind == _RuleTokKind.RP)
                {
                    _sc.Consume();
                }

                return result ? 1 : 0;
            }

            _sc.Consume();
            return tok.Val;
        }

        private static bool Compare(object? left, _RuleTokKind op, object? right)
        {
            if (left == null && right == null) return op == _RuleTokKind.EQ;
            if (left == null || right == null) return op == _RuleTokKind.NEQ;

            if (left is int leftIntCoerce && right is decimal)
            {
                left = (decimal)leftIntCoerce;
            }

            if (left is decimal && right is int rightIntCoerce)
            {
                right = (decimal)rightIntCoerce;
            }

            if (left is DateTime leftDate && right is DateTime rightDate)
            {
                return EvalCmp(leftDate.CompareTo(rightDate), op);
            }

            if (left is decimal leftDecimal && right is decimal rightDecimal)
            {
                return EvalCmp(leftDecimal.CompareTo(rightDecimal), op);
            }

            if (left is int leftInt && right is int rightInt)
            {
                return op switch
                {
                    _RuleTokKind.EQ => leftInt == rightInt,
                    _RuleTokKind.NEQ => leftInt != rightInt,
                    _RuleTokKind.GT => leftInt > rightInt,
                    _RuleTokKind.GTE => leftInt >= rightInt,
                    _RuleTokKind.LT => leftInt < rightInt,
                    _RuleTokKind.LTE => leftInt <= rightInt,
                    _ => false
                };
            }

            var leftString = left.ToString();
            var rightString = right.ToString();
            return EvalCmp(string.CompareOrdinal(leftString, rightString), op);
        }

        private static bool EvalCmp(int cmpResult, _RuleTokKind op)
        {
            return op switch
            {
                _RuleTokKind.EQ => cmpResult == 0,
                _RuleTokKind.NEQ => cmpResult != 0,
                _RuleTokKind.GT => cmpResult > 0,
                _RuleTokKind.GTE => cmpResult >= 0,
                _RuleTokKind.LT => cmpResult < 0,
                _RuleTokKind.LTE => cmpResult <= 0,
                _ => false
            };
        }

        private static bool IsTruthy(object? value)
        {
            return value switch
            {
                bool boolean => boolean,
                int integer => integer != 0,
                decimal decimalValue => decimalValue != 0m,
                string text => !string.IsNullOrEmpty(text),
                _ => value != null
            };
        }
    }
}
