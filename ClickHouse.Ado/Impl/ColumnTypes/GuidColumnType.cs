namespace ClickHouse.Ado.Impl.ColumnTypes
{
    using System;
    using System.Collections;
    using System.Data;
    using System.Linq;
    using System.Runtime.InteropServices;
    using ATG.Insert;
    using Buffer = System.Buffer;

    /// <summary> UUID column type </summary>
    internal class GuidColumnType : ColumnType
    {
        internal const string UuidColumnTypeName = "UUID";
        
        public Guid[] Data { get; protected set; }
        
        public override int Rows => Data?.Length ?? 0;

        internal override Type CLRType => typeof(Guid);

        public GuidColumnType()
        {
            
        }

        public GuidColumnType(Guid[] data)
        {
            Data = data;
        }

        private static readonly int[] swapTable = {
            4,5,6,7,2,3,0,1,15,14,13,12,11,10,9,8
        };
        internal override void Read(ProtocolFormatter formatter, int rows)
        {
            var itemSize = Marshal.SizeOf(typeof(Guid));
            var xdata = new Guid[rows];
            var guidSwappedBytes = new byte[itemSize];
            for (var i = 0; i < rows; i++) {
                var guidBytes = formatter.ReadBytes(itemSize);
                for (var b = 0; b < itemSize; b++)
                    guidSwappedBytes[b] = guidBytes[swapTable[b]];
                xdata[i] = new Guid(guidSwappedBytes);
            }
            Data = xdata;
        }

        public override void ValueFromConst(Parser.ValueType val)
        {
            switch (val.TypeHint)
            {
                case Parser.ConstType.String:
                    Data = new[]
                           {
                               new Guid(val.StringValue)
                           };
                    break;
                default:
                    throw new InvalidCastException("Cannot convert numeric value to Guid.");
            }
        }

        public override string AsClickHouseType()
        {
            return UuidColumnTypeName;
        }

        public override void Write(ProtocolFormatter formatter, int rows) {
            foreach (var d in Data) {
                var guidBytes = d.ToByteArray();
                formatter.WriteBytes(guidBytes, 6, 2);
                formatter.WriteBytes(guidBytes, 4, 2);
                formatter.WriteBytes(guidBytes, 0, 4);
                for (var b = 15; b >= 8; b--)
                    formatter.WriteByte(guidBytes[b]);
            }
        }

        public override void ValueFromParam(ClickHouseParameter parameter)
        {
            switch (parameter.DbType)
            {
                case DbType.Guid:
                    Data = new[] {(Guid) Convert.ChangeType(parameter.Value, typeof(Guid))};
                    break;
                default:
                    throw new InvalidCastException($"Cannot convert parameter with type {parameter.DbType} to Guid.");
            }
        }

        public override object Value(int currentRow)
        {
            return Data[currentRow];
        }

        public override long IntValue(int currentRow)
        {
            throw new InvalidCastException();
        }

        public override void ValuesFromConst(IEnumerable objects)
        {
            Data = objects.Cast<Guid>().ToArray();
        }
    }
}