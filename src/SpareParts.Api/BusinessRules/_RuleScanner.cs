using System;
using System.Globalization;

namespace Pixel.iris5.shared.br
{
    internal sealed class _RuleScanner
    {
        internal const string PfxBool = "Convert.ToInt32(Convert.ToBoolean(\"";
        internal const string PfxDecimal = "Convert.ToDecimal(\"";
        internal const string PfxDate = "Convert.ToDateTime(\"";

        private readonly string _s;
        private int _i;
        private _RuleTok? _peek;

        public _RuleScanner(string s)
        {
            _s = s ?? string.Empty;
        }

        public _RuleTok ParseToDecimal()
        {
            var q1 = _i + PfxDecimal.Length - 1;
            var q2 = _s.IndexOf('"', q1 + 1);
            if (q2 < 0)
            {
                _i = _s.Length;
                return Tok(_RuleTokKind.EOF);
            }

            var numStr = _s.Substring(q1 + 1, q2 - q1 - 1);
            var closeP = _s.IndexOf(')', q2);
            _i = closeP >= 0 ? closeP + 1 : _s.Length;
            return Tok(_RuleTokKind.ValDec, decimal.Parse(numStr, CultureInfo.InvariantCulture));
        }

        public _RuleTok ParseToDate()
        {
            var q1 = _i + PfxDate.Length - 1;
            var q2 = _s.IndexOf('"', q1 + 1);
            if (q2 < 0)
            {
                _i = _s.Length;
                return Tok(_RuleTokKind.EOF);
            }

            var dtStr = _s.Substring(q1 + 1, q2 - q1 - 1);
            var closeP = _s.IndexOf(')', q2);
            _i = closeP >= 0 ? closeP + 1 : _s.Length;
            return Tok(_RuleTokKind.ValDt, DateTime.Parse(dtStr, CultureInfo.InvariantCulture));
        }

        public _RuleTok ParseToBool()
        {
            var q1 = _i + PfxBool.Length - 1;
            var q2 = _s.IndexOf('"', q1 + 1);
            if (q2 < 0)
            {
                _i = _s.Length;
                return Tok(_RuleTokKind.EOF);
            }

            var boolVal = _s.Substring(q1 + 1, q2 - q1 - 1);
            var ppPos = _s.IndexOf("))", q2, StringComparison.Ordinal);
            _i = ppPos >= 0 ? ppPos + 2 : _s.Length;
            var intVal = string.Equals(boolVal, "True", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            return Tok(_RuleTokKind.ValInt, intVal);
        }

        public _RuleTok Peek()
        {
            if (_peek == null)
            {
                _peek = Read();
            }

            return _peek.Value;
        }

        public _RuleTok Consume()
        {
            var token = Peek();
            _peek = null;
            return token;
        }

        public bool StartsWith(string prefix)
        {
            if (_i + prefix.Length > _s.Length)
            {
                return false;
            }

            return string.Compare(_s, _i, prefix, 0, prefix.Length, StringComparison.Ordinal) == 0;
        }

        private void SkipWS()
        {
            while (_i < _s.Length && char.IsWhiteSpace(_s[_i]))
            {
                _i++;
            }
        }

        private static _RuleTok Tok(_RuleTokKind kind, object? value = null)
            => new() { Kind = kind, Val = value };

        private _RuleTok Read()
        {
            SkipWS();
            if (_i >= _s.Length)
            {
                return Tok(_RuleTokKind.EOF);
            }

            if (StartsWith(PfxBool))
            {
                return ParseToBool();
            }

            if (StartsWith(PfxDecimal))
            {
                return ParseToDecimal();
            }

            if (StartsWith(PfxDate))
            {
                return ParseToDate();
            }

            if (StartsWith("&&")) { _i += 2; return Tok(_RuleTokKind.AND); }
            if (StartsWith("||")) { _i += 2; return Tok(_RuleTokKind.OR); }
            if (StartsWith("==")) { _i += 2; return Tok(_RuleTokKind.EQ); }
            if (StartsWith("!=")) { _i += 2; return Tok(_RuleTokKind.NEQ); }
            if (StartsWith(">=")) { _i += 2; return Tok(_RuleTokKind.GTE); }
            if (StartsWith("<=")) { _i += 2; return Tok(_RuleTokKind.LTE); }

            var c = _s[_i];
            if (c == '(') { _i++; return Tok(_RuleTokKind.LP); }
            if (c == ')') { _i++; return Tok(_RuleTokKind.RP); }
            if (c == '>') { _i++; return Tok(_RuleTokKind.GT); }
            if (c == '<') { _i++; return Tok(_RuleTokKind.LT); }

            if (c == '"')
            {
                _i++;
                var start = _i;
                while (_i < _s.Length && _s[_i] != '"')
                {
                    _i++;
                }

                var strVal = _s.Substring(start, _i - start);
                if (_i < _s.Length)
                {
                    _i++;
                }

                return Tok(_RuleTokKind.ValStr, strVal);
            }

            if (c == '-' || char.IsDigit(c))
            {
                var start = _i;
                if (c == '-')
                {
                    _i++;
                }

                while (_i < _s.Length && (char.IsDigit(_s[_i]) || _s[_i] == '.'))
                {
                    _i++;
                }

                var numStr = _s.Substring(start, _i - start);
                if (numStr == "-")
                {
                    return Read();
                }

                if (numStr.IndexOf('.') >= 0)
                {
                    return Tok(_RuleTokKind.ValDec, decimal.Parse(numStr, CultureInfo.InvariantCulture));
                }

                return Tok(_RuleTokKind.ValInt, int.Parse(numStr, CultureInfo.InvariantCulture));
            }

            _i++;
            return Read();
        }
    }
}
