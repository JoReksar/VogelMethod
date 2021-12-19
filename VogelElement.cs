using System;

namespace ConsoleApp5
{
    internal class VogelElement
    {
        public int Tariff { get; }
        public decimal Value { get; }
        public bool IsClosed { get; protected set; }
        
        public VogelElement(int tariff, decimal value)
            : this(tariff, value, false)
        {
            if (tariff <= 0)
                throw new ArgumentException($"Тариф не может быть меньше 1, переданное значение было равно {tariff}", nameof(tariff));
            if (value < 0)
                throw new ArgumentException($"Элемент не может содержать значения меньше 0. Значение было {value}", nameof(value));
        }
        protected VogelElement(int tariff, decimal value, bool isClosed)
        {
            Tariff = tariff;
            Value = value;
            IsClosed = isClosed;
        }

        internal void SetAsClosed()
            => IsClosed = true;
        public VogelElement Clone()
            => new VogelElement(Tariff, Value, IsClosed);

        public override string ToString()
            => $"{Tariff}|{Value}";

        public static VogelElement operator +(VogelElement leftOperand, decimal rightOperand)
        {
            return new VogelElement(leftOperand.Tariff, leftOperand.Value + rightOperand);
        }
        public static VogelElement operator -(VogelElement leftOperand, decimal rightOperand)
        {
            return new VogelElement(leftOperand.Tariff, leftOperand.Value - rightOperand);
        }
    }
}
